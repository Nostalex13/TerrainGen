// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "AmplifyShader"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 5
		_Mask("Mask", 2D) = "white" {}
		_TextureSample12("Texture Sample 12", 2D) = "white" {}
		_Float0("Float 0", Range( 0 , 1)) = 0
		_TextureSample13("Texture Sample 13", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Geometry+0" }
		Cull Back
		Stencil
		{
			Ref 0
		}
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _TextureSample12;
		uniform float4 _TextureSample12_ST;
		uniform sampler2D _TextureSample13;
		uniform float4 _TextureSample13_ST;
		uniform float _Float0;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform float _Cutoff = 5;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_TextureSample12 = i.uv_texcoord * _TextureSample12_ST.xy + _TextureSample12_ST.zw;
			float4 tex2DNode4 = tex2D( _TextureSample12, uv_TextureSample12 );
			float2 uv_TextureSample13 = i.uv_texcoord * _TextureSample13_ST.xy + _TextureSample13_ST.zw;
			float4 tex2DNode11 = tex2D( _TextureSample13, uv_TextureSample13 );
			float4 lerpResult13 = lerp( tex2DNode4 , tex2DNode11 , _Float0);
			o.Albedo = lerpResult13.rgb;
			o.Alpha = 1;
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float4 tex2DNode1 = tex2D( _Mask, uv_Mask );
			float4 lerpResult12 = lerp( ( 1.0 - tex2DNode1 ) , tex2DNode11 , _Float0);
			clip( lerpResult12.r - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
2009;34;1668;920;1496.286;465.7885;1.42249;True;True
Node;AmplifyShaderEditor.SamplerNode;1;-918.6527,14.59099;Inherit;True;Property;_Mask;Mask;1;0;Create;True;0;0;0;False;0;False;-1;78fbd26fd7a4ece45aca2fe7346b53ba;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;9;-821.0767,458.6613;Inherit;False;Property;_Float0;Float 0;3;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-978.4711,-241.3696;Inherit;True;Property;_TextureSample12;Texture Sample 12;2;0;Create;True;0;0;0;False;0;False;-1;108b19b88ad425c4fb94b7f8484fbcf1;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;8;-564.223,124.1427;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;11;-853.0028,248.9425;Inherit;True;Property;_TextureSample13;Texture Sample 13;4;0;Create;True;0;0;0;False;0;False;-1;9cb497824af2b974b88a0c63656084c1;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;12;-269.9314,189.0226;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-574.5248,-64.41248;Inherit;False;2;2;0;COLOR;1,1,1,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;13;-291.437,-76.02629;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;AmplifyShader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;5;True;True;0;False;Transparent;;Geometry;All;14;all;True;True;True;True;0;False;-1;True;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;8;0;1;0
WireConnection;12;0;8;0
WireConnection;12;1;11;0
WireConnection;12;2;9;0
WireConnection;3;0;4;0
WireConnection;3;1;1;0
WireConnection;13;0;4;0
WireConnection;13;1;11;0
WireConnection;13;2;9;0
WireConnection;0;0;13;0
WireConnection;0;10;12;0
ASEEND*/
//CHKSM=6B3CF78AF049EBE42C22E2B133B61C4A1FDE9D93