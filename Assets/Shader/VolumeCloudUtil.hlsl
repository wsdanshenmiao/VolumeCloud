#ifndef __VOLUMECLOUDUTIL__HLSL__
#define __VOLUMECLOUDUTIL__HLSL__

#ifndef M_PI
#define M_PI 3.14159265359
#endif

struct Ray
{
    float3 startPos;
    float3 dir;
};

// abstract ： 判断射线与AABB盒是否相交
// return : 射线与包围盒是否相交,相交的近点和远点)
float3 RayInsertBox(float3 boxMin, float3 boxMax, float3 origin, float3 invDir)
{
    // 三个轴的 tEnter 和 tExit
    float3 tMins = (boxMin - origin) * invDir;    // invDir为(1/x,1/y,1/z)
    float3 tMaxs = (boxMax - origin) * invDir;
    for (int i = 0; i < 3; ++i) {
        if (invDir[i] < 0) {  // 若该轴为负从tMax进，tMin出,需要交换
            float tmp = tMins[i];
            tMins[i] = tMaxs[i];
            tMaxs[i] = tmp;
        }
    }
    float tMin = max(tMins.x, max(tMins.y, tMins.z));   // 取进入时间的最大值
    float tMax = min(tMaxs.x, min(tMaxs.y, tMaxs.z));   // 取离开时间的最小值

    if (tMin > tMax || tMax < 0) {
        return float3(0, tMin, tMax);
    }
    else{
        return float3(1, tMin, tMax);
    }
}

// 重映射函数，将 value 从 (lo, ho) 映射到 (ln, hn)
float Remap(float value, float lo, float ho, float ln, float hn)
{
    return ln + (value - lo) * (hn - ln) / (ho - lo);
}

// Beer–Lambert law
// depth 为光学深度, 返回透射率
float BeerLambert(float depth)
{
    return exp(-depth);
}

// 计算吸光率, A = t * d * l (t为消光系数,d为密度,l为距离)
float CalcuAbsorbance(float t, float d, float l)
{
    return t * d * l;
}

// 计算某点的高度梯度
float GetDensityHeightGradient(float3 pos, float min, float max)
{
    float heightGradient = (pos.y - min) / (max - min);
    return saturate(heightGradient);
}

// 改编自 Henyey-Greenstein 函数， 双 Henyey-Greenstein相位函数 的 单参数版 (原双相位函数拥有3个参数, 要确定3个参数非常复杂)
// g : ( -0.75, -0.999 )
//      3 * ( 1 - g^2 )               1 + cos^2
// F = ----------------- * -------------------------------
//      <4pi> * 2 * ( 2 + g^2 )     ( 1 + g^2 - 2 * g * cos )^(3/2)
float HenyeyGreenstein(float cos, float anisotropy)
{
    float g = anisotropy;
    float gg = g * g;

    float a = 3 * (1 - gg);
    float b = 8 * M_PI * (2 + gg);
    float c = 1 + cos * cos;
    float d = pow((1 + gg - 2 * g * cos), 3 / 2);

    return a / b * c / d;
}

#endif