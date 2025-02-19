/****************************************************************************************
    File name:      VolumeCloud.hlsl
	Author:			danshenmiao
	Versions:		1.0
	Creation time:	2025.1.10
	Finish time:	2025.2.1
	Abstract:       实现基本的体积云
****************************************************************************************/

/****************************************************************************************
    File name:      VolumeCloud.hlsl
	Author:			danshenmiao
	Versions:		1.0
	Creation time:	2025.2.1
	Finish time:	2025.2.2
	Abstract:       1. 优化rayMarching
                    优化思路：
                        由于采样密度时会弱化底部和顶部，因此开始步进时采用大步进，遇到密度非 0 时切换到普通步进.
                        后续步进累计密度连续为 0 的采样次数，若达到阈值则进入大步进.
                        若遇到密度非 0 时则切换到普通步进，并回退一步，防止漏采样.

                    优化前的步进：
                    for(int i = 0; i < _ShapeMarchingCount; currPos += rayStep, ++i){
                        // 计算当前点的密度
                        float density = SampleDensity(currPos, true, weatherDensityScale) * stepSize;

                        if(density > 0){
                            float lightDensity = LightMarching(currPos, lightDir);
                            lightIntensity += density * transmittance * lightDensity * phase;
                            // 计算吸光率
                            float absorbance = weatherDensityScale * _ExtinctionCoefficient * density;
                            transmittance *= BeerLambert(absorbance);
                            sumDensity += density;
                            
                            if(transmittance < 0.01) break;
                        }
                    }


                    2. 引入蓝噪声,将其作用在raymarching起始位置上，消除步进带来的层次感

                    3. 添加分帧绘制，提供不分帧、4帧、16帧三种
****************************************************************************************/

#ifndef __VOLUMECLOUD__HLSL__
#define __VOLUMECLOUD__HLSL__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "VolumeCloudUtil.hlsl"

TEXTURE2D(_CloudBackTex);
TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_WeatherNoiceTex);
TEXTURE2D(_BlueNoiceTex);

TEXTURE3D(_ShapeNoiceTex);
TEXTURE3D(_DetailNoiceTex);

SAMPLER(sampler_CloudBackTex);
SAMPLER(sampler_CameraDepthTexture);
SAMPLER(sampler_WeatherNoiceTex);
SAMPLER(sampler_BlueNoiceTex);
SAMPLER(sampler_ShapeNoiceTex);
SAMPLER(sampler_DetailNoiceTex);

CBUFFER_START(UnityPreMaterial)
float4 _CloudBoxMin;
float4 _CloudBoxMax;

float4 _ShapeWeights;
float4 _DetailWeights;

float4 _SampleShapeOffset;
float _SampleShapeScale;

float4 _SampleDetailOffset;
float _SampleDetailScale;

int _ShapeMarchingCount;
int _LightMarchingCount;
int _LargeStepThreshold;

float _DarknessThreshold;

float _ExtinctionCoefficient;
float _CloudScatter;

float _BlueNoiceScale;

float4 _WindDirection;
float _WindSpeed;

float _DensityOffset;
float _DetailScale;
float _DensityThreshold;
float _DensityMultiplier;

float _CloudCoverage;

int _TextureWidth;
int _TextureHeight;
int _CurrFrameCount;
CBUFFER_END

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
};


float2 SampleDensity(float3 currPos, bool enableDetail)
{
    const float densityScale = 0.01;

    float density = 0;
    // 复原体积云的范围信息
    float3 cloudCenter = (_CloudBoxMin + _CloudBoxMax).xyz * 0.5;
    float3 cloudSize = (_CloudBoxMax - _CloudBoxMin).xyz;

    float3 windOffset = _WindDirection.xyz * _WindSpeed * _Time.y * 0.001;
    float3 uvw = currPos * 0.0001 + windOffset * 0.5;

    // 获取体积云的基本形状
    float3 sampleShapeUV = uvw * _SampleShapeScale + _SampleShapeOffset.xyz;
    // 获取低频 Perlin-Worley 和 Worley 噪声
    float4 shapeNoice = SAMPLE_TEXTURE3D(_ShapeNoiceTex, sampler_ShapeNoiceTex, sampleShapeUV);
    // 计算不同频率 Worley 噪声所构成的 FBM 为基础形状添加细节
    // FBM 是一系列噪声的叠加，每一个噪声都有更高的频率和更低的幅度。
    float shapeFBM = dot(shapeNoice.gba, normalize(_ShapeWeights.rgb));
    // 使用 FBM 重映射体积云的密度
    density = Remap(shapeNoice.r, saturate(1 - shapeFBM), 1, 0, 1);

    // 获取高度百分比
    float heightGradient = GetDensityHeightGradient(currPos, _CloudBoxMin.y, _CloudBoxMax.y);

    // 形状变化因子
    // 圆化云的底部
    float roundButton = saturate(Remap(heightGradient, 0, 0.07, 0, 1));
    // 圆化云的顶部
    float roundTop = saturate(Remap(heightGradient, 0.2, 1, 1, 0));
    float roundFac = roundButton * roundTop;
    // 密度变化因子
    float densityButton = heightGradient * saturate(Remap(heightGradient, 0, _DensityOffset, 0, 1));
    float densityTop = saturate(Remap(heightGradient, 1 - _DensityOffset, 1, 1, 0));
    float densityFac = densityButton * densityTop * 2;

    density *= roundFac * densityFac;

    if(density > 0) {
        // 为体积云添加天气属性
        float2 weatherUV = (currPos.xz - cloudCenter.xz) / max(cloudSize.x, cloudSize.z);
        weatherUV += windOffset;
        // 获取天气纹理, r 通道存取体积云的覆盖百分比，g 通道存取云层降雨的可能性， b 通道存取云的类型
        float4 weatherTex = SAMPLE_TEXTURE2D(_WeatherNoiceTex, sampler_WeatherNoiceTex, weatherUV);
        // 云层覆盖率
        float cloudCoverage = weatherTex.r;
        cloudCoverage = lerp(cloudCoverage, 1, _CloudCoverage);
        cloudCoverage = lerp(0, cloudCoverage, _CloudCoverage);
        density *= cloudCoverage;

        if(density > 0 && enableDetail){
            // 为云添加细节
            float3 sampleDetailUV = uvw * _SampleDetailScale + _SampleDetailOffset.xyz;
            // 获取高频的 Worley 噪声
            float3 detailNoice = SAMPLE_TEXTURE3D(_DetailNoiceTex, sampler_DetailNoiceTex, sampleDetailUV).rgb;
            // 计算 Worley 噪声的FBM
            float detailFBM = dot(detailNoice, normalize(_DetailWeights.xyz));
            float detailErode = (1 - detailFBM) * densityScale * _DetailScale;
            density -= detailErode;
        }
    } 

    density = max(0, density - _DensityThreshold * densityScale) * _DensityMultiplier;

    return density * 0.5;
}

// 从采样点出发，沿光照方向进行raymarching
float LightMarching(float3 currPos, float3 lightDir)
{
    // 总密度
    float sumDensity = 0;
    float transmittance = 1;

    float3 invDir = 1 / lightDir;

    float3 insertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);
    float stepSize = insertInfo.z / _LightMarchingCount;
    float3 rayStep = lightDir * stepSize;
    if(insertInfo.x != 0){
        [loop]
        for(int i = 0; i < _LightMarchingCount; currPos += rayStep, ++i){
            float density = SampleDensity(currPos, true);
            sumDensity += density;
        }
    }

    // 计算透光率
    float absorbance = CalcuAbsorbance(_ExtinctionCoefficient, sumDensity, stepSize);
    // 可选择加入糖粉效应
    transmittance = BeerLambert(absorbance);

   	// 添加一个阈值来控制亮度
    return _DarknessThreshold + (1 - _DarknessThreshold) * transmittance;
}

// 从相机出发，沿观察方向进行raymarching
float4 VolumeCloudRaymarching(Ray viewRay, float3 lightDir, float linearDepth, float blueNoice)
{
    // 总密度
    float sumDensity = 0;
    // 透光率
    float transmittance = 1;
    // 光照强度
    float3 lightIntensity = 0;

    float3 currPos = viewRay.startPos;
    float3 rayDir = viewRay.dir;

    float cos = dot(rayDir, lightDir);
    float phase = HenyeyGreenstein(cos, _CloudScatter);

    // 相机视线与包围盒相交
    float3 invDir = 1 / rayDir;
    float3 insertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);

    // 未相交直接返回 0 光照强度
    if(insertInfo.x == 0) return float4(lightIntensity, transmittance);
    
	// 需要考虑再云内部的情况
    float enterPos = max(0, insertInfo.y);
    // 物体到相机的距离
    currPos += rayDir * enterPos;
    // 考虑物体遮挡情况，选取最小距离
    float marchingLimit = min(linearDepth, insertInfo.z) - enterPos;

    float stepSize = marchingLimit / _ShapeMarchingCount;
    float3 rayStep = rayDir * stepSize;

    // 蓝噪声优化
    float3 startOffset = (blueNoice - 0.5) * 2 * rayStep;
    currPos += startOffset * _BlueNoiceScale;

    // rayMarching优化
    float densityTest = 0;
    float preDensity = 0;
    int zeroDensityCount = 0;
    [loop]
    for(int i = 0; i < _ShapeMarchingCount; ++i){
        currPos += rayStep;

        // 检测是否进入大步进, 第一次进入大步进
        if(densityTest <= 0){   // 大步进
            densityTest = SampleDensity(currPos, false);

            if(densityTest > 0){
                currPos -= rayStep; // 回退一步，防止漏采样
                --i;
            }
            else{
                currPos += rayStep; // 额外步进一次
                ++i;
            }
        }
        else{   // 普通步进
            // 计算当前点的密度
            float density = SampleDensity(currPos, true);

            if(density > 0){
                zeroDensityCount = 0;
                
                float lightTransmittance = LightMarching(currPos, lightDir);
                lightIntensity += density * stepSize * transmittance * lightTransmittance;
                // 计算吸光率
                float absorbance = CalcuAbsorbance(_ExtinctionCoefficient, density, stepSize);
                transmittance *= BeerLambert(absorbance);
                sumDensity += density * stepSize;
                
                if(transmittance < 0.01) break;
            }
            else if(preDensity <= 0){
                ++zeroDensityCount;
            }

            // 0 密度次数达到阈值清空累计次数，进入大步进
            if(zeroDensityCount >= _LargeStepThreshold){
                zeroDensityCount = 0;
                densityTest = 0;
            }

            preDensity = density;
        }
    }
    
    lightIntensity *= phase;

    return float4(lightIntensity, transmittance);
}


