Shader "Hidden/Beat Saber Bloom"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_BloomLowTexture);
        TEXTURE2D_X_FLOAT(_BloomCameraDepthTexture);
        float4 _BloomLowTexture_TexelSize;
        float2 _BloomCombine;
        float _SampleScale;
        float _BloomThreshold;
        float _BloomIntensity;
        float _BaseColorBoost;
        float _BaseColorBoostThreshold;

        float MaskBackgroundAlpha(float2 uv, float alpha)
        {
            float depth = SAMPLE_TEXTURE2D_X(
                _BloomCameraDepthTexture, sampler_PointClamp, uv).r;
            #if UNITY_REVERSED_Z
                return depth <= 0.0001 ? 0.0 : alpha;
            #else
                return depth >= 0.9999 ? 0.0 : alpha;
            #endif
        }

        float4 SampleSource(float2 uv, float2 offset)
        {
            float2 sampleUv = saturate(uv + offset * _BlitTexture_TexelSize.xy);
            float4 color = SAMPLE_TEXTURE2D_X(
                _BlitTexture, sampler_LinearClamp, sampleUv);
            color.a = MaskBackgroundAlpha(sampleUv, color.a);
            return color;
        }

        float4 Downsample13(float2 uv)
        {
            float4 a0 = SampleSource(uv, float2( 0.5,  0.5));
            float4 a1 = SampleSource(uv, float2(-0.5,  0.5));
            float4 a2 = SampleSource(uv, float2( 0.5, -0.5));
            float4 a3 = SampleSource(uv, float2(-0.5, -0.5));
            float4 c0 = SampleSource(uv, float2(-1.0, -1.0));
            float4 c1 = SampleSource(uv, float2( 0.0, -1.0));
            float4 c2 = SampleSource(uv, float2( 1.0, -1.0));
            float4 c3 = SampleSource(uv, float2(-1.0,  0.0));
            float4 c4 = SampleSource(uv, 0.0);
            float4 c5 = SampleSource(uv, float2( 1.0,  0.0));
            float4 c6 = SampleSource(uv, float2(-1.0,  1.0));
            float4 c7 = SampleSource(uv, float2( 0.0,  1.0));
            float4 c8 = SampleSource(uv, float2( 1.0,  1.0));

            return (a0 + a1 + a2 + a3) * 0.125 +
                   (c0 + c2 + c6 + c8) * 0.03125 +
                   (c1 + c3 + c5 + c7) * 0.0625 +
                   c4 * 0.125;
        }

        float4 SampleTent(float2 uv, float2 offset)
        {
            return SAMPLE_TEXTURE2D_X(_BloomLowTexture, sampler_LinearClamp,
                saturate(uv + offset * _BloomLowTexture_TexelSize.xy * _SampleScale));
        }

        float4 Tent9(float2 uv)
        {
            float4 corners = SampleTent(uv, float2(-1.0,  1.0)) +
                             SampleTent(uv, float2( 1.0,  1.0)) +
                             SampleTent(uv, float2(-1.0, -1.0)) +
                             SampleTent(uv, float2( 1.0, -1.0));
            float4 edges = SampleTent(uv, float2( 0.0,  1.0)) +
                           SampleTent(uv, float2(-1.0,  0.0)) +
                           SampleTent(uv, float2( 1.0,  0.0)) +
                           SampleTent(uv, float2( 0.0, -1.0));
            float4 center = SampleTent(uv, 0.0);
            return (corners + edges * 2.0 + center * 4.0) * (1.0 / 16.0);
        }

        float4 FragPrefilter(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float4 color = Downsample13(uv);
            color.rgb *= saturate(color.a * _BloomThreshold);
            return color;
        }

        float4 FragDownsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return Downsample13(UnityStereoTransformScreenSpaceTex(input.texcoord));
        }

        float4 FragUpsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float4 high = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            return high * _BloomCombine.x + Tent9(uv) * _BloomCombine.y;
        }

        float4 FragComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            float2 texelSize = _BlitTexture_TexelSize.xy;
            float2 alphaUv0 = saturate(uv + texelSize * float2(-0.5, 0.5));
            float2 alphaUv1 = saturate(uv + texelSize * float2(0.0, -0.5));
            float2 alphaUv2 = saturate(uv + texelSize * float2(0.5, 0.5));
            float alpha = MaskBackgroundAlpha(
                              alphaUv0,
                              SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, alphaUv0).a) +
                          MaskBackgroundAlpha(
                              alphaUv1,
                              SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, alphaUv1).a) +
                          MaskBackgroundAlpha(
                              alphaUv2,
                              SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, alphaUv2).a) +
                          MaskBackgroundAlpha(uv, scene.a);
            alpha *= 0.25;
            float whiteBoost = alpha * alpha * _BaseColorBoost - _BaseColorBoostThreshold;
            float3 boostedScene = saturate(scene.rgb + whiteBoost);
            float3 bloom = SAMPLE_TEXTURE2D_X(_BloomLowTexture, sampler_LinearClamp, uv).rgb;
            return float4(boostedScene + bloom * _BloomIntensity, scene.a);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Alpha-gated prefilter and 13-tap downsample"
            HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex Vert
                #pragma fragment FragPrefilter
            ENDHLSL
        }

        Pass
        {
            Name "13-tap downsample"
            HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex Vert
                #pragma fragment FragDownsample
            ENDHLSL
        }

        Pass
        {
            Name "Tent upsample and weighted merge"
            HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex Vert
                #pragma fragment FragUpsample
            ENDHLSL
        }

        Pass
        {
            Name "Scene and bloom composite"
            HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex Vert
                #pragma fragment FragComposite
            ENDHLSL
        }
    }
}
