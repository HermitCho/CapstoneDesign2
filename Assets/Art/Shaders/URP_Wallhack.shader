Shader "Custom/URP_Wallhack_Unlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,0,0,1)
        _EmissionColor ("Emission Color", Color) = (1,0,0,1)
        _MainTex ("MainTex", 2D) = "white" {}
    }

    SubShader
    {
        // 투명/오버레이 계열 (카메라에 가장 나중에 그려지게 함)
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }

        // 한 패스 : 깊이 테스트 무시, 깊이 기록 끔
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _BaseColor;
            float4 _EmissionColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _BaseColor;
                // emission만 보이게 하고 싶으면 _EmissionColor만 리턴해도 됩니다.
                return lerp(col, _EmissionColor, 0.9); 
            }
            ENDHLSL
        }
    }
    FallBack "Unlit/Color"
}
