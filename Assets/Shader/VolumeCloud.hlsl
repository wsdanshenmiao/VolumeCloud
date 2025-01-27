#ifndef __VOLUMECLOUD__HLSL__
#define __VOLUMECLOUD__HLSL__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_BackgroundTex);
TEXTURE2D(_CameraDepthTexture);

SAMPLER(sampler_BackgroundTex);
SAMPLER(sampler_CameraDepthTexture);

CBUFFER_START(UnityPreMaterial)
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
    float3 viewDir : TEXCOORD1;
};

v2f vertVolumeCloud(appdata v)
{
    v2f o;
    
    VertexPositionInputs vertPosInput = GetVertexPositionInputs(v.vertex.xyz);
    o.vertex = vertPosInput.positionCS;
    o.uv = v.uv;

    // 先变换到NDC空间，后乘观察空间到投影的逆矩阵变换到观察空间
    float3 viewDir = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, 0)).xyz;
    o.viewDir = mul(unity_CameraToWorld, float4(viewDir, 0)).xyz;

    return o;
}

float4 fragVolumeCloud(v2f i) : SV_Target
{
    // 获取当前的位置
    float3 viewDir = normalize(i.viewDir);
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    float3 cameraPosW = GetCameraPositionWS();
    float3 posW = cameraPosW + viewDir * linearDepth;

    float3 cloudCol = 1;

    // 获取背景颜色
    float4 preColor = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, i.uv);

    return float4(preColor.rgb * cloudCol, preColor.a);
}

#endif