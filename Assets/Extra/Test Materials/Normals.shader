Shader "Unlit/Normals"
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
                half3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 normal : NORMAL;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = 0;
                color.rgb = i.normal * 0.5 + 0.5;
                return color;
            }
            ENDCG
        }
    }
}
