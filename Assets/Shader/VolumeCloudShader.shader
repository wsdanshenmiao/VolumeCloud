Shader "DSMRender/VolumeCloud"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "VolumeCloud"

            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            #include "VolumeCloud.hlsl"
            #pragma vertex vertVolumeCloud
            #pragma fragment fragVolumeCloud
            ENDHLSL
        }
    }
}
