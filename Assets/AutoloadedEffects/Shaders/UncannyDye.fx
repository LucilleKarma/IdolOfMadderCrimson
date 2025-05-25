sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 ArmorAnimatedSine(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Sample the base texture
    float4 color = tex2D(uImage0, coords);

    // Noise calculation
    float2 noiseCoords = (coords * uImageSize0 - uSourceRect.xy) / uImageSize1;
    float4 noise = tex2D(uImage1, noiseCoords);
    float luminosity = (color.r + color.g + color.b) / 3;

    // Apply the original luminosity + noise effect
    color.rgb = luminosity * noise.b * 2;

    // Define the chosen color and the target replacement color
    float3 chosenColor = float3(1.0, 0.0, 0.0);  // Example: Red
    float3 targetColor = float3(0.0, 1.0, 0.0);  // Example: Green

    // Define the threshold for color similarity
    float threshold = 0.2;

    // Calculate the distance between the pixel color and the chosen color
    float colorDistance = distance(color.rgb, chosenColor);

    // Blend the color if within the threshold
    if (colorDistance < threshold)
    {
        color.rgb = lerp(color.rgb, targetColor, 1); // Blend with 50% strength
    }

    // Return the final color multiplied by the sample color
    return color * sampleColor;
}



technique Technique1
{
    pass ArmorAnimatedSine
    {
        PixelShader = compile ps_2_0 ArmorAnimatedSine();
    }
}