#ifndef __VOLUMECLOUDUTIL__HLSL__
#define __VOLUMECLOUDUTIL__HLSL__

float _RayMarchingStride;
float4 _CloudBoxMin;
float4 _CloudBoxMax;
sampler3D _NoiceTexture;
float _NoiceTexScale;
float4 _NoicSampleOffset;

// 判断射线与AABB盒是否相交，返回 (是否相交， 射线远点与包围盒的最短距离)
float2 RayInsertBox(float3 boxMin, float3 boxMax, float3 origin, float3 invDir)
{
    // 三个轴的 tEnter 和 tExit
    int3 dirIsNeg = int3(int(invDir.x > 0), int(invDir.y > 0), int(invDir.z > 0));
    float3 tMins = (boxMin - origin) * invDir;    // invDir为(1/x,1/y,1/z)
    float3 tMaxs = (boxMax - origin) * invDir;
    for (int i = 0; i < 3; ++i) {
        if (!dirIsNeg[i]) {  // 若该轴为负从tMax进，tMin出,需要交换
            float tmp = tMins[i];
            tMins[i] = tMaxs[i];
            tMaxs[i] = tmp;
        }
    }
    float tMin = max(tMins.x, max(tMins.y, tMins.z));   // 取进入时间的最大值
    float tMax = min(tMaxs.x, min(tMaxs.y, tMaxs.z));   // 取离开时间的最小值

    if (tMin > tMax || tMax < 0) {
        return float2(0, tMin);
    }
    return float2(1, tMin);
}

float SampleDensity(sampler3D noiceTexture, float3 pos)
{
    float3 uvw = pos * _NoiceTexScale + _NoicSampleOffset.xyz;
    return tex3D(noiceTexture, uvw).r;
}

// 从散射点计算来自光源的光照
float3 LightMarching(float3 currPos, float3 lightPos)
{
    float sumDensity = 0;
    float3 lightDir = normalize(lightPos - currPos);
    float3 step  = lightDir * _RayMarchingStride;
    // 计算光线与包围盒的交点
    float3 invDir = float3(1 / lightDir.x, 1/lightDir.y,1/ lightDir.z);
    float2 insertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);
    if(insertInfo.x ==0) return sumDensity;

    // 沿光照方向进行管线步进
    for(int i =0; i < insertInfo.y; i += _RayMarchingStride){
        currPos += step;
    }
    return sumDensity;
}

float3 VolumeCloudRayMarching(float3 starPos, float3 dir, float3 posW)
{
    float3 sumDensity = 0;          // 体积云密度
    float transmittance = 1;        // 体积云透射比
    float3 currPos = starPos;
    // 步进步长
    float3 step = dir * _RayMarchingStride;
    float3 invDir = float3(1 / dir.x, 1 / dir.y, 1 / dir.z);
    
    // 判断射线是否与云的包围盒相交
    float2 rayInsertInfo = RayInsertBox(_CloudBoxMin.xyz, _CloudBoxMax.xyz, currPos, invDir);
    if(rayInsertInfo.x == 0)return sumDensity;
    // 若相交直接向前步进
    currPos = currPos + dir * rayInsertInfo.y;
    float limitLen = length(posW - starPos) - rayInsertInfo.y;

    // 进行光线步进
    [loop]
    for(float i = 0; i < limitLen; i += _RayMarchingStride) {
        float density = SampleDensity(_NoiceTexture, currPos.xyz);
        sumDensity += LightMarching(currPos, _WorldSpaceLightPos0.xyz);      // 计算来自主光源的光照
        transmittance *= exp(-density * _RayMarchingStride);        // 云的密度越大，衰减越严重
        currPos += step;
    }

    return sumDensity;
}

#endif