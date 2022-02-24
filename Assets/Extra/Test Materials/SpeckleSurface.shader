Shader "Custom/SpeckleSurface"
{
    Properties
    {
        [Header(Cube)]
        _Thickness("Thickness", Range(0, 1)) = 0.32
        _Margin("Margin", Range(0, 1)) = 0.15
        _NormalSaturation("Normal Saturation", range(0,1)) = 0.03
        _SmoothnessSaturation("Smoothness Saturation", range(0,1)) = 0.02
        
        [Header(Tiling)]
        _Mul("Multiple", int) = 8192
        _GridSize("Grid Size", int) = 128
        
        _Color0("Color 0", Color) = (0.6, 0.6, 0.6, 1)
        _Color1("Color 1", Color) = (0.57, 0.57, 0.57, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        //#pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        
		float _Thickness;
        float _Margin;
		int _Mul;
        int _GridSize;

        fixed4 _Color0;
        fixed4 _Color1;
        float _NormalSaturation;
        float _SmoothnessSaturation;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        //UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        //UNITY_INSTANCING_BUFFER_END(Props)


        fixed speckleSample(float2 pos) 
        {
            // sample the texture
            const fixed top  = +0.5;
            const fixed left = -1;
            const fixed base = -0.5;

            bool invert;
            { //Fine Grid
                float2 grid_pos = (pos + 0.5) * _Mul / 2.0;
                grid_pos = grid_pos - floor(grid_pos);
                invert = grid_pos.x > 0.5 ^ grid_pos.y > 0.5;
             }
            
            
            float2 cell_pos = (pos + 0.5) * _Mul;
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

            if(invert) grey = grey - 1;
            
            //Ears
            if( cell_pos.x + cell_pos.y < _Thickness
            || 1.0 - cell_pos.x + 1.0 - cell_pos.y < _Thickness)
            {
               grey = 0; 
            }
            
            //Margins
            const bool inCube = cell_pos.x > _Margin && cell_pos.y < 1.0 -_Margin;
            if(!inCube) grey = 0;                               

            return grey;                
        }
        
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 background = _Color0;
            
            //Coarse grid
             float2 grid_pos = (IN.uv_MainTex + 0.5) * _Mul / _GridSize;
             grid_pos = grid_pos - floor(grid_pos);
             if(grid_pos.x > 0.5 ^ grid_pos.y > 0.5)
             {
                 background = _Color1;
             }
             
            const half grey = speckleSample(IN.uv_MainTex);
            
            o.Albedo = background;
            o.Metallic = 0.0;
            o.Normal = o.Normal + (grey + 1) * _NormalSaturation;
            o.Smoothness = grey * _SmoothnessSaturation;
            o.Alpha = background.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
