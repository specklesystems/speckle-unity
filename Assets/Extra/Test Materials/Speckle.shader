Shader "Unlit/Speckle"
{
    Properties
    {
        _Thickness("Thickness", Range(0, 1)) = 0.32
        _Margin("Margin", Range(0, 1)) = 0.15
        _Mul("Multiple", int) = 32
        _GridSize("Grid Size", int) = 32
        _Color0("Color 0", Color) = (0.8, 0.8, 0.8, 1)
        _Color1("Color 1", Color) = (0.85, 0.85, 0.85, 1)
        _Saturation("Saturation", range(0,1)) = 0.05
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
            // make fog work
            #pragma multi_compile_fog
            #pragma target 2.0
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float2 tex_coord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

		    float _Thickness;
            float _Margin;
		    int _Mul;
            int _GridSize;

            fixed4 _Color0;
            fixed4 _Color1;
            float _Saturation;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.tex_coord = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed top  = +0.5;
                fixed left = -1;
                fixed base = -0.5;
                fixed4 background = _Color0;


                { //Coarse grid
                    float2 grid_pos = (i.tex_coord + 0.5) * _Mul / _GridSize;
                     grid_pos = grid_pos - floor(grid_pos);
                     if(grid_pos.x > 0.5 ^ grid_pos.y > 0.5)
                     {
                         background = _Color1;
                     }
                 }
                
                { //Fine Grid
                    float2 grid_pos = (i.tex_coord + 0.5) * _Mul / 2.0;
                     grid_pos = grid_pos - floor(grid_pos);
                     if(grid_pos.x > 0.5 ^ grid_pos.y > 0.5)
                    {
                        top  = -top;
                        left = -left;
                        base = -base;
                    }
                 }
                
                
                float2 cell_pos = (i.tex_coord + 0.5) * _Mul;
                cell_pos = cell_pos - floor(cell_pos);
                //cell_pos between 0-1 for each Speckle cube

                fixed grey;
                
                //Cube
                {
                    if(cell_pos.x + cell_pos.y < 1.0)
                        grey = left;
                    else
                        grey = top;
                    
                    if(cell_pos.x > _Thickness && cell_pos.y < 1.0 -_Thickness)
                       grey = base;
                }

                //Ears
                if( cell_pos.x + cell_pos.y < _Thickness
                || 1.0 - cell_pos.x + 1.0 - cell_pos.y < _Thickness)
                {
                   grey = 0; 
                }
                
                //Margins
                const bool inCube = cell_pos.x > _Margin && cell_pos.y < 1.0 -_Margin;
                if(!inCube) grey = 0;                               

                fixed4 col = background += grey * _Saturation;
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
}
