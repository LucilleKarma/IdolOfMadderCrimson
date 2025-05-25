sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float globalTime;
float waterAppearanceThreshold;
float4 waterColorA;
float4 waterColorB;

float4 PixelFunction(float2 coords : TEXCOORD0, float4 color : COLOR0, float4 position : SV_Position) : COLOR0
{
    float4 timeScrollData = tex2D(baseTexture, coords);
    float noise = tex2D(noiseTexture, coords);
    
    float foam = smoothstep(0.5, 0.4, timeScrollData.r);
    float4 waterColor = lerp(waterColorA, waterColorB, sin(noise * 4 + timeScrollData.r * 10 + globalTime * 20) * 0.5 + 0.5) + foam;
    return waterColor * timeScrollData.a * pow(timeScrollData.g, 1.3);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelFunction();
    }
}