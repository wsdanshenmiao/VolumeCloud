using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudParamer : VolumeComponent, IPostProcessComponent
{
    [Tooltip("编辑模式时渲染云")]
    public BoolParameter m_EnableInEdit = new BoolParameter(false);
    
    [Tooltip("步进步长")]
    public ClampedFloatParameter m_RayMarchingStride = new ClampedFloatParameter(.5f, .2f, 10);
    [Tooltip("光照阈值")]
    public ClampedFloatParameter m_DarknessThreshold = new ClampedFloatParameter(.2f, 0, 1);
    [Tooltip("消光系数")]
    public ClampedFloatParameter m_ExtinctionCoefficient = new ClampedFloatParameter(1, 0, 1);
    [Tooltip("密度偏移")]
    public FloatParameter m_DensityOffset = new FloatParameter(0);
    [Tooltip("纹理坐标缩放")]
    public FloatParameter m_SampleNoiceScale = new FloatParameter(1);
    [Tooltip("密度阈值")]
    public ClampedFloatParameter m_DensityThreshold = new ClampedFloatParameter(.2f, 0, 1);
    [Tooltip("密度缩放")]
    public FloatParameter m_DensityMultiplier = new FloatParameter(1);

    [Tooltip("体积云中心")]
    public Vector3Parameter m_CloudBoxCenter = new Vector3Parameter(Vector3.zero);
    [Tooltip("体积云大小")]
    public Vector3Parameter m_CloudBoxSize = new Vector3Parameter(Vector3.one);
    [Tooltip("纹理坐标偏移")]
    public Vector3Parameter m_SampleNoiceOffset = new Vector3Parameter(Vector3.zero);


    [Tooltip("形状控制权重")]
    public Vector4Parameter m_ShapeWeights = new Vector4Parameter(Vector4.one);

    [Tooltip("形状噪声")]
    public Texture3DParameter m_ShapeNoiceTex = new Texture3DParameter(null);
    [Tooltip("细节噪声")]
    public Texture3DParameter m_DetailNoiceTex = new Texture3DParameter(null);
    [Tooltip("控制天气的噪声")]
    public Texture2DParameter m_WeatherNoiceTex = new Texture2DParameter(null);

    public bool IsActive() => true;
    public bool IsTileCompatible() => false;
}
