using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudRenderPass : ScriptableRenderPass
{
    private VolumeCloudParamer m_VolumeCloudParamer;
    private Material m_VolumeCloudMat;
    private RTHandle m_RenderTarget;
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("VolumeCloud");
    private FrameBlock m_FrameBlock;
    private RenderTexture[] m_CloudTex;

    private int m_FrameCount = 0;

    public bool EnableEdit => 
        m_VolumeCloudParamer == null ? false : m_VolumeCloudParamer.m_EnableInEdit.value;

    public VolumeCloudRenderPass(VolumeCloudFeature.VolumeCloudSetting setting)
    {
        if(setting.m_VolumeCloudShader == null){
            Debug.LogError("VolumeCloud Shader Should Be Set.");
            return;
        }

        this.renderPassEvent = setting.m_RenderPassEvent;
        this.m_FrameBlock = setting.m_FrameBlock;
        this.m_VolumeCloudMat = setting.m_VolumeCloudMat;
    }

    public void SetUp(RTHandle renderTarget)
    {
        m_RenderTarget = renderTarget;
    }

    public void SetCloudTex(ref RenderTexture[] cloudTex)
    {
        m_CloudTex = cloudTex;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 获取后处理组件
        m_VolumeCloudParamer = VolumeManager.instance.stack.GetComponent<VolumeCloudParamer>();

        bool showInEdit = renderingData.cameraData.cameraType == CameraType.Game ||
            m_VolumeCloudParamer.m_EnableInEdit.value;
        if(!renderingData.cameraData.postProcessEnabled || 
            m_VolumeCloudParamer == null ||
            m_VolumeCloudMat == null ||
            !showInEdit) return;

        CommandBuffer cmd = CommandBufferPool.Get();
        // 添加一个分析项，方便在帧调试器中定位
        using (new ProfilingScope(cmd, m_ProfilingSampler)) {
            Render(cmd, renderingData);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    private void Render(CommandBuffer cmd, RenderingData renderingData)
    {
        Vector3 cloudCenter = m_VolumeCloudParamer.m_CloudBoxCenter.value;
        Vector3 cloudSize = m_VolumeCloudParamer.m_CloudBoxSize.value;
        Vector3 cloudBoxMin = cloudCenter - cloudSize * .5f;
        Vector3 cloudBoxMax = cloudCenter + cloudSize * .5f;

        int textureWidth = renderingData.cameraData.camera.pixelWidth;
        int textureHeight = renderingData.cameraData.camera.pixelHeight;

        if(m_FrameBlock == FrameBlock._OFF){
            m_VolumeCloudMat.EnableKeyword("_FrameBlockOFF");
            m_VolumeCloudMat.DisableKeyword("_FrameBlock2X2");
            m_VolumeCloudMat.DisableKeyword("_FrameBlock4X4");
        }
        else if(m_FrameBlock == FrameBlock._2X2){
            m_VolumeCloudMat.DisableKeyword("_FrameBlockOFF");
            m_VolumeCloudMat.EnableKeyword("_FrameBlock2X2");
            m_VolumeCloudMat.DisableKeyword("_FrameBlock4X4");
        }
        else if(m_FrameBlock == FrameBlock._4X4){
            m_VolumeCloudMat.DisableKeyword("_FrameBlockOFF");
            m_VolumeCloudMat.DisableKeyword("_FrameBlock2X2");
            m_VolumeCloudMat.EnableKeyword("_FrameBlock4X4");
        }

        m_VolumeCloudMat.SetTexture("_ShapeNoiceTex", m_VolumeCloudParamer.m_ShapeNoiceTex.value);
        m_VolumeCloudMat.SetTexture("_DetailNoiceTex", m_VolumeCloudParamer.m_DetailNoiceTex.value);
        m_VolumeCloudMat.SetTexture("_BlueNoiceTex", m_VolumeCloudParamer.m_BlueNoiceTex.value);
        m_VolumeCloudMat.SetTexture("_WeatherNoiceTex", m_VolumeCloudParamer.m_WeatherNoiceTex.value);

        m_VolumeCloudMat.SetInt("_ShapeMarchingCount", m_VolumeCloudParamer.m_ShapeMarchingCount.value);
        m_VolumeCloudMat.SetInt("_LightMarchingCount", m_VolumeCloudParamer.m_LightMarchingCount.value);
        m_VolumeCloudMat.SetInt("_LargeStepThreshold", m_VolumeCloudParamer.m_LargeStepThreshold.value);
        m_VolumeCloudMat.SetInt("_TextureWidth", textureWidth);
        m_VolumeCloudMat.SetInt("_TextureHeight", textureHeight);
        m_VolumeCloudMat.SetInt("_CurrFrameCount", m_FrameCount);
        
        m_VolumeCloudMat.SetFloat("_SampleShapeScale", m_VolumeCloudParamer.m_SampleShapeScale.value);
        m_VolumeCloudMat.SetFloat("_SampleDetailScale", m_VolumeCloudParamer.m_SampleDetailScale.value);
        m_VolumeCloudMat.SetFloat("_DarknessThreshold", m_VolumeCloudParamer.m_DarknessThreshold.value);
        m_VolumeCloudMat.SetFloat("_ExtinctionCoefficient", m_VolumeCloudParamer.m_ExtinctionCoefficient.value);
        m_VolumeCloudMat.SetFloat("_DensityOffset", m_VolumeCloudParamer.m_DensityOffset.value);
        m_VolumeCloudMat.SetFloat("_DensityThreshold", m_VolumeCloudParamer.m_DensityThreshold.value);
        m_VolumeCloudMat.SetFloat("_DensityMultiplier", m_VolumeCloudParamer.m_DensityMultiplier.value);
        m_VolumeCloudMat.SetFloat("_DetailScale", m_VolumeCloudParamer.m_DetailScale.value);
        m_VolumeCloudMat.SetFloat("_CloudScatter", m_VolumeCloudParamer.m_CloudScatter.value);
        m_VolumeCloudMat.SetFloat("_BlueNoiceScale", m_VolumeCloudParamer.m_BlueNoiceScale.value);
        m_VolumeCloudMat.SetFloat("_WindSpeed", m_VolumeCloudParamer.m_WindSpeed.value);
        m_VolumeCloudMat.SetFloat("_CloudCoverage", m_VolumeCloudParamer.m_CloudCoverage.value);

        m_VolumeCloudMat.SetVector("_SampleShapeOffset", m_VolumeCloudParamer.m_SampleShapeOffset.value);
        m_VolumeCloudMat.SetVector("_SampleDetailOffset", m_VolumeCloudParamer.m_SampleDetailOffset.value);
        m_VolumeCloudMat.SetVector("_CloudBoxMin", cloudBoxMin);
        m_VolumeCloudMat.SetVector("_ShapeWeights", m_VolumeCloudParamer.m_ShapeWeights.value);
        m_VolumeCloudMat.SetVector("_DetailWeights", m_VolumeCloudParamer.m_DetailWeights.value);
        m_VolumeCloudMat.SetVector("_CloudBoxMax", cloudBoxMax);
        m_VolumeCloudMat.SetVector("_WindDirection", m_VolumeCloudParamer.m_WindDirection.value.normalized);

        if(m_FrameBlock == FrameBlock._OFF){
            RenderTexture tmpTex = RenderTexture.GetTemporary(textureWidth, textureHeight);

            cmd.Blit(m_RenderTarget, tmpTex, m_VolumeCloudMat, 0);
            m_VolumeCloudMat.SetTexture("_BackTex", m_RenderTarget);
            m_VolumeCloudMat.SetTexture("_BlendCloudTex", tmpTex);
            cmd.Blit(tmpTex, m_RenderTarget, m_VolumeCloudMat, 1);

            RenderTexture.ReleaseTemporary(tmpTex);
        }
        else{
            int index1 = m_FrameCount % 2;
            int index2 = (m_FrameCount + 1) % 2;
            m_VolumeCloudMat.SetTexture("_CloudBackTex", m_CloudTex[index1]);
            cmd.Blit(m_CloudTex[index1], m_CloudTex[index2], m_VolumeCloudMat, 0);
            m_VolumeCloudMat.SetTexture("_BackTex", m_RenderTarget);
            m_VolumeCloudMat.SetTexture("_BlendCloudTex", m_CloudTex[index2]);
            cmd.Blit(m_CloudTex[index2], m_RenderTarget, m_VolumeCloudMat, 1);
        }

        ++m_FrameCount;
    }
}