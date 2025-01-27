
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudParamer : VolumeComponent, IPostProcessComponent
{
    public BoolParameter m_EnableInEdit = new BoolParameter(false);
    public ClampedFloatParameter m_RayMarchingStride = new ClampedFloatParameter(.5f, .2f, 1);

    public bool IsActive() => true;
    public bool IsTileCompatible() => false;
}