v2f vertVolumeCloud(appdata v)
{
    v2f o;
    
    VertexPositionInputs vertPosInput = GetVertexPositionInputs(v.vertex.xyz);
    o.vertex = vertPosInput.positionCS;
    o.uv = v.uv;

    return o;
}

float4 fragVolumeCloud(v2f i) : SV_Target
{
#ifndef _FrameBlockOFF
    // 获取背景颜色
    float4 preColor = SAMPLE_TEXTURE2D(_CloudBackTex, sampler_CloudBackTex, i.uv);
    #ifdef _FrameBlock2X2
    int blockCount = 2;
    #elif _FrameBlock4X4
    int blockCount = 4;
    #endif

    int index = GetPixelIndex(i.uv, _TextureWidth, _TextureHeight, blockCount);

    if(index != _CurrFrameCount % (blockCount * blockCount)) return preColor;
#endif

    // 获取深度
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv).x;
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    
    // 获取当前的位置
    // _ScreenParams的xy纹理宽度和高度，z分量是1.0 + 1.0/宽度，w为1.0 + 1.0/高度。
    float2 posSS = i.vertex.xy * (_ScreenParams.zw - 1);
    float4 posNDC = float4(posSS * 2 - 1, depth, 1);
#if UNITY_UV_STARTS_AT_TOP
    posNDC.y *= -1;
#endif
    
#if REQUIRE_POSITION_VS
    float4 positionVS = mul(UNITY_MATRIX_I_P, posNDC);
    positionVS /= positionVS.w;
    float4 posW = mul(UNITY_MATRIX_I_V, positionVS);
#else
    float4 posW = mul(UNITY_MATRIX_I_VP, posNDC);
    posW /= posW.w;
#endif

    Light mainLight = GetMainLight();
    float3 lightDir = normalize(mainLight.direction);

    float blueNoiceOffset = SAMPLE_TEXTURE2D(_BlueNoiceTex, sampler_BlueNoiceTex, i.uv).r;

    float3 cameraPosW = GetCameraPositionWS();
    Ray ray;
    ray.startPos = cameraPosW;
    ray.dir = normalize(posW.xyz - cameraPosW);
    float4 cloud = 0;
    cloud = VolumeCloudRaymarching(ray, lightDir, linearDepth, blueNoiceOffset);
    if(lightDir.y < 0.1){
        float cos = dot(lightDir, float3(0, -1, 0));
        cloud *= saturate(0.4 - cos);
    }
    cloud.rgb *= mainLight.color;

    //return float4(preColor.rgb * cloud.a + cloud.rgb, preColor.a);
    return cloud;
}



TEXTURE2D(_BlendCloudTex);
TEXTURE2D(_BackTex);
SAMPLER(sampler_BlendCloudTex);
SAMPLER(sampler_BackTex);

v2f vertBlendCloud(appdata v)
{
    v2f o;
    VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
    o.vertex = vertexPos.positionCS;
    o.uv = v.uv;
    return o;
}

float4 fragBlendCloud(v2f i): SV_Target
{
    float4 cloud = SAMPLE_TEXTURE2D(_BlendCloudTex, sampler_BlendCloudTex, i.uv);
    float4 back = SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, i.uv);
    return float4(back.rgb * cloud.a + cloud.rgb, back.a);
}

#endif