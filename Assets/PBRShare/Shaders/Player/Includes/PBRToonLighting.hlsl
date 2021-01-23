#ifndef PBRTOONLIGHTING_INCLUDED
#define PBRTOONLIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                      Lighting Functions                                   //
///////////////////////////////////////////////////////////////////////////////
SAMPLER(my_bilinear_clamp_sampler);
half3 PBRToonLightingLambert(half3 lightColor, half3 lightDir, half3 normal,Texture2D rampTex)
{
    half NdotL = saturate(dot(normal, lightDir));
    half HalfLambertDiffuse = NdotL * 0.5 + 0.5;
    half2 UV = half2(HalfLambertDiffuse, 0);
    half4 ramp = SAMPLE_TEXTURE2D(rampTex, my_bilinear_clamp_sampler, UV);
    half3 rampHSV = RgbToHsv(ramp.rgb);
    
    //return lightColor * rampHSV.b;
    return lightColor * NdotL;
}


#endif
