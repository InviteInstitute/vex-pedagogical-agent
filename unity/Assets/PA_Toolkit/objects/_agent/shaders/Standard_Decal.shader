Shader "Balance/Standard Decal"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        //Decal inclusion here
        _DecalTex0 ("Decal", 2D) = "black" {} 
        [ShowAsVector2] _Decal0_pos ("Position", Vector) = (0,0,0,0)
		_Decal0_rot ("Rotation", Range(0.0, 360)) = 0.0
		_Decal0_scale ("Scale", Float) = 1
		_Decal0_flipX ("Flip X", Range(-1,1)) = 1
		[Enum(UV2, 1, UV3, 2, UV4, 3)] _DecalIndex ("Decal UV Set", Float) = 1
      


        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" } 
        
        Cull Off

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

         //Decal Code here
        uniform sampler2D _DecalTex0;
        uniform half2 _Decal0_pos;
		uniform half _Decal0_rot;
		uniform half _Decal0_scale;
		uniform half _Decal0_flipX;
		uniform uint _DecalIndex;
        
        
        
        sampler2D _MainTex;
        uniform float _Cutoff;

        //The name structure uvX_TEX2DNAME matters
        //that pulls from the specific channel
        struct Input
        {
            float2 uv_MainTex;// : TEXCOORD0;
            float2 uv2_DecalTex0;
            float2 uv3_DecalTex0;
            float2 uv4_DecalTex0;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float2 GetDecalUV(Input i)
		{
			return  _DecalIndex == 1 ? i.uv2_DecalTex0 : 
                    _DecalIndex == 2 ? i.uv3_DecalTex0 : 
                    _DecalIndex == 3 ? i.uv4_DecalTex0 : 
                                       i.uv2_DecalTex0;
		}
        
        float2 TransformPoint(float2 uvPos)
        {
            //Generate Rotation
			float cosAngle_0 = cos(radians(_Decal0_rot));
			float sinAngle_0 = sin(radians(_Decal0_rot));
			float2x2 rot_0 = float2x2(cosAngle_0,-sinAngle_0,sinAngle_0,cosAngle_0);
			
			
			float2 pivot_0 = float2(0.5,0.5);
			float2 result = uvPos;
			
			//Move the texcoord centered on the bottom left corner
			result = result - pivot_0;
			//Apply Translation
			result = result - _Decal0_pos.xy;
			//Rotate the coord
		    result = mul(rot_0, result);
			//Scale
			result = result / _Decal0_scale;

			result.x = result.x * _Decal0_flipX;
			//Reset pivot
			result = result + pivot_0;
            return result;
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c =          tex2D (_MainTex,   IN.uv_MainTex) * _Color;
            
            if(c.a < _Cutoff)
            {
                discard;
            }

            fixed4 decalColor = tex2D (_DecalTex0, TransformPoint(GetDecalUV(IN)));
            o.Albedo = lerp(c.rgba, decalColor.rgba, decalColor.w);
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }


        ENDCG
    }
    FallBack "Diffuse"
}
