
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
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
        [HideInInspector] public Material m_VolumeCloudMat = null;
    }

    private VolumeCloudRenderPass m_VolumeCloudRenderPass;
    private RenderTexture[] m_CloudTex = new RenderTexture[2];
    public VolumeCloudSetting m_VolumeCloudSetting;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bool disableEdit = !m_VolumeCloudRenderPass.EnableEdit;
        if (renderingData.cameraData.cameraType != CameraType.Game && disableEdit) return;

        if(m_VolumeCloudSetting.m_FrameBlock == FrameBlock._OFF){
            for(int i = 0; i < m_CloudTex.Length; ++i){
                m_CloudTex[i] = null;
            }
        }
        else{
            int textureWidth = renderingData.cameraData.camera.pixelWidth;
            int textureHeight = renderingData.cameraData.camera.pixelHeight;
            for(int i = 0; i < m_CloudTex.Length; ++i){
                // 为分帧绘制创建新的纹理
                if(m_CloudTex[i] == null ||
                    m_CloudTex[i].width != textureWidth ||
                    m_CloudTex[i].height != textureHeight){
                    m_CloudTex[i] = new RenderTexture(textureWidth, textureHeight, 0);
                }
            }
        }

        m_VolumeCloudRenderPass.SetCloudTex(ref m_CloudTex);

        renderer.EnqueuePass(m_VolumeCloudRenderPass);
    }

    public override void Create()
    {
        name = "VolumeCloud";
        if(m_VolumeCloudSetting.m_VolumeCloudMat == null){
            m_VolumeCloudSetting.m_VolumeCloudMat = 
                CoreUtils.CreateEngineMaterial(m_VolumeCloudSetting.m_VolumeCloudShader);
        }

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