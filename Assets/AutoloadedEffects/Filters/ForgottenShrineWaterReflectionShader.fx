sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);
sampler liquidTexture : register(s2);
sampler rippleTexture : register(s3);
sampler tileTexture : register(s4);
sampler lightDistanceTexture : register(s5);

float globalTime;
float perturbationStrength;
float reflectionMaxDepth;
float reflectionStrength;
float reflectionWaviness;
float ripplePerspectiveSquishFactor;
float2 zoom;
float2 perturbationScroll;
float2 screenPosition;
float2 oldScreenPosition;
float2 targetSize;

float CalculateLiquidPixelLineY(float2 coords)
{
    float top = 0;
    float bottom = 1;

    // Perform binary search to find the first Y with alpha > 0.
    for (int i = 0; i < 30; i++)
    {
        float midpoint = (top + bottom) * 0.5;
        float alpha = tex2D(liquidTexture, float2(coords.x, midpoint)).a;

        // If transparent, look further down.
        bool lookFurtherDown = alpha < 0.01;
        top = lerp(top, midpoint, lookFurtherDown * 0.5);
        bottom = lerp(bottom, midpoint, !lookFurtherDown * 0.5);
    }
    
    return bottom;
}

float2 CalculatePerturbation(float2 coords)
{
    return tex2D(noiseTexture, coords * 10 + perturbationScroll) * perturbationStrength;
}

float2 CalculateReflectedCoords(float2 coords, float2 worldStableCoords, float2 screenOffset, float reflectionLineY, float reflectionInterpolant)
{
    // Determine how much coordinates should be stretched vertically as a result of reflections.
    float stretch = 1 + (1 - reflectionInterpolant) * -0.4;
    
    float reconvertedReflectionY = (reflectionLineY - screenOffset.y - 0.5) * zoom.y + 0.5;
    float reflectedY = reconvertedReflectionY * 2 - coords.y;
    float reflectionWave = sin(worldStableCoords.y * 300 - globalTime * 3) * (1 - reflectionInterpolant) * reflectionWaviness;
    float2 perturbation = CalculatePerturbation(worldStableCoords);
    return float2(coords.x + reflectionWave, (reflectedY - 0.5) * stretch + 0.5) + perturbation;
}

float4 ModulateReflectedColors(float4 reflectedColor)
{
    // Make reflections brighter in spots where they're already bright, for a bit of artistic zest.
    float reflectionBrightness = dot(reflectedColor.rgb, float3(0.3, 0.6, 0.1));
    reflectedColor *= 1 + smoothstep(0.5, 1, reflectionBrightness) * 0.93;
    
    return reflectedColor;
}

float3 CalculateRippleLight(float2 liquidTextureCoords, float reflectionLineY)
{
    // Squash ripple coordinates vertically around the reflection line to give a better sense of perspective that isn't conferred with the perfect circles
    // present within the ripple texture.
    float2 rippleCorrectiveOffset = float2(0.5, reflectionLineY);
    float2 rippleCoords = (liquidTextureCoords - rippleCorrectiveOffset) * float2(1, ripplePerspectiveSquishFactor) + rippleCorrectiveOffset;
    
    float4 rippleData = tex2D(rippleTexture, rippleCoords);
    float liquidOpacity = tex2D(liquidTexture, liquidTextureCoords).a;
    
    return clamp(fwidth(rippleData.y) * 3.5, 0, 0.3) * liquidOpacity;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate coordinates.
    float2 screenOffset = (screenPosition - oldScreenPosition) / targetSize;
    float2 worldStableCoords = (coords - 0.5) / zoom + 0.5 + screenPosition / targetSize;
    float2 liquidTextureCoords = (coords - 0.5) / zoom + 0.5 + screenOffset;
    
    // Calculate the reflection line Y position.
    float reflectionLineY = CalculateLiquidPixelLineY(liquidTextureCoords);
    
    // Use the reflection line to determine how deep in the water the current pixel is.
    float depth = (liquidTextureCoords.y - reflectionLineY) * targetSize.y;
    
    // Determine how strong reflections should be at this pixel.
    // This effect is strongest at the top of water and tapers off from there.
    float reflectionInterpolant = smoothstep(reflectionMaxDepth, 0, depth) * smoothstep(-1, 0, depth);
    
    // Make reflections dissipate if the water is shallow.
    reflectionInterpolant *= smoothstep(0.1, 0.2, tex2D(lightDistanceTexture, liquidTextureCoords).g);
    
    // Use reflection math to determine the color of the pixel when reflected along the reflection line.
    float2 reflectionCoords = CalculateReflectedCoords(coords, worldStableCoords, screenOffset, reflectionLineY, reflectionInterpolant);
    float4 reflectedColor = ModulateReflectedColors(tex2D(baseTexture, reflectionCoords));
    
    // Calculate the influence of ripples.
    float rippleLight = CalculateRippleLight(liquidTextureCoords, reflectionLineY);
    
    // Combine things together.
    float edgeOfScreenTaper = smoothstep(0, 0.1, reflectionCoords.y) * smoothstep(1, 0.9, reflectionCoords.y);
    float4 baseColor = tex2D(baseTexture, coords);
    return baseColor + reflectionInterpolant * reflectedColor * reflectionStrength * edgeOfScreenTaper + rippleLight;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}