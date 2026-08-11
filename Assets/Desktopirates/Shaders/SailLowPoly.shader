Shader "Desktopirates/Sail"
{
    Properties { _Color ("Canvas Color", Color) = (0.82, 0.72, 0.52, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float2 pixel : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.pixel = o.vertex.xy; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float pattern = fmod(floor(i.pixel.x) + floor(i.pixel.y), 3.0) < 1.0 ? 0.92 : 1.04;
                return fixed4(_Color.rgb * pattern, 1);
            }
            ENDCG
        }
    }
}
