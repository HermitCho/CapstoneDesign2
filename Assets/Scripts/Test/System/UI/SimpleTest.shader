Shader "UI/SimpleTest"
{
    Properties
    {
        _Intensity ("Intensity", Range(0,1)) = 1
        _Color ("Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Overlay"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Intensity;
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
                // 단순히 색상과 강도만 적용
                return float4(_Color.rgb, _Intensity * _Color.a);
            }
            ENDCG
        }
    }
}