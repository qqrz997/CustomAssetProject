using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Settings
{
    /// <summary>Adds the Beat Saber-style bloom pyramid and composite to the Unity Scene view camera.</summary>
    public sealed class BeatSaberBloomRendererFeature : ScriptableRendererFeature
    {
        private const string MainEffectKeyword = "ENABLE_MAIN_EFFECT";

        [SerializeField] private Shader bloomShader;
        [SerializeField, Min(1)] private int bloomWidth = 928;
        [SerializeField, Range(0f, 10f)] private float bloomRadius = 5f;
        [SerializeField, Min(0f)] private float bloomIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float bloomBlend = 0.3f;
        [SerializeField, Min(0f)] private float bloomThreshold = 4f;
        [SerializeField, Range(0f, 3f)] private float baseColorBoost = 1f;
        [SerializeField, Min(0f)] private float baseColorBoostThreshold;

        private Material material;
        private BloomPass pass;
        private bool subscribed;

        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (bloomShader != null)
            {
                material = CoreUtils.CreateEngineMaterial(bloomShader);
            }

            pass = new BloomPass(material)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };

            if (!subscribed)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
                subscribed = true;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null ||
                !renderingData.cameraData.isSceneViewCamera)
            {
                return;
            }

            pass.Setup(
                bloomWidth,
                bloomRadius,
                bloomIntensity,
                bloomBlend,
                bloomThreshold,
                baseColorBoost,
                baseColorBoostThreshold);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            if (subscribed)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
                subscribed = false;
            }

            Shader.DisableKeyword(MainEffectKeyword);
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext _, Camera camera)
        {
            if (camera != null && camera.cameraType == CameraType.SceneView)
            {
                Shader.EnableKeyword(MainEffectKeyword);
            }
            else
            {
                Shader.DisableKeyword(MainEffectKeyword);
            }
        }

        private static void OnEndCameraRendering(ScriptableRenderContext _, Camera __) =>
            Shader.DisableKeyword(MainEffectKeyword);

        private sealed class BloomPass : ScriptableRenderPass
        {
            private const int MaxPyramidLevels = 16;
            private static readonly int BloomLowTextureId = Shader.PropertyToID("_BloomLowTexture");
            private static readonly int BloomCombineId = Shader.PropertyToID("_BloomCombine");
            private static readonly int SampleScaleId = Shader.PropertyToID("_SampleScale");
            private static readonly int BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
            private static readonly int BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
            private static readonly int BaseColorBoostId = Shader.PropertyToID("_BaseColorBoost");
            private static readonly int BaseColorBoostThresholdId = Shader.PropertyToID("_BaseColorBoostThreshold");
            private static readonly int CameraDepthTextureId = Shader.PropertyToID("_BloomCameraDepthTexture");

            private readonly TextureHandle[] down = new TextureHandle[MaxPyramidLevels];
            private readonly TextureHandle[] up = new TextureHandle[MaxPyramidLevels];
            private readonly Material material;
            private int bloomWidth;
            private float bloomRadius;
            private float bloomIntensity;
            private float bloomBlend;
            private float bloomThreshold;
            private float baseColorBoost;
            private float baseColorBoostThreshold;

            public BloomPass(Material material)
            {
                this.material = material;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Setup(
                int bloomWidth,
                float bloomRadius,
                float bloomIntensity,
                float bloomBlend,
                float bloomThreshold,
                float baseColorBoost,
                float baseColorBoostThreshold)
            {
                this.bloomWidth = Mathf.Max(1, bloomWidth);
                this.bloomRadius = Mathf.Clamp(bloomRadius, 0f, 10f);
                this.bloomIntensity = Mathf.Max(0f, bloomIntensity);
                this.bloomBlend = Mathf.Clamp01(bloomBlend);
                this.bloomThreshold = Mathf.Max(0f, bloomThreshold);
                this.baseColorBoost = Mathf.Max(0f, baseColorBoost);
                this.baseColorBoostThreshold = Mathf.Max(0f, baseColorBoostThreshold);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                TextureHandle depth = resourceData.activeDepthTexture;
                TextureDesc sourceDesc = source.GetDescriptor(renderGraph);
                int firstWidth = Mathf.Clamp(bloomWidth, 1, sourceDesc.width);
                int firstHeight = Mathf.Max(1, Mathf.RoundToInt(firstWidth * sourceDesc.height / (float)sourceDesc.width));
                CalculatePyramidParameters(
                    firstWidth,
                    firstHeight,
                    bloomRadius,
                    out int levelCount,
                    out float sampleScale);

                TextureDesc bloomDesc = sourceDesc;
                bloomDesc.width = firstWidth;
                bloomDesc.height = firstHeight;
                bloomDesc.depthBufferBits = DepthBits.None;
                bloomDesc.msaaSamples = MSAASamples.None;
                bloomDesc.useMipMap = false;
                bloomDesc.autoGenerateMips = false;
                bloomDesc.filterMode = FilterMode.Bilinear;
                bloomDesc.wrapMode = TextureWrapMode.Clamp;
                bloomDesc.clearBuffer = false;

                for (int i = 0; i < levelCount; i++)
                {
                    bloomDesc.name = $"BeatSaberBloom Down {i}";
                    down[i] = renderGraph.CreateTexture(bloomDesc);

                    if (i < levelCount - 1)
                    {
                        bloomDesc.name = $"BeatSaberBloom Up {i}";
                        up[i] = renderGraph.CreateTexture(bloomDesc);
                    }

                    bloomDesc.width = Mathf.Max(1, bloomDesc.width >> 1);
                    bloomDesc.height = Mathf.Max(1, bloomDesc.height >> 1);
                }

                TextureDesc compositeDesc = sourceDesc;
                compositeDesc.name = "BeatSaberBloom Composite";
                compositeDesc.depthBufferBits = DepthBits.None;
                compositeDesc.msaaSamples = MSAASamples.None;
                compositeDesc.clearBuffer = false;
                TextureHandle composite = renderGraph.CreateTexture(compositeDesc);

                using (IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass("Beat Saber Bloom", out PassData passData))
                {
                    passData.Material = material;
                    passData.Source = source;
                    passData.Depth = depth;
                    passData.Composite = composite;
                    passData.Down = down;
                    passData.Up = up;
                    passData.LevelCount = levelCount;
                    passData.BloomBlend = bloomBlend;
                    passData.AlphaWeight = bloomThreshold;
                    passData.PyramidIntensity = bloomIntensity;
                    passData.SampleScale = sampleScale;
                    passData.BaseColorBoost = baseColorBoost;
                    passData.BaseColorBoostThreshold = baseColorBoostThreshold;

                    builder.UseTexture(source);
                    builder.UseTexture(depth);
                    builder.UseTexture(composite, AccessFlags.Write);
                    for (int i = 0; i < levelCount; i++)
                    {
                        builder.UseTexture(down[i], AccessFlags.ReadWrite);
                        if (i < levelCount - 1)
                        {
                            builder.UseTexture(up[i], AccessFlags.ReadWrite);
                        }
                    }

                    builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) => Execute(data, context));
                }

                resourceData.cameraColor = composite;
            }

            private static void CalculatePyramidParameters(
                int width,
                int height,
                float radius,
                out int levelCount,
                out float sampleScale)
            {
                float levels = Mathf.Log(Mathf.Max(width, height), 2f) + Mathf.Min(radius, 10f) - 10f;
                int wholeLevels = Mathf.FloorToInt(levels);
                levelCount = Mathf.Clamp(wholeLevels, 1, MaxPyramidLevels);
                sampleScale = 0.5f + levels - wholeLevels;
            }

            private static void Execute(PassData data, UnsafeGraphContext context)
            {
                CommandBuffer commandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                const RenderBufferLoadAction loadAction = RenderBufferLoadAction.DontCare;
                const RenderBufferStoreAction storeAction = RenderBufferStoreAction.Store;

                commandBuffer.SetGlobalFloat(BloomThresholdId, data.AlphaWeight);
                commandBuffer.SetGlobalTexture(CameraDepthTextureId, data.Depth);
                Blitter.BlitCameraTexture(commandBuffer, data.Source, data.Down[0], loadAction, storeAction, data.Material, 0);

                for (int i = 1; i < data.LevelCount; i++)
                {
                    Blitter.BlitCameraTexture(commandBuffer, data.Down[i - 1], data.Down[i], loadAction, storeAction, data.Material, 1);
                }

                TextureHandle bloom = data.Down[data.LevelCount - 1];
                commandBuffer.SetGlobalFloat(SampleScaleId, data.SampleScale);
                for (int i = data.LevelCount - 2; i >= 0; i--)
                {
                    commandBuffer.SetGlobalTexture(BloomLowTextureId, bloom);
                    float highWeight = Mathf.Min(
                        1f,
                        Mathf.Pow(data.PyramidIntensity * (i + 1f) / (data.LevelCount - 1f), 0.01f));
                    float lowWeight = Mathf.Min(1f, 2f - highWeight);
                    commandBuffer.SetGlobalVector(BloomCombineId, new Vector4(highWeight, lowWeight, 0f, 0f));
                    Blitter.BlitCameraTexture(commandBuffer, data.Down[i], data.Up[i], loadAction, storeAction, data.Material, 2);
                    bloom = data.Up[i];
                }

                commandBuffer.SetGlobalTexture(BloomLowTextureId, bloom);
                commandBuffer.SetGlobalFloat(BloomIntensityId, data.BloomBlend);
                commandBuffer.SetGlobalFloat(BaseColorBoostId, data.BaseColorBoost);
                commandBuffer.SetGlobalFloat(BaseColorBoostThresholdId, data.BaseColorBoostThreshold);
                Blitter.BlitCameraTexture(commandBuffer, data.Source, data.Composite, loadAction, storeAction, data.Material, 3);
            }

            private sealed class PassData
            {
                public Material Material;
                public TextureHandle Source;
                public TextureHandle Depth;
                public TextureHandle Composite;
                public TextureHandle[] Down;
                public TextureHandle[] Up;
                public int LevelCount;
                public float BloomBlend;
                public float AlphaWeight;
                public float PyramidIntensity;
                public float SampleScale;
                public float BaseColorBoost;
                public float BaseColorBoostThreshold;
            }
        }
    }
}
