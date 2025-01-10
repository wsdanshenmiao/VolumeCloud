Shader "DSMRenderPipeline/VolumeCloudShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #include "VolumeCloud.hlsl"
            #pragma vertex vertVolumeCloud
            #pragma fragment fragVolumeCloud
            ENDHLSL
        }
    }
}
