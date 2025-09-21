Shader "UI/HitDirectionOverlay"
{
    Properties
    {
        _HitDir ("Hit Direction", Vector) = (1,0,0,0)
        _Intensity ("Effect Intensity", Range(0,2)) = 1
        _ArcRadius ("Arc Radius", Range(0.3, 0.9)) = 0.7
        _ArcThickness ("Arc Thickness", Range(0.01, 0.2)) = 0.05
        _ArcAngle ("Arc Angle", Range(10, 120)) = 60
        _EdgeSharpness ("Edge Sharpness", Range(1, 50)) = 10
        _Color ("Hit Color", Color) = (1,0,0,1)
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
            float _ArcRadius;
            float _ArcThickness;
            float _ArcAngle;
            float _EdgeSharpness;
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
                
                // 픽셀의 각도 계산 (atan2 사용)
                float pixelAngle = atan2(uv.y, uv.x);
                
                // 공격 방향의 각도 계산
                float hitAngle = atan2(_HitDir.y, _HitDir.x);
                
                // 각도 차이 계산 (-π ~ π 범위)
                float angleDiff = pixelAngle - hitAngle;
                
                // 각도를 -π ~ π 범위로 정규화
                if (angleDiff > 3.14159) angleDiff -= 6.28318;
                if (angleDiff < -3.14159) angleDiff += 6.28318;
                
                // 각도를 절댓값으로 변환하고 도 단위로 변환
                float absAngleDiff = abs(angleDiff) * 57.2958; // 라디안을 도로 변환
                
                // 원호 각도 범위 체크
                float arcHalfAngle = _ArcAngle * 0.5;
                float angleInRange = 1.0 - smoothstep(arcHalfAngle - 5.0, arcHalfAngle, absAngleDiff);
                
                // 원주 거리 체크 (지정된 반지름 근처에서만 표시)
                float radiusStart = _ArcRadius - _ArcThickness * 0.5;
                float radiusEnd = _ArcRadius + _ArcThickness * 0.5;
                
                float radiusInRange = 1.0;
                radiusInRange *= smoothstep(radiusStart - 0.02, radiusStart, dist);
                radiusInRange *= (1.0 - smoothstep(radiusEnd, radiusEnd + 0.02, dist));
                
                // 가장자리 선명도 적용
                angleInRange = pow(angleInRange, _EdgeSharpness);
                radiusInRange = pow(radiusInRange, _EdgeSharpness);
                
                // 최종 알파 계산
                float alpha = angleInRange * radiusInRange * _Intensity;
                
                // 화면 가장자리에서 페이드 아웃
                float2 screenEdgeFade = smoothstep(0.9, 1.0, abs(uv));
                float edgeFade = 1.0 - max(screenEdgeFade.x, screenEdgeFade.y);
                alpha *= edgeFade;
                
                return float4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}