#ifndef __SCATTERING__HLSL__
#define __SCATTERING__HLSL__

#define M_PI 3.14159265359

// 大气密度比
float AtmosphericDensityRatio(float h, float H)
{
    return exp(-h / H);
}

// 瑞利散射系数函数
float3 RayleighCoefficient()
{
    return float3(5.802, 13.558, 33.1) * 1e-6;
}

// 瑞利散射相位函数
float RayleighPhase(float cos)
{
    return 3 / (16 * M_PI) * (1 + cos * cos);
}


// 米氏散射系数函数
float3 MieCoefficient()
{
    return (3.996 * 1e-6).xxx;
}

// 米氏散射相位函数
// 改编自 Henyey-Greenstein 函数， 双 Henyey-Greenstein相位函数 的 单参数版 (原双相位函数拥有3个参数, 要确定3个参数非常复杂)
// g : ( -0.75, -0.999 )
//      3 * ( 1 - g^2 )               1 + cos^2
// F = ----------------- * -------------------------------
//      <4pi> * 2 * ( 2 + g^2 )     ( 1 + g^2 - 2 * g * cos )^(3/2)
float MiePhase(float cos, float anisotropy)
{
    float g = anisotropy;
    float gg = g * g;

    float a = 3 * (1 - gg);
    float b = 8 * M_PI * (2 + gg);
    float c = 1 + cos * cos;
    float d = pow((1 + gg - 2 * g * cos), 3 / 2);

    return a / b * c / d;
}

float3 Scattering(float3 pos, float3 viewDir, float3 lightDir)
{
    float cos = dot(normalize(viewDir), normalize(lightDir));
    float h = max(length(pos - _PlanetCenter.xyz) - _PlanetRadius, 0);
    float3 ray = RayleighCoefficient() * AtmosphericDensityRatio(h, _RayleighScatteringScalarHeight) * RayleighPhase(cos);
    float3 mie = MieCoefficient() * AtmosphericDensityRatio(h, _MieScatteringScalarHeight) * MiePhase(cos, _MieAnisotropy);
    return ray + mie;
}


#endif