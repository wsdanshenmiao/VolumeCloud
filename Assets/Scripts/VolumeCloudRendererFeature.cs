using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitRendererFeature : ScriptableRendererFeature{

    public Shader m_VolumeCloudShader;

    private Material m_VolumeCloudMat;
    private VolumeCloudPass m_RenderPass = null;

    // 基类的抽象函数 OnEnable和OnValidate时调用
    public override void Create() {
        this.name = "VolumCloud";
        // 创建一个附带shader的material
        m_VolumeCloudMat = CoreUtils.CreateEngineMaterial(m_VolumeCloudShader);
        // 创建BiltPass脚本实例
        m_RenderPass = new VolumeCloudPass(m_VolumeCloudShader);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if(renderingData.cameraData.cameraType == CameraType.Game){
            renderer.EnqueuePass(m_RenderPass);
        }
    } 

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) {
        if(renderingData.cameraData.cameraType == CameraType.Game){
            // 设置向pass输入color (m_RenderPass父类)
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            // 设置RT为相机的color
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle);
        }
    }

    protected override void Dispose(bool disposing) {
        CoreUtils.Destroy(m_VolumeCloudMat);
    }
}

