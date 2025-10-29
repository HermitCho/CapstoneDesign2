Shader "UI/HitDirectionOverlay"
{
    Properties
    {
        _HitDir ("Hit Direction", Vector) = (1,0,0,0)
        _Intensity ("Effect Intensity", Range(0,2)) = 1
        _ArcRadius ("Arc Radius", Range(0.1, 0.8)) = 0.4
        _ArcThickness ("Arc Thickness", Range(0.01, 0.2)) = 0.08
        _ArcAngle ("Arc Angle", Range(10, 120)) = 60
        _EdgeSharpness ("Edge Sharpness", Range(1, 50)) = 10
        _Color ("Hit Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "IgnoreProjector"="True" "PreviewType"="Plane" }
        
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

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
                float2 uv = (i.uv - 0.5) * 2.0;
                float dist = length(uv);

                float pixelAngle = atan2(uv.y, uv.x);
                float hitAngle = atan2(_HitDir.y, _HitDir.x);
                float angleDiff = pixelAngle - hitAngle;
                if (angleDiff > 3.14159) angleDiff -= 6.28318;
                if (angleDiff < -3.14159) angleDiff += 6.28318;
                float absAngleDiff = abs(angleDiff) * 57.2958;

                float arcHalfAngle = _ArcAngle * 0.5;
                float angleInRange = 1.0 - smoothstep(arcHalfAngle - 5.0, arcHalfAngle, absAngleDiff);

                float radiusStart = _ArcRadius - _ArcThickness * 0.5;
                float radiusEnd   = _ArcRadius + _ArcThickness * 0.5;
                float inner = smoothstep(radiusStart, radiusStart + 0.02, dist);   // 안쪽은 점점 연하게
                float outer = 1.0 - smoothstep(radiusEnd - 0.02, radiusEnd, dist); // 바깥은 진하게
                float radiusInRange = inner * outer;

                // 겉 진하게, 안쪽 연하게 그라데이션
                float fade = saturate((dist - radiusStart) / _ArcThickness);
                float edgeFade = lerp(0.4, 1.0, fade); 

                float alpha = angleInRange * radiusInRange * _Intensity * edgeFade;

                float2 screenEdgeFade = smoothstep(0.9, 1.0, abs(uv));
                alpha *= (1.0 - max(screenEdgeFade.x, screenEdgeFade.y));

                return float4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
