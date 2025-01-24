#ifndef __VOLUMECLOUD__HLSL__
#define __VOLUMECLOUD__HLSL__

#include "VolumeCloudUtil.hlsl"

TEXTURE2D(_MainTex);
TEXTURE2D( _CameraDepthTexture);

SAMPLER(sampler_CameraDepthTexture);
SAMPLER(sampler_MainTex);

float4 _MainTex_TexelSize;
float4x4 _FrustumCorners;

struct volumeCloudData
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct volumeCloudV2f
{
    float4 vertex : SV_POSITION;
    float4 uv : TEXCOORD0;
    float3 interpolatedRay : TEXCOORD1;
};

volumeCloudV2f vertVolumeCloud (volumeCloudData v)
{
    volumeCloudV2f o;
    VertexPositionInputs positionInputs = GetVertexPositionInputs(v.vertex.xyz);
    o.vertex = positionInputs.positionCS;

    float2 uv = v.uv;
    o.uv = v.uv.xyxy;

    int index = 0;
    if(uv.x < 0.5 && uv.y < 0.5){   // 左下角
        index = 0;
    }
    else if(uv.x < 0.5 && uv.y >= 0.5){ // 左上角
        index = 1;
    }
    else if(uv.x >= 0.5 && uv.y >= 0.5){  // 右上角
        index = 2;
    }
    else{
        index = 3;
    }

    // 消除平台差异
    #if UNITY_UV_STARTS_AT_TOP
    if(_MainTex_TexelSize.y < 0){
        o.uv.zw.y = 1 - o.uv.zw.y;
        index = 3 - index;
    }
    #endif
    // 不需要归一化，否则插值后会出现问题
    o.interpolatedRay = _FrustumCorners[index].xyz;

    return o;
}

float4 fragVolumeCloud (volumeCloudV2f i) : SV_Target
{
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv.zw);
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    float3 posW = _WorldSpaceCameraPos.xyz + i.interpolatedRay * linearDepth; // 复原世界坐标
    float3 dir = normalize(posW - _WorldSpaceCameraPos.xyz);

    float3 cloud = VolumeCloudRayMarching(_WorldSpaceCameraPos.xyz, dir, posW);
    
    float4 preCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
    preCol = float4(preCol.xyz + cloud.xyz, preCol.a);

    return preCol;
}


#endif