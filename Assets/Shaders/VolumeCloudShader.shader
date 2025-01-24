Shader "DSMRenderPipeline/VolumeCloudShader"
{
    Properties
    {
        _RayMarchingStride("步进步频", Range(0.2, 1)) = 0.5
        _CloudBoxMin("体积云的范围最小值", vector) = (-1,-1,-1,0)
        _CloudBoxMax("体积云的范围最大值", vector) = (1,1,1,1)
        _NoiceTexScale("纹理坐标的缩放", vector) = (1,1,1,1)
        _NoiceSampleOffset("采样噪声图的偏移", vector) = (0,0,0,0)
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
