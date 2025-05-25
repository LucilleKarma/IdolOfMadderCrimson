﻿sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler liquidTexture : register(s2);
sampler lightTexture : register(s3);
sampler lightDistanceTexture : register(s4);
sampler tileTexture : register(s5);

float globalTime;
float noiseAppearanceThreshold;
float noiseAppearanceSmoothness;
float mistHeight;
float2 zoom;
float2 mistCoordinatesZoom;
float2 screenPosition;
float2 oldScreenPosition;
float2 targetSize;
float4 mistColor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate coordinates.
    float2 screenOffset = (screenPosition - oldScreenPosition) / targetSize;
    float2 worldStableCoords = (coords - 0.5) / zoom + 0.5 + screenPosition / targetSize;
    float2 liquidTextureCoords = (coords - 0.5) / zoom + 0.5 + screenOffset;
    
    // Determine how much light is assigned to this pixel.
    float light = tex2D(lightTexture, liquidTextureCoords);
    
    // Determine how much mist should be present. Only pixels above liquid may receive mist.
    float4 liquidDistanceData = tex2D(lightDistanceTexture, liquidTextureCoords);
    float distanceToLiquid = liquidDistanceData.r * targetSize.y;
    float mistInterpolant = smoothstep(0, mistHeight * 0.1875, distanceToLiquid) * smoothstep(mistHeight, mistHeight * 0.45, distanceToLiquid);
    
    // Make mist dissipate if light is low.
    mistInterpolant *= smoothstep(0, 0.5, pow(light, 1.6));
    
    // Make mist dissipate if the water is shallow.
    mistInterpolant *= smoothstep(0.05, 0.15, liquidDistanceData.g);
    
    // Make mist dissipate if it's inside of tiles.
    mistInterpolant *= 1 - tex2D(tileTexture, liquidTextureCoords).a;
    
    // Do some standard noise-warping-noise calculations to determine the shape of the mist.
    float time = globalTime * 0.3;
    float2 noiseCoords = worldStableCoords * mistCoordinatesZoom;
    float warpNoise = tex2D(noiseTexture, noiseCoords * float2(0.3, 2.76) + float2(time * 0.02, 0)) * 0.045;
    float mistNoiseA = tex2D(noiseTexture, noiseCoords * float2(0.6, 1.1) + float2(time * -0.03, 0.3) - warpNoise);
    float mistNoiseB = tex2D(noiseTexture, noiseCoords * float2(0.2, 1.4) + float2(time * 0.02, 0.5) + warpNoise + mistNoiseA * 0.1);
    float mistNoise = smoothstep(0, noiseAppearanceSmoothness, sqrt(mistNoiseA * mistNoiseB) - noiseAppearanceThreshold);
    
    float4 baseColor = tex2D(baseTexture, coords);
    return baseColor + mistColor * mistInterpolant * mistNoise;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}