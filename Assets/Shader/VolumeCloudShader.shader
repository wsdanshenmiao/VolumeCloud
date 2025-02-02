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

        Pass
        {
            Name "BlendVolumeCloud"
            
            HLSLPROGRAM
            
            #pragma vertex vertBlendCloud
            #pragma fragment fragBlendCloud
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BlendCloudTex);
            TEXTURE2D(_BackTex);
            SAMPLER(sampler_BlendCloudTex);
            SAMPLER(sampler_BackTex);
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex: SV_POSITION;
                float2 uv: TEXCOORD0;
            };
            
            v2f vertBlendCloud(appdata v)
            {
                v2f o;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexPos.positionCS;
                o.uv = v.uv;
                return o;
            }
            
            float4 fragBlendCloud(v2f i): SV_Target
            {
                float4 cloud = SAMPLE_TEXTURE2D(_BlendCloudTex, sampler_BlendCloudTex, i.uv);
                float4 back = SAMPLE_TEXTURE2D(_BackTex, sampler_BackTex, i.uv);
                return float4(back.rgb * cloud.a + cloud.rgb, back.a);
            }
            
            ENDHLSL
        }
    }
}
