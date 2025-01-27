#ifndef __VOLUMECLOUD__HLSL__
#define __VOLUMECLOUD__HLSL__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "VolumeCloudUtil.hlsl"

TEXTURE2D(_BackgroundTex);
TEXTURE2D(_CameraDepthTexture);

SAMPLER(sampler_BackgroundTex);
SAMPLER(sampler_CameraDepthTexture);

CBUFFER_START(UnityPreMaterial)
float4 _CloudBoxMin;
float4 _CloudBoxMax;
float _RayMarchingStride;
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


float3 GetDensity()
{

}

float3 VolumeCloudRaymarching(Ray viewRay, float linearDepth)
{
    float3 sumDensity = 0;
    float3 currPos = viewRay.startPos;
    float3 rayDir = viewRay.dir;
    float3 rayStep = _RayMarchingStride * rayDir;

    float3 invDir = 1 / rayDir;

    // 相机视线与包围盒相交
    float3 insertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);
    [flatten]
    if(insertInfo.x != 0){
        // 物体到相机的距离
        currPos += rayDir * insertInfo.y;
        // 考虑物体遮挡情况，选取最小距离
        float marchingLimit = min(linearDepth, insertInfo.z) - insertInfo.y;

        for(float i = 0; i < marchingLimit; currPos += rayStep, i += _RayMarchingStride){
            sumDensity += GetDensity() * _RayMarchingStride;
        }
    }

    return sumDensity;
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
    float3 posNDC = float4(posSS * 2 - 1, depth, 1);
    #if UNITY_UV_STARTS_AT_TOP
    posNDC.y *= -1;
    #endif
    
    #if REQUIRE_POSITION_VS
        float4 positionVS = mul(UNITY_MATRIX_I_P, float4(posNDC, 1));
        positionVS /= positionVS.w;
        float4 posW = mul(UNITY_MATRIX_I_V, positionVS);
    #else
        float4 posW = mul(UNITY_MATRIX_I_VP, float4(posNDC, 1));
        posW /= posW.w;
    #endif

    float3 cameraPosW = GetCameraPositionWS();
    Ray ray;
    ray.startPos = cameraPosW;
    ray.dir = normalize(posW.xyz - cameraPosW);
    float3 cloudCol = VolumeCloudRaymarching(ray, linearDepth);

    // 获取背景颜色
    float4 preColor = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, i.uv);

    return float4(preColor.rgb + cloudCol, preColor.a);
}


#endif