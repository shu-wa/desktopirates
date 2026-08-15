Shader "Desktopirates/Ocean"
{
    Properties
    {
        _MainTex ("Faceted Surface", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.05, 0.34, 0.40, 1)
        _DeepColor ("Deep Color", Color) = (0.018, 0.12, 0.16, 1)
        _FoamColor ("Foam Color", Color) = (0.62, 0.86, 0.82, 1)
        _VoyageOffset ("Voyage Offset", Vector) = (0, 0, 0, 0)
        _VoyageDirection ("Voyage Direction", Vector) = (0, 1, 0, 0)
        _VoyageSpeed ("Voyage Speed", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Tint;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        float4 _VoyageOffset;
        float4 _VoyageDirection;
        float _VoyageSpeed;

        struct Input
        {
            fixed4 color : COLOR;
            float2 uv_MainTex;
            float4 screenPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 uv = IN.uv_MainTex * 1.25 + _VoyageOffset.xy;
            float2 snappedA = floor((uv + _Time.y * float2(0.012, 0.006)) * 128.0) / 128.0;
            float2 snappedB = floor((uv * 0.57 - _Time.y * float2(0.004, 0.009)) * 96.0) / 96.0;
            fixed3 a = tex2D(_MainTex, snappedA).rgb;
            fixed3 b = tex2D(_MainTex, snappedB).rgb;
            float waveLight = saturate(dot(a, fixed3(0.23, 0.62, 0.15)) * 2.15);
            float secondary = saturate(dot(b, fixed3(0.2, 0.65, 0.15)) * 1.75);
            fixed3 water = lerp(_DeepColor.rgb * 1.15, _Tint.rgb * 1.48, saturate(0.12 + waveLight * 0.70 + secondary * 0.24));
            water *= lerp(0.72, 1.30, saturate(a.g * 2.25));
            float foam = smoothstep(0.44, 0.74, max(max(a.r, a.g), a.b));
            float2 local = IN.uv_MainTex - 0.5;
            float2 forward = normalize(_VoyageDirection.xy + float2(0.0001, 0.0001));
            float bow = (1.0 - smoothstep(0.025, 0.060, length(local - forward * 0.045))) * _VoyageSpeed;
            float2 pixel = floor((IN.screenPos.xy / IN.screenPos.w) * _ScreenParams.xy);
            float dither = fmod(pixel.x + pixel.y * 2.0, 4.0) < 1.0 ? 0.022 : -0.006;
            o.Albedo = saturate((water + dither) * IN.color.rgb);
            // Persistent wake belongs to ShipWakeTrailController, which records actual
            // sailed coordinates. The ocean shader keeps only the local bow disturbance.
            o.Emission = water * 0.30 + _FoamColor.rgb * (foam * 0.52 + bow * 0.58);
            o.Specular = 0.18;
            o.Gloss = 0.22;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
