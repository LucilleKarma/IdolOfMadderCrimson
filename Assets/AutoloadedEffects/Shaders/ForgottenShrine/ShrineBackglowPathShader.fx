sampler noiseTexture : register(s1);

float globalTime;
float endFadeoutTaper;
float manualTimeOffset;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    // Apply smoothening to the visual.
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    float noiseA = tex2D(noiseTexture, coords * float2(1.1, 0.216) + float2(globalTime * 0.01 + manualTimeOffset, 0));
    float noiseB = tex2D(noiseTexture, coords * float2(1.2, 0.194) + float2(globalTime * 0.02 + manualTimeOffset * 2, 0));
    float noise = (noiseA + noiseB) * 0.5;
    float edgeFadeout = smoothstep(0.5, 0.25, distance(coords.y, 0.5));
    color.rgb += float3(0.2, 0.02, -0.06) * smoothstep(0.45, 0, coords.x);
    
    float frontFadeOut = smoothstep(0.03, 0.08, coords.x);
    float endFadeOut = smoothstep(1.1, 1, coords.x + endFadeoutTaper);
    
    return color * smoothstep(0.36, 1, noise) * edgeFadeout * frontFadeOut * endFadeOut * 2;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
