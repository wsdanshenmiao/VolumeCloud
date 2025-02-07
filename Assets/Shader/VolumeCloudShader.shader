Shader "DSMRender/VolumeCloud"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        HLSLINCLUDE
        #pragma multi_compile _FrameBlockOFF _FrameBlock2X2 _FrameBlock4X4
        #include "VolumeCloud.hlsl"
        ENDHLSL

        Pass
        {
            Name "VolumeCloud"
            HLSLPROGRAM
            #include "VolumeCloud.hlsl"
            #pragma vertex vertVolumeCloud
            #pragma fragment fragVolumeCloud
            ENDHLSL
        }

        Pass
        {
            Blend Off
            Name "BlendVolumeCloud"
            HLSLPROGRAM
            #pragma vertex vertBlendCloud
            #pragma fragment fragBlendCloud
            ENDHLSL
        }
    }
}
