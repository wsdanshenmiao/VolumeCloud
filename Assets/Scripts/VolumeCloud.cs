using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

[Serializable, VolumeComponentMenuForRenderPipeline("DSMRendering/VolumeCloud", typeof(UniversalRenderPipeline))]
public class VolumeCloud : VolumeComponent , IPostProcessComponent
{
    [Tooltip("步进步频")]
    public ClampedFloatParameter m_RayMarchingStride = new ClampedFloatParameter(.5f, .2f, 1);
    [Tooltip("体积云的位置")]
    public Vector3Parameter m_CloudPos = new Vector3Parameter(Vector3.zero);
    [Tooltip("体积云的大小")]
    public Vector3Parameter m_CloudSize = new Vector3Parameter(Vector3.one);
    [Tooltip("噪声图")]
    public Texture3DParameter m_NoiceTextrue = new Texture3DParameter(null);
    [Tooltip("纹理坐标的缩放")]
    public FloatParameter m_NoiceTexScale = new FloatParameter(1);
    [Tooltip("采样噪声图的偏移")]
    public Vector4Parameter m_NoiceSampleOffset = new Vector4Parameter(Vector4.zero);

    public bool IsActive() => m_RayMarchingStride.value < 1;

    public bool IsTileCompatible() => false;
}
