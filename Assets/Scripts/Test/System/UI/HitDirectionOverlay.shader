Shader "UI/HitDirectionOverlay"
{
    Properties
    {
        _HitDir ("Hit Direction", Vector) = (1,0,0,0)
        _Intensity ("Effect Intensity", Range(0,2)) = 1
        _Spread ("Spread", Range(0.1, 2)) = 0.5
        _EdgeFade ("Edge Fade", Range(0.01, 1)) = 0.3
        _Color ("Hit Color", Color) = (1,0,0,0.8)
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

            float4 _HitDir;
            float _Intensity;
            float _Spread;
            float _EdgeFade;
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
                // UV 좌표를 -1~1 범위로 변환 (중심이 0,0)
                float2 uv = (i.uv - 0.5) * 2.0;
                
                // 중심으로부터의 거리
                float dist = length(uv);
                
                // 픽셀의 방향 벡터 (정규화)
                float2 pixelDir = normalize(uv);
                
                // 공격 방향 벡터 (정규화)
                float2 hitDir = normalize(_HitDir.xy);
                
                // 방향 일치도 계산 (내적)
                float directionMatch = dot(pixelDir, hitDir);
                
                // 음수 값을 0으로 클램프 (반대 방향은 제거)
                directionMatch = max(directionMatch, 0.0);
                
                // 방향성 강화 (Spread로 조절)
                directionMatch = pow(directionMatch, 1.0 / max(_Spread, 0.1));
                
                // 가장자리 효과 (중심에서 멀수록 강해짐)
                float edgeEffect = smoothstep(0.0, 1.0, dist);
                
                // EdgeFade 적용
                edgeEffect = smoothstep(1.0 - _EdgeFade, 1.0, edgeEffect);
                
                // 최종 알파 계산
                float alpha = directionMatch * edgeEffect * _Intensity;
                
                return float4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}