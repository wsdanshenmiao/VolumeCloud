#ifndef __VOLUMECLOUD__HLSL__
#define __VOLUMECLOUD__HLSL__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "VolumeCloudUtil.hlsl"

TEXTURE2D(_BackgroundTex);
TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_WeatherNoiceTex);

TEXTURE3D(_ShapeNoiceTex);
TEXTURE3D(_DetailNoiceTex);

SAMPLER(sampler_BackgroundTex);
SAMPLER(sampler_CameraDepthTexture);
SAMPLER(sampler_WeatherNoiceTex);
SAMPLER(sampler_ShapeNoiceTex);
SAMPLER(sampler_DetailNoiceTex);

CBUFFER_START(UnityPreMaterial)
float4 _CloudBoxMin;
float4 _CloudBoxMax;

float4 _ShapeWeights;
float4 _DetailWeights;

float4 _SampleNoiceOffset;
float _SampleNoiceScale;

int _ShapeMarchingCount;
int _LightMarchingCount;

float _DarknessThreshold;

float _ExtinctionCoefficient;

float _DensityOffset;
float _DensityThreshold;
float _DensityMultiplier;
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


float GetDensity(float3 currPos)
{
    float density = 0;
    // 复原体积云的范围信息
    float3 cloudCenter = (_CloudBoxMin + _CloudBoxMax).xyz * 0.5;
    float3 cloudSize = (_CloudBoxMax - _CloudBoxMin).xyz;

    // 边缘的衰减
    float edgeFadeDistance = 100;
    float distanceX = min(edgeFadeDistance, min(_CloudBoxMax.x - currPos.x, currPos.x - _CloudBoxMin.x));
    float distanceZ = min(edgeFadeDistance, min(_CloudBoxMax.z - currPos.z, currPos.z - _CloudBoxMin.z));
    float edgeFade = min(distanceX, distanceZ) / edgeFadeDistance;


    // 获取体积云的基本形状
    float3 uvw = (currPos + _SampleNoiceOffset.xyz) * 0.001 * _SampleNoiceScale;
    // 获取低频 Perlin-Worley 和 Worley 噪声
    float4 shapeNoice = SAMPLE_TEXTURE3D(_ShapeNoiceTex, sampler_ShapeNoiceTex, uvw);
    // 计算不同频率 Worley 噪声所构成的 FBM 为基础形状添加细节
    // FBM 是一系列噪声的叠加，每一个噪声都有更高的频率和更低的幅度。
    float shapeFBM = dot(shapeNoice.gba, _ShapeWeights.gba);
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
    float densityButton = heightGradient * saturate(Remap(heightGradient, 0, 0.15, 0, 1));
    float densityTop = saturate(Remap(heightGradient, 0.9, 1, 1, 0));
    float densityFac = densityButton * densityTop * 2;

    density *= roundFac * densityFac;

    if(density > 0){
        // 为体积云添加天气属性
        float2 weatherUV = (currPos.xz - cloudCenter.xz) / max(cloudSize.x, cloudSize.z);
        // 获取天气纹理, r 通道存取体积云的覆盖百分比，g 通道存取云层降雨的可能性， b 通道存取云的类型
        float4 weatherTex = SAMPLE_TEXTURE2D(_WeatherNoiceTex, sampler_WeatherNoiceTex, weatherUV);
        // 云层覆盖率
        float cloudCoverage = weatherTex.r;
        // density = Remap(density, cloudCoverage, 1, 0, 1);
        density *= cloudCoverage * weatherTex.g;

        if(density > 0){
            // 为云添加细节
            // 获取高频的 Worley 噪声
            float3 detailNoice = SAMPLE_TEXTURE3D(_DetailNoiceTex, sampler_DetailNoiceTex, uvw).rgb;
            // 计算 Worley 噪声的FBM
            float detailFBM = dot(detailNoice, _DetailWeights.xyz);
            density = Remap(density, detailFBM * 0.01, 1, 0, 1);
        }
    } 

    density = max(0, density - _DensityThreshold * 0.01) * _DensityMultiplier;

    return density;
}

// 从采样点出发，沿光照方向进行raymarching
float LightMarching(float3 currPos, float3 lightDir)
{
    // 总密度
    float3 sumDensity = 0;
    float transmittance = 1;

    float3 invDir = 1 / lightDir;

    float3 insertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);
    if(insertInfo.x != 0){
        float stepSize = insertInfo.z / _LightMarchingCount;
        float3 rayStep = lightDir * stepSize;
        [loop]
        for(int i = 0; i < _LightMarchingCount; currPos += rayStep, ++i){
            float density = GetDensity(currPos) * stepSize;
            sumDensity += density;
        }
    }

    // 计算透光率
    float absorbance = CalcuAbsorbance(_ExtinctionCoefficient, sumDensity, insertInfo.z);
    transmittance = BeerLambert(absorbance);

    return _DarknessThreshold + (1 - _DarknessThreshold) * transmittance;
}

// 从相机出发，沿观察方向进行raymarching
float3 VolumeCloudRaymarching(Ray viewRay, float linearDepth, out float transmittance)
{
    // 总密度
    float sumDensity = 0;
    // 透光率
    transmittance = 1;
    // 光照强度
    float3 lightIntensity = 0;

    float3 currPos = viewRay.startPos;
    float3 rayDir = viewRay.dir;

    float3 invDir = 1 / rayDir;

    Light mainLight = GetMainLight();
    float3 lightDir = normalize(mainLight.direction);

    // 相机视线与包围盒相交
    float3 insertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);
    [flatten]
    if(insertInfo.x != 0){
        float enterPos = max(0, insertInfo.y);
        // 物体到相机的距离
        currPos += rayDir * enterPos;
        // 考虑物体遮挡情况，选取最小距离
        float marchingLimit = min(linearDepth, insertInfo.z) - enterPos;

        float stepSize = marchingLimit / _ShapeMarchingCount;
        float3 rayStep = rayDir * stepSize;

        [loop]
        for(int i = 0; i < _ShapeMarchingCount; ++i){
            currPos += rayStep;
            // 计算当前点的密度
            float density = GetDensity(currPos);

            if(density > 0){
                lightIntensity += density * stepSize * transmittance * LightMarching(currPos, lightDir);
                // 计算吸光率
                float absorbance = CalcuAbsorbance(_ExtinctionCoefficient, density, stepSize);
                transmittance *= BeerLambert(absorbance);
                sumDensity += density;
                
                if(transmittance < 0.01) break;
            }
        }
    }

    return lightIntensity;
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
    // 获取深度
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv).x;
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    
    // 获取当前的位置
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

    float3 cameraPosW = GetCameraPositionWS();
    float transmittance = 1;
    Ray ray;
    ray.startPos = cameraPosW;
    ray.dir = normalize(posW.xyz - cameraPosW);
    float3 cloudCol = VolumeCloudRaymarching(ray, linearDepth, transmittance);

    // 获取背景颜色
    float4 preColor = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, i.uv);

    return float4(preColor.rgb * transmittance + cloudCol, preColor.a);
}


#endif