using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudPass : ScriptableRenderPass
{
    private VolumeCloud m_VolumeCloud;
    private Material m_VolumeCloudMat;
    // RTHandle，封装了纹理及相关信息，可以认为是CPU端纹理
    private RTHandle m_CameraTarget;
    // 给profiler入一个新的事件
    private ProfilingSampler m_ProfilingSamper = new ProfilingSampler("VolumeCloud");

    public VolumeCloudPass(Shader shader)
    {
        m_VolumeCloudMat = CoreUtils.CreateEngineMaterial(shader);
        // 指定执行Pass的时机
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    // 指定进行后处理的target
    public void SetTarget(RTHandle colorHandle) {
        m_CameraTarget = colorHandle;
    }
    
    // OnCameraSetup是纯虚函数，相机初始化时调用
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) 
    {
        // (父类函数)指定pass的render target
        ConfigureTarget(m_CameraTarget);
    }

    // 提交渲染指令
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;
        m_VolumeCloud = VolumeManager.instance.stack.GetComponent<VolumeCloud>();
        // 只在游戏相机中进行后处理渲染
        if (renderingData.cameraData.cameraType != CameraType.Game ||
            m_VolumeCloudMat == null ||
            !renderingData.cameraData.postProcessEnabled ||
            m_VolumeCloud == null ||
            !m_VolumeCloud.IsActive()) return;

        CommandBuffer cmd = CommandBufferPool.Get();
        // 把cmd里执行的命令添加到m_ProfilingSampler定义的profiler块中
        using(new ProfilingScope(cmd, m_ProfilingSamper)){
            Render(cmd, ref renderingData);
        }

        // 分派命令列表给命令队列，同时清除命令队列
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    private void Render(CommandBuffer cmd, ref RenderingData renderingData)
    {
        Debug.Log("Render");
        Camera renderCamera = renderingData.cameraData.camera;

        Matrix4x4 frustumCorners = Matrix4x4.identity;
        Transform cameraTrans = renderCamera.transform;
        float halfHeight = renderCamera.nearClipPlane * Mathf.Tan(renderCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 toTop = cameraTrans.up * halfHeight;
        Vector3 toRight = cameraTrans.right * halfHeight * renderCamera.aspect;

        Vector3[] frustumPoints = new Vector3[4];
        // 左下角的点
        frustumPoints[0] = cameraTrans.forward * renderCamera.nearClipPlane - toTop - toRight;
        // 左上角的点
        frustumPoints[1] = cameraTrans.forward * renderCamera.nearClipPlane + toTop - toRight;
        // 右上角的点
        frustumPoints[2] = cameraTrans.forward * renderCamera.nearClipPlane + toTop + toRight;
        // 右下角的点
        frustumPoints[3] = cameraTrans.forward * renderCamera.nearClipPlane - toTop + toRight;
        float scale = frustumPoints[0].magnitude / renderCamera.nearClipPlane;
        for (int i = 0; i < frustumPoints.Length; ++i){
            frustumCorners.SetRow(i, frustumPoints[i].normalized * scale);
        }

        Vector3 cloudSize = m_VolumeCloud.m_CloudSize.value;
        Vector4 cloudBoxMin = m_VolumeCloud.m_CloudPos.value - 0.5f * cloudSize;
        Vector4 cloudBoxMax = m_VolumeCloud.m_CloudPos.value + 0.5f * cloudSize;

        m_VolumeCloudMat.SetMatrix("_FrustumCorners", frustumCorners);
        m_VolumeCloudMat.SetFloat("_RayMarchingStride", m_VolumeCloud.m_RayMarchingStride.value);
        m_VolumeCloudMat.SetVector("_CloudBoxMin", cloudBoxMin);
        m_VolumeCloudMat.SetVector("_CloudBoxMax", cloudBoxMax);
        m_VolumeCloudMat.SetTexture("_NoiceTexture", m_VolumeCloud.m_NoiceTextrue.value);
        m_VolumeCloudMat.SetFloat("_NoiceTexScale", m_VolumeCloud.m_NoiceTexScale.value);
        m_VolumeCloudMat.SetVector("_NoiceSampleOffset", m_VolumeCloud.m_NoiceSampleOffset.value);

        Blitter.BlitCameraTexture(cmd, m_CameraTarget, m_CameraTarget, m_VolumeCloudMat, 0);
    }

}
