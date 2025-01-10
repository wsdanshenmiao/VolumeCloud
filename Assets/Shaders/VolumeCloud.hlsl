#ifndef __VOLUMECLOUD__HLSL__
#define __VOLUMECLOUD__HLSL__

#include "UnityCG.cginc"
#include "VolumeCloudUtil.hlsl"

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

sampler2D _MainTex;
sampler2D _CameraDepthTexture;
float4 _MainTex_TexelSize;
float4x4 _FrustumCorners;

volumeCloudV2f vertVolumeCloud (volumeCloudData v)
{
    volumeCloudV2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
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
    float linearDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.zw));
    float3 posW = _WorldSpaceCameraPos.xyz + i.interpolatedRay * linearDepth; // 复原世界坐标
    float3 dir = normalize(posW - _WorldSpaceCameraPos.xyz);
    float3 cloud = VolumeCloudRayMarching(_WorldSpaceCameraPos.xyz, dir, posW);
    float4 preCol = tex2D(_MainTex, i.uv.xy);
    return float4(preCol.xyz + cloud.xyz, preCol.a);
}


#endif