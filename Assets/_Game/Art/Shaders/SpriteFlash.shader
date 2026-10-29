// Sprite non éclairé (le rendu toon est déjà dans l'image) avec un flash blanc d'impact
// et un liseré de contre-jour rouge du côté de la lune.
// _Flash (0 à 1) est réglé par MaterialPropertyBlock (HitReaction, EnemyTelegraph) : aucun
// matériau n'est dupliqué. La couleur du SpriteRenderer reste une teinte (annonces d'attaque).
// Le liseré est calculé dans le monde (dérivées des UV rapportées à celles de la position) : il reste
// du côté de la lune quel que soit le sens du personnage, retourné ou non, et quelle que soit la cible de rendu.
// Le ciel du chapitre (SkyController) remplace la couleur et la direction du liseré et ajoute une teinte
// d'ambiance, par des variables globales ; sans ciel (alpha ou w à 0), les valeurs du matériau s'appliquent.
Shader "RED MET/Sprite Flash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Flash ("Flash", Range(0, 1)) = 0
        _FlashColor ("Couleur du flash", Color) = (1, 1, 1, 1)
        [HDR] _RimColor ("Couleur du liseré", Color) = (1.6, 0.18, 0.1, 1)
        _RimWidth ("Épaisseur du liseré (m)", Range(0, 0.1)) = 0.03
        _RimDirection ("Direction de la lune (monde)", Vector) = (0.85, 0.55, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float _Flash;
                float4 _FlashColor;
                float4 _RimColor;
                float _RimWidth;
                float4 _RimDirection;
            CBUFFER_END

            // Variables globales du ciel du chapitre (hors CBUFFER : communes à tous les personnages).
            float4 _SkyRimColor;
            float4 _SkyRimDirection;
            float4 _SkyAmbient;

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
                float2 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz).xy;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                // UV par mètre dans le monde (le signe suit le retournement du sprite), puis test de
                // l'opacité vers la lune : transparent à _RimWidth mètres = bord éclairé.
                float2 uvPerMeter = float2(ddx(input.uv.x) / ddx(input.positionWS.x), ddy(input.uv.y) / ddy(input.positionWS.y));
                bool skyRim = _SkyRimDirection.w > 0.5;
                float4 rimColor = skyRim ? _SkyRimColor : _RimColor;
                float2 direction = normalize(skyRim ? _SkyRimDirection.xy : _RimDirection.xy);
                float2 probe = input.uv + direction * uvPerMeter * _RimWidth;
                half outside = 1.0h - step(0.5h, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, probe).a);
                color.rgb *= lerp(1.0, _SkyAmbient.rgb, _SkyAmbient.a);
                color.rgb = lerp(color.rgb, rimColor.rgb, outside * step(0.5h, color.a) * rimColor.a);

                color.rgb = lerp(color.rgb, _FlashColor.rgb, _Flash);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
