// Palette-index sprite shader (unlit). Works in the Built-in pipeline and in URP / URP 2D Renderer
// (passes without a LightMode tag are drawn as SRPDefaultUnlit). It ignores 2D lights.
//
// Sprite texture: red channel = palette index 0..255, alpha = coverage.
//   Import: Point filter, no compression, no mipmaps, sRGB OFF (IndexTextureImporter does this for *_index.png).
// _PaletteTex: one row per (biome, accent), one column per index.
//   Import: Point filter, clamp, no compression, no mipmaps, sRGB ON (the importer does this for palette.png).
// _PaletteRow is set per renderer (WorldArt.ApplyPalette), so one material serves every biome.
Shader "EscapeThe90s/PaletteSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Index Sprite", 2D) = "white" {}
        _PaletteTex ("Palette", 2D) = "white" {}
        [PerRendererData] _PaletteRow ("Palette Row", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _PaletteTex;
            float4 _PaletteTex_TexelSize;   // x = 1/width, y = 1/height, z = width, w = height
            float _PaletteRow;
            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                float index = floor(src.r * 255.0 + 0.5);
                float u = (index + 0.5) * _PaletteTex_TexelSize.x;
                float v = (_PaletteRow + 0.5) * _PaletteTex_TexelSize.y;
                fixed4 pal = tex2D(_PaletteTex, float2(u, v));
                float a = src.a * i.color.a;
                return fixed4(pal.rgb * i.color.rgb * a, a);   // premultiplied alpha
            }
            ENDCG
        }
    }
    FallBack Off
}
