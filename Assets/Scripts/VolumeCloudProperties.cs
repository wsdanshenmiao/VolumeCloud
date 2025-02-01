using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudParamer : VolumeComponent, IPostProcessComponent
{
    [Tooltip("编辑模式时渲染云")]
    public BoolParameter m_EnableInEdit = new BoolParameter(false);
    
    [Tooltip("采样形状的最大步进次数")]
    public ClampedIntParameter m_ShapeMarchingCount = new ClampedIntParameter(50, 10, 200);
    [Tooltip("采样光照的最大步进次数")]
    public ClampedIntParameter m_LightMarchingCount = new ClampedIntParameter(20, 5, 50);

    [Tooltip("密度偏移")]
    public FloatParameter m_DensityOffset = new FloatParameter(0);
    [Tooltip("采样形状坐标缩放")]
    public FloatParameter m_SampleShapeScale = new FloatParameter(1);
    [Tooltip("采样形状细节坐标缩放")]
    public FloatParameter m_SampleDetailScale = new FloatParameter(1);
    [Tooltip("密度缩放")]
    public FloatParameter m_DensityMultiplier = new FloatParameter(1);
    [Tooltip("散射系数")]
    public FloatParameter m_CloudScatter = new FloatParameter(1);

    [Tooltip("密度阈值")]
    public ClampedFloatParameter m_DensityThreshold = new ClampedFloatParameter(.2f, -10, 10);
    [Tooltip("光照阈值")]
    public ClampedFloatParameter m_DarknessThreshold = new ClampedFloatParameter(.2f, 0, 1);
    [Tooltip("消光系数")]
    public ClampedFloatParameter m_ExtinctionCoefficient = new ClampedFloatParameter(1, 0, 1);
    [Tooltip("细节影响权重")]
    public ClampedFloatParameter m_DetailScale = new ClampedFloatParameter(1, 0, 100);

    [Tooltip("体积云中心")]
    public Vector3Parameter m_CloudBoxCenter = new Vector3Parameter(Vector3.zero);
    [Tooltip("体积云大小")]
    public Vector3Parameter m_CloudBoxSize = new Vector3Parameter(Vector3.one);
    [Tooltip("采样形状坐标偏移")]
    public Vector3Parameter m_SampleShapeOffset = new Vector3Parameter(Vector3.zero);
    [Tooltip("采样形状细节坐标偏移")]
    public Vector3Parameter m_SampleDetailOffset = new Vector3Parameter(Vector3.zero);
    [Tooltip("细节控制权重")]
    public Vector3Parameter m_DetailWeights = new Vector3Parameter(Vector3.one);


    [Tooltip("形状控制权重")]
    public Vector3Parameter m_ShapeWeights = new Vector3Parameter(Vector3.one);

    [Tooltip("形状噪声")]
    public Texture3DParameter m_ShapeNoiceTex = new Texture3DParameter(null);
    [Tooltip("细节噪声")]
    public Texture3DParameter m_DetailNoiceTex = new Texture3DParameter(null);
    [Tooltip("控制天气的噪声")]
    public Texture2DParameter m_WeatherNoiceTex = new Texture2DParameter(null);

    public bool IsActive() => true;
    public bool IsTileCompatible() => false;
}
