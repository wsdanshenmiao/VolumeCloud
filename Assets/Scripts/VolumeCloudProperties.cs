using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudParamer : VolumeComponent, IPostProcessComponent
{
    public BoolParameter m_EnableInEdit = new BoolParameter(false);
    public ClampedFloatParameter m_RayMarchingStride = new ClampedFloatParameter(.5f, .2f, 1);
    public Vector3Parameter m_CloudBoxCenter = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter m_CloudBoxSize = new Vector3Parameter(Vector3.one);

    public bool IsActive() => true;
    public bool IsTileCompatible() => false;
}
