Shader "Custom/IrisWipe"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _Color ("Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _Progress;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center UVs at 0,0
                float2 centered = i.uv - 0.5;
                // Account for aspect ratio
                centered.x *= _ScreenParams.x / _ScreenParams.y;
                // Distance from center
                float dist = length(centered);
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float maxRadius = length(float2(0.5 * aspect, 0.5)); // Distance from center to corner
                float radius = (1.0 - _Progress) * maxRadius;
                // Black outside the circle
                float alpha = dist > radius ? 1.0 : 0.0;
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}