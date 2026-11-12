Shader "TL4/SiegeCoreContinuousBeam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _BeamAtlas ("Original animated beam artwork", 2D) = "white" {}
        _FrameRect ("Beam middle UV rectangle", Vector) = (0,0,1,1)
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowTime ("Scaled attack time", Float) = 0
        _BeamLength ("Beam length", Float) = 1
        _Direction ("Flow direction", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            sampler2D _BeamAtlas;
            fixed4 _Color;
            float4 _FrameRect;
            float _FlowTime, _BeamLength, _Direction;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the original pixel-art frame, using only its gapless middle.
                // Projectile tails/tips and their transparent padding never repeat along the shot.
                float along=frac(i.uv.x*_BeamLength/1.3-_FlowTime*_Direction*2.5);
                float2 uv=_FrameRect.xy+float2(along,saturate(i.uv.y))*_FrameRect.zw;
                fixed4 c=tex2D(_BeamAtlas,uv)*i.color;
                c.rgb*=c.a;
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
