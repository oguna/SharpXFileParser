struct VS_IN
{
	float4 pos : POSITION;
	float4 col : COLOR;
	float2 tex : TEXCOORD;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
	float2 tex : TEXCOORD;
};

float4x4 worldViewProj;
float4 ambient;
float4 localLightDirection; // ローカル座標系でのライトの向き
float4 diffuse;

Texture2D picture;
SamplerState pictureSampler;

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = mul(input.pos, worldViewProj);
	output.col = input.col;
	output.tex = input.tex;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	//return input.col;
	return ambient + diffuse *max(0, dot(input.col, -localLightDirection));
}

float4 PSTex( PS_IN input ) : SV_Target
{
	return (ambient + diffuse *max(0, dot(input.col, -localLightDirection))) *picture.Sample(pictureSampler, input.tex);
}