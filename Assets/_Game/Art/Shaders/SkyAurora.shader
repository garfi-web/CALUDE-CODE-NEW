// Aurores boréales qui ondulent (ciel du ch. 5), en addition sur le ciel peint.
// Trois rideaux calculés dans l'UV du sprite : bord bas net, lumière qui s'efface vers le haut, stries
// verticales qui glissent. _ColorA (bas) et _ColorB (haut), _Intensity et _Speed viennent du SkyProfile
// (MaterialPropertyBlock posé par SkyController). Aucune texture : le coût est celui d'un seul quad.
Shader "RED MET/Sky Aurora"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (UV seulement)", 2D) = "white" {}
        _ColorA ("Couleur basse", Color) = (0.2, 1, 0.55, 1)
        _ColorB ("Couleur haute", Color) = (0.2, 0.55, 1, 1)
        _Intensity ("Intensité", Range(0, 2)) = 1
        _Speed ("Vitesse", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float _Intensity;
                float _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // Un rideau : ligne de base qui ondule, bord bas net, longue traîne vers le haut, stries.
            float Curtain(float2 uv, float t, float baseY, float phase, float height)
            {
                float wave = 0.07 * sin(uv.x * 5.0 + t * 1.3 + phase) + 0.035 * sin(uv.x * 11.0 - t * 0.9 + phase * 2.0);
                float d = uv.y - (baseY + wave);
                float body = d > 0.0 ? exp(-d / height) : exp(d * 60.0);
                float streaks = 0.55 + 0.45 * sin(uv.x * 140.0 + 4.0 * sin(uv.x * 9.0 + t * 0.7 + phase) + t * 2.0);
                float fold = 0.6 + 0.4 * sin(uv.x * 3.0 + t * 0.5 + phase * 3.0);
                return body * streaks * fold;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = _Time.y * _Speed * 6.0;

                float a = Curtain(uv, t, 0.42, 0.0, 0.22) + 0.7 * Curtain(uv, t * 0.8, 0.55, 2.1, 0.16) + 0.5 * Curtain(uv, t * 1.2, 0.33, 4.4, 0.12);

                // Fondu sur les bords du quad.
                float edge = smoothstep(0.0, 0.15, uv.x) * smoothstep(1.0, 0.85, uv.x) * smoothstep(0.0, 0.2, uv.y) * smoothstep(1.0, 0.75, uv.y);
                float3 color = lerp(_ColorA.rgb, _ColorB.rgb, saturate((uv.y - 0.3) * 2.0));
                return half4(color * input.color.rgb, saturate(a * edge * _Intensity * 0.55) * input.color.a);
            }
            ENDHLSL
        }
    }
}
