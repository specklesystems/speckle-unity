Shader "Unlit/UVs"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 colors : TEXCOORD0;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.colors = v.uv;
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return half4(i.colors.x, i.colors.y, 0, 0);
            }
            ENDCG
        }
    }
}
