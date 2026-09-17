Shader "BeatSaber/Skybox"
{
    Properties
    {
		_MainTex ("Texture", 2D) = "white" { }
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Unlit"
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 position_os : POSITION;
                float4 color : COLOR;
                float2 tex_coord : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position_cs : SV_POSITION;
                float4 color : COLOR;
                float2 tex_coord : TEXCOORD0;
                
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            
            float4 _MainTex_ST;
            float4 _Color;
            
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.position_cs = TransformObjectToHClip(input.position_os.xyz);
                output.color = _Color;
                output.tex_coord = TRANSFORM_TEX(input.tex_coord, _MainTex);
                return output;
            }

            half4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.tex_coord);
                return tex * half4(1, 1, 1, 0) * input.color;
            }

            ENDHLSL
        }
    }
}
