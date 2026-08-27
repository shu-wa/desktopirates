Shader "Hidden/Desktopirates/PixelWorldMask"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _MaskCenter ("Mask Center", Vector) = (0.5, 0.5, 0, 0)
        _MaskRadius ("Mask Radius", Vector) = (0.4, 0.3, 0, 0)
        _KeyColor ("Transparent Key", Color) = (1, 0, 1, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MaskCenter;
            float4 _MaskRadius;
            fixed4 _KeyColor;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 normalized = (input.uv - _MaskCenter.xy) / max(_MaskRadius.xy, float2(0.0001, 0.0001));
                if (dot(normalized, normalized) > 1.0) return _KeyColor;
                return tex2D(_MainTex, input.uv);
            }
            ENDCG
        }
    }
}
