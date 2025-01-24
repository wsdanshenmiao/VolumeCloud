using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorTint : VolumeComponent
{
    //【设置颜色参数】
    public ColorParameter colorChange = new ColorParameter(Color.white, true);//如果有两个true,则为HDR设置
}
