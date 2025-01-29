#ifndef __VOLUMECLOUDUTIL__HLSL__
#define __VOLUMECLOUDUTIL__HLSL__


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
    int3 dirIsNeg = int3(int(invDir.x > 0), int(invDir.y > 0), int(invDir.z > 0));
    float3 tMins = (boxMin - origin) * invDir;    // invDir为(1/x,1/y,1/z)
    float3 tMaxs = (boxMax - origin) * invDir;
    for (int i = 0; i < 3; ++i) {
        if (dirIsNeg[i] != 0) {  // 若该轴为负从tMax进，tMin出,需要交换
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

#endif