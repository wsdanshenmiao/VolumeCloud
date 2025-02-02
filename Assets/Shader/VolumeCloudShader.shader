Shader "DSMRender/VolumeCloud"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "VolumeCloud"


            HLSLPROGRAM
            #include "VolumeCloud.hlsl"
            #pragma vertex vertVolumeCloud
            #pragma fragment fragVolumeCloud
            #pragma multi_compile _FrameBlockOFF _FrameBlock2X2 _FrameBlock4X4
            ENDHLSL
        }
    }
}
