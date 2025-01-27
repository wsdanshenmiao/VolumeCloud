
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class VolumeCloudFeature : ScriptableRendererFeature
{
    public bool m_EnableInEdit;
    public RenderPassEvent m_RenderPassEvent;
    public Shader m_VolumeCloudShader;

    private VolumeCloudRenderPass m_VolumeCloudRenderPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bool disableEdit = !m_VolumeCloudRenderPass.EnableEdit;
        if (renderingData.cameraData.cameraType != CameraType.Game && disableEdit) return;
        renderer.EnqueuePass(m_VolumeCloudRenderPass);
    }

    public override void Create()
    {
        name = "VolumeCloud";
        m_VolumeCloudRenderPass = new VolumeCloudRenderPass(m_VolumeCloudShader, m_RenderPassEvent);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        bool disableEdit = !m_VolumeCloudRenderPass.EnableEdit;
        if (renderingData.cameraData.cameraType != CameraType.Game && disableEdit) return;
        m_VolumeCloudRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        m_VolumeCloudRenderPass.SetUp(renderer.cameraColorTargetHandle);
    }
}