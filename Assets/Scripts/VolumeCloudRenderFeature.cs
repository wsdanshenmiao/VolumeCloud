
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum FrameBlock{
    _OFF = 1, _2X2 = 4, _4X4 = 16
}

public class VolumeCloudFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class VolumeCloudSetting
    {
        public bool m_EnableInEdit = false;
        public RenderPassEvent m_RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader m_VolumeCloudShader;
        public FrameBlock m_FrameBlock = FrameBlock._2X2;
    }

    private VolumeCloudRenderPass m_VolumeCloudRenderPass;

    public VolumeCloudSetting m_VolumeCloudSetting;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bool disableEdit = !m_VolumeCloudRenderPass.EnableEdit;
        if (renderingData.cameraData.cameraType != CameraType.Game && disableEdit) return;
        renderer.EnqueuePass(m_VolumeCloudRenderPass);
    }

    public override void Create()
    {
        name = "VolumeCloud";
        m_VolumeCloudRenderPass = new VolumeCloudRenderPass(m_VolumeCloudSetting);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        bool disableEdit = !m_VolumeCloudRenderPass.EnableEdit;
        if (renderingData.cameraData.cameraType != CameraType.Game && disableEdit) return;
        m_VolumeCloudRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        m_VolumeCloudRenderPass.SetUp(renderer.cameraColorTargetHandle);
    }
}