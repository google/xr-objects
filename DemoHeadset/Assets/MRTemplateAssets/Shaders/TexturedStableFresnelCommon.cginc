#ifndef STABLE_FRESNEL_COMMON
#define STABLE_FRESNEL_COMMON

half4 _EdgeColor;   // Color and alpha of the fresnel effect
half4 _Color;   // Color and alpha of the base of the object
half4 _EdgeData;    // Min, Max, Power, Blend values

sampler2D _MainTex;
float4 _MainTex_ST;

struct appdata_fresnel
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;

    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct fresnel_vertex
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float2 uv : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

fresnel_vertex vert(appdata_fresnel v)
{
    fresnel_vertex o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.uv = o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    return o;
}

half4 fragEmpty(fresnel_vertex i) : COLOR
{
    return half4(0,0,0,1);
}

half4 fragRimShader(fresnel_vertex i) : COLOR
{
    half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
    half rim = saturate(((1.0 - saturate(dot(normalize(worldViewDir), i.worldNormal))) - _EdgeData.x) / (_EdgeData.y - _EdgeData.x));
    half processedRim = (3 + _EdgeData.z) * pow(rim, _EdgeData.z + 1) - (2 + _EdgeData.z) * pow(rim, _EdgeData.z + 2);
    return lerp(_Color, _EdgeColor, lerp(rim, processedRim, _EdgeData.w)) * tex2D(_MainTex, i.uv).rgba;
}

#endif // STABLE_FRESNEL_COMMON
