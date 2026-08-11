Shader "Desktopirates/Ocean"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.05, 0.34, 0.40, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0

        fixed4 _Tint;

        struct Input
        {
            fixed4 color : COLOR;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Tint.rgb * IN.color.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
