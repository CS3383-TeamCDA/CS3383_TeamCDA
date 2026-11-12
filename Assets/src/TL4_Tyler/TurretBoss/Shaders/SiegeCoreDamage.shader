Shader "TL4/SiegeCoreDamage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashAmount ("Hit flash", Range(0,1)) = 0
        _Wear ("Wear", Range(0,1)) = 0
        _Shutdown ("Shutdown", Range(0,1)) = 0
        _PowerLoss ("Red energy shutdown", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _FlashAmount, _Wear, _Shutdown, _PowerLoss;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float energy = saturate((c.r - max(c.g, c.b)) * 6);
                // Fixed pixel-space damage patterns accumulate with HP loss; they never crawl in time.
                float2 pixel = floor(i.uv * float2(1448,1086));
                float2 patch = floor(pixel / 18);
                float patchNoise = frac(sin(dot(patch, float2(127.1,311.7))) * 43758.5453);
                float2 chip = floor(pixel / 3);
                float chipNoise = frac(sin(dot(chip, float2(269.5,183.3))) * 43758.5453);
                float armor = (1 - energy) * step(0.12, max(c.r,max(c.g,c.b)));
                float scorch = step(1 - _Wear * 0.42, patchNoise) * armor;
                float scratchLine = 1 - step(2, fmod(pixel.x + floor(pixel.y / 6) * 3, 23));
                float scratches = scratchLine * step(1 - _Wear * 0.65, chipNoise) * armor;
                float chipped = step(1 - _Wear * 0.18, chipNoise) * scorch;
                c.rgb *= lerp(float3(1,1,1), float3(0.72,0.62,0.61), _Wear);
                c.rgb *= 1 - scorch * 0.65;
                c.rgb = lerp(c.rgb, float3(0.43,0.48,0.53), max(scratches * 0.65, chipped * 0.8));
                c.rgb *= 1 - _PowerLoss * energy * 0.96;
                c.rgb = lerp(c.rgb, float3(1,0.94,0.82), _FlashAmount);
                c.rgb *= 1 - _Shutdown * 0.86;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
