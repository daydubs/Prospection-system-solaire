Shader "SolarSystem/SpaceSkybox"
{
    Properties
    {
        _StarsTex ("Star field", 2D) = "black" {}
        _StarIntensity ("Star brightness", Range(0, 8)) = 3
        _GalaxyIntensity ("Milky Way brightness", Range(0, 0.15)) = 0.025
        _Rotation ("Sky rotation", Range(0, 360)) = 25
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_StarsTex);
            SAMPLER(sampler_StarsTex);
            CBUFFER_START(UnityPerMaterial)
                float _StarIntensity;
                float _GalaxyIntensity;
                float _Rotation;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1,0,0)), f.x),
                                 lerp(Hash(i + float3(0,1,0)), Hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0,0,1)), Hash(i + float3(1,0,1)), f.x),
                                 lerp(Hash(i + float3(0,1,1)), Hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float angle = radians(_Rotation);
                float s, c;
                sincos(angle, s, c);
                d.xz = float2(c * d.x - s * d.z, s * d.x + c * d.z);
                float2 uv = float2(atan2(d.z, d.x) * (0.5 / PI) + 0.5,
                                   asin(clamp(d.y, -1.0, 1.0)) / PI + 0.5);
                half3 stars = SAMPLE_TEXTURE2D(_StarsTex, sampler_StarsTex, uv).rgb * _StarIntensity;
                float latitude = dot(d, normalize(float3(0.35, 0.86, 0.37)));
                float cloud = Noise(d * 14.0) * 0.55 + Noise(d * 35.0) * 0.3 + Noise(d * 80.0) * 0.15;
                float band = exp(-latitude * latitude * 110.0);
                float dust = smoothstep(0.28, 0.72, Noise(d * 26.0 + 5.0));
                float galaxy = band * pow(saturate(cloud), 2.0) * lerp(0.25, 1.0, dust) * _GalaxyIntensity;
                half3 color = stars + galaxy * half3(0.92, 0.94, 1.0);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
