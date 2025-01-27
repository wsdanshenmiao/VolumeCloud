using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudRenderPass : ScriptableRenderPass
{
    private VolumeCloudParamer m_VolumeCloudParamer;
    private Material m_VolumeCloudMat;
    private RTHandle m_RenderTarget;
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("VolumeCloud");

    public bool EnableEdit => 
        m_VolumeCloudParamer == null ? false : m_VolumeCloudParamer.m_EnableInEdit.value;

    public VolumeCloudRenderPass(Shader volumeCloudShader, RenderPassEvent renderPassEvent)
    {
        if(volumeCloudShader == null){
            Debug.LogError("VolumeCloud Shader Should Be Set.");
            return;
        }

        this.renderPassEvent = renderPassEvent;
        m_VolumeCloudMat = CoreUtils.CreateEngineMaterial(volumeCloudShader);
    }

    public void SetUp(RTHandle renderTarget)
    {
        m_RenderTarget = renderTarget;
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
        using (new ProfilingScope(cmd, m_ProfilingSampler)) {
            Render(cmd);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    private void Render(CommandBuffer cmd)
    {
        m_VolumeCloudMat.SetTexture("_BackgroundTex", m_RenderTarget);
        m_VolumeCloudMat.SetFloat("_RayMarchingStride", m_VolumeCloudParamer.m_RayMarchingStride.value);

        cmd.Blit(m_RenderTarget, m_RenderTarget, m_VolumeCloudMat, 0);
    }
}