sampler uImage0 : register(s0);

float uTime;
float4 uColor;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.position, uWorldViewProjection);
    output.position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    const float pi = 3.1415926534897;
    
    float xFade = smoothstep(0, 0.1, coords.x) * (1 - coords.x);
    bool edge = coords.y > 1 - ((1 - sin((coords.x - uTime) * 10 * pi)) * 0.1 + 0.06) * pow(xFade, 2);
    float4 noise = tex2D(uImage0, float2(coords.x/ 2 - uTime, coords.y));
    float decay = pow(length(noise.rgb) * 3 * xFade * coords.y, coords.x * 4);
    return edge * uColor + pow(coords.y * xFade, 1.5) * input.Color * (1 + decay * 2 * sqrt(coords.x));

}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
