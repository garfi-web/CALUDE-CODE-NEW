// Sprite lumineux non éclairé, en addition : lune, halo, fissures de Red Met, braises.
// _Intensity pousse la couleur au-delà de 1 pour que le bloom du post-traitement s'en empare.
// _Tint teinte le sprite (astres, halos et éclairs des ciels de chapitre, par MaterialPropertyBlock) : sous
// Unity 6, la couleur d'un SpriteRenderer n'arrive plus par les sommets dans ce shader.
Shader "RED MET/Sprite Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Intensity ("Intensité", Range(0, 8)) = 2
        _Tint ("Teinte", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Pas de _MainTex_ST : le SRP Batcher 2D refuse les matériaux qui en ont.
            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float4 _Tint;
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

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                color *= _Tint;
                color.rgb *= _Intensity;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
