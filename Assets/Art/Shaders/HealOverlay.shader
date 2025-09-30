Shader "UI/HealOverlayBorder"
{
    Properties
    {
        _Intensity ("Effect Intensity", Range(0,2)) = 1
        _BorderThickness ("Border Thickness", Range(0.01, 0.5)) = 0.15
        _Color ("Heal Color", Color) = (0.3,1,0.3,0.6)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            float _Intensity;
            float _BorderThickness;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 화면 경계와의 거리 계산 (좌우/상하)
                float2 distToEdge = min(uv, 1.0 - uv);

                // 가장자리에서 가까울수록 알파 값 ↑
                float edgeMask = smoothstep(_BorderThickness, 0.0, min(distToEdge.x, distToEdge.y));

                float alpha = edgeMask * _Intensity * _Color.a;

                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
