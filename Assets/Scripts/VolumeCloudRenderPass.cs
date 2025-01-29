using System;
using Unity.Mathematics;
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
        Vector3 cloudCenter = m_VolumeCloudParamer.m_CloudBoxCenter.value;
        Vector3 cloudSize = m_VolumeCloudParamer.m_CloudBoxSize.value;
        Vector3 cloudBoxMin = cloudCenter + cloudSize * .5f;
        Vector3 cloudBoxMax = cloudCenter - cloudSize * .5f;

        m_VolumeCloudMat.SetTexture("_BackgroundTex", m_RenderTarget);
        m_VolumeCloudMat.SetTexture("_ShapeNoiceTex", m_VolumeCloudParamer.m_ShapeNoiceTex.value);
        m_VolumeCloudMat.SetTexture("(_DetailNoiceTex", m_VolumeCloudParamer.m_DetailNoiceTex.value);

        m_VolumeCloudMat.SetFloat("_SampleNoiceScale", m_VolumeCloudParamer.m_SampleNoiceScale.value);
        m_VolumeCloudMat.SetFloat("_DarknessThreshold", m_VolumeCloudParamer.m_DarknessThreshold.value);
        m_VolumeCloudMat.SetFloat("_ExtinctionCoefficient", m_VolumeCloudParamer.m_ExtinctionCoefficient.value);
        m_VolumeCloudMat.SetFloat("_RayMarchingStride", m_VolumeCloudParamer.m_RayMarchingStride.value);
        m_VolumeCloudMat.SetFloat("_DensityOffset", m_VolumeCloudParamer.m_DensityOffset.value);
        m_VolumeCloudMat.SetFloat("_DensityThreshold", m_VolumeCloudParamer.m_DensityThreshold.value);
        m_VolumeCloudMat.SetFloat("_DensityMultiplier", m_VolumeCloudParamer.m_DensityMultiplier.value);

        m_VolumeCloudMat.SetVector("_SampleNoiceOffset", m_VolumeCloudParamer.m_SampleNoiceOffset.value);
        m_VolumeCloudMat.SetVector("_CloudBoxMin", cloudBoxMin);
        m_VolumeCloudMat.SetVector("_ShapeWeights", m_VolumeCloudParamer.m_ShapeWeights.value);
        m_VolumeCloudMat.SetVector("_CloudBoxMax", cloudBoxMax);

        cmd.Blit(m_RenderTarget, m_RenderTarget, m_VolumeCloudMat, 0);
    }
}