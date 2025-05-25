sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float decayFactor;
float2 pixelationFactor;
float2 ripplePoints[10];
float2 stepSize;

float4 PixelFunction(float2 coords : TEXCOORD0, float4 color : COLOR0, float4 position : SV_Position) : COLOR0
{
    float addedPressure = 0;
    for (int i = 0; i < 10; i++)
    {
        float distanceFromRipple = distance(coords, ripplePoints[i]);
        addedPressure = saturate(addedPressure + 0.003 / distanceFromRipple);
    }
    
    float4 currentData = tex2D(baseTexture, coords);
    float pressureLeft = tex2D(baseTexture, coords + float2(-1, 0) * stepSize);
    float pressureRight = tex2D(baseTexture, coords + float2(1, 0) * stepSize);
    float pressureTop = tex2D(baseTexture, coords + float2(0, -1) * stepSize);
    float pressureBottom = tex2D(baseTexture, coords + float2(0, 1) * stepSize);
    
    float pressure = currentData.x;
    float surroundingPressureChange = (pressureLeft + pressureRight + pressureTop + pressureBottom - pressure * 4) * 0.25;
    float pressureVelocity = currentData.y + surroundingPressureChange;
    pressure += pressureVelocity;
    
    // Make change in pressure decrease with pressure, as a rebound-like effect.
    pressureVelocity -= pressure * 0.004;
    
    // Make pressure and change in pressure exponentially dissipate over time.
    pressureVelocity *= decayFactor;
    pressure *= decayFactor;
    
    return float4(pressure + addedPressure, pressureVelocity, 0, 1);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelFunction();
    }
}