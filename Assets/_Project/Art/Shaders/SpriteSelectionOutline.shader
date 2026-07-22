// Draws a solid-color ring exactly where the sprite's own alpha is transparent but a nearby
// texel is opaque — i.e. only outside the sprite's own silhouette, never overlapping it. Meant
// for a dedicated "Outline" child SpriteRenderer (same sprite as the real Visuals renderer,
// rendered behind it), toggled on/off via GameEntityBase.SetSelected, rather than tinting the
// real sprite directly (see SpriteGrayscaleGhost.shader's doc comment for why tinting full-color
// art directly reads muddy/ambiguous instead of as a clean signal).
Shader "CaseGame/SpriteSelectionOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0.92, 0.016, 1)
        _OutlineThickness ("Outline Thickness (texels)", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineThickness;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float centerAlpha = SampleAlpha(IN.uv);
                float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;

                float neighborAlpha = max(
                    max(SampleAlpha(IN.uv + float2(texel.x, 0)), SampleAlpha(IN.uv - float2(texel.x, 0))),
                    max(SampleAlpha(IN.uv + float2(0, texel.y)), SampleAlpha(IN.uv - float2(0, texel.y))));

                float isEdge = step(centerAlpha, 0.01) * step(0.01, neighborAlpha);
                return float4(_OutlineColor.rgb, _OutlineColor.a * isEdge) * IN.color.a;
            }
            ENDHLSL
        }
    }
}
