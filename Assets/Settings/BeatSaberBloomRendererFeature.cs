using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BeatSaberBloom
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

        private Material m_Material;
        private BloomPass m_Pass;
        private bool m_Subscribed;

        public override void Create()
        {
            CoreUtils.Destroy(m_Material);
            if (bloomShader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(bloomShader);
            }

            m_Pass = new BloomPass(m_Material)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };

            if (!m_Subscribed)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
                m_Subscribed = true;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Material == null ||
                !renderingData.cameraData.isSceneViewCamera)
            {
                return;
            }

            m_Pass.Setup(
                bloomWidth,
                bloomRadius,
                bloomIntensity,
                bloomBlend,
                bloomThreshold,
                baseColorBoost,
                baseColorBoostThreshold);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            if (m_Subscribed)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
                m_Subscribed = false;
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

            private readonly TextureHandle[] m_Down = new TextureHandle[MaxPyramidLevels];
            private readonly TextureHandle[] m_Up = new TextureHandle[MaxPyramidLevels];
            private readonly Material m_Material;
            private int m_BloomWidth;
            private float m_BloomRadius;
            private float m_BloomIntensity;
            private float m_BloomBlend;
            private float m_BloomThreshold;
            private float m_BaseColorBoost;
            private float m_BaseColorBoostThreshold;

            public BloomPass(Material material)
            {
                m_Material = material;
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
                m_BloomWidth = Mathf.Max(1, bloomWidth);
                m_BloomRadius = Mathf.Clamp(bloomRadius, 0f, 10f);
                m_BloomIntensity = Mathf.Max(0f, bloomIntensity);
                m_BloomBlend = Mathf.Clamp01(bloomBlend);
                m_BloomThreshold = Mathf.Max(0f, bloomThreshold);
                m_BaseColorBoost = Mathf.Max(0f, baseColorBoost);
                m_BaseColorBoostThreshold = Mathf.Max(0f, baseColorBoostThreshold);
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
                int firstWidth = Mathf.Clamp(m_BloomWidth, 1, sourceDesc.width);
                int firstHeight = Mathf.Max(1, Mathf.RoundToInt(firstWidth * sourceDesc.height / (float)sourceDesc.width));
                CalculatePyramidParameters(
                    firstWidth,
                    firstHeight,
                    m_BloomRadius,
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
                    m_Down[i] = renderGraph.CreateTexture(bloomDesc);

                    if (i < levelCount - 1)
                    {
                        bloomDesc.name = $"BeatSaberBloom Up {i}";
                        m_Up[i] = renderGraph.CreateTexture(bloomDesc);
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

                using (IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass<PassData>("Beat Saber Bloom", out PassData passData))
                {
                    passData.material = m_Material;
                    passData.source = source;
                    passData.depth = depth;
                    passData.composite = composite;
                    passData.down = m_Down;
                    passData.up = m_Up;
                    passData.levelCount = levelCount;
                    passData.bloomBlend = m_BloomBlend;
                    passData.alphaWeight = m_BloomThreshold;
                    passData.pyramidIntensity = m_BloomIntensity;
                    passData.sampleScale = sampleScale;
                    passData.baseColorBoost = m_BaseColorBoost;
                    passData.baseColorBoostThreshold = m_BaseColorBoostThreshold;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(depth, AccessFlags.Read);
                    builder.UseTexture(composite, AccessFlags.Write);
                    for (int i = 0; i < levelCount; i++)
                    {
                        builder.UseTexture(m_Down[i], AccessFlags.ReadWrite);
                        if (i < levelCount - 1)
                        {
                            builder.UseTexture(m_Up[i], AccessFlags.ReadWrite);
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

                commandBuffer.SetGlobalFloat(BloomThresholdId, data.alphaWeight);
                commandBuffer.SetGlobalTexture(CameraDepthTextureId, data.depth);
                Blitter.BlitCameraTexture(commandBuffer, data.source, data.down[0], loadAction, storeAction, data.material, 0);

                for (int i = 1; i < data.levelCount; i++)
                {
                    Blitter.BlitCameraTexture(commandBuffer, data.down[i - 1], data.down[i], loadAction, storeAction, data.material, 1);
                }

                TextureHandle bloom = data.down[data.levelCount - 1];
                commandBuffer.SetGlobalFloat(SampleScaleId, data.sampleScale);
                for (int i = data.levelCount - 2; i >= 0; i--)
                {
                    commandBuffer.SetGlobalTexture(BloomLowTextureId, bloom);
                    float highWeight = Mathf.Min(
                        1f,
                        Mathf.Pow(data.pyramidIntensity * (i + 1f) / (data.levelCount - 1f), 0.01f));
                    float lowWeight = Mathf.Min(1f, 2f - highWeight);
                    commandBuffer.SetGlobalVector(BloomCombineId, new Vector4(highWeight, lowWeight, 0f, 0f));
                    Blitter.BlitCameraTexture(commandBuffer, data.down[i], data.up[i], loadAction, storeAction, data.material, 2);
                    bloom = data.up[i];
                }

                commandBuffer.SetGlobalTexture(BloomLowTextureId, bloom);
                commandBuffer.SetGlobalFloat(BloomIntensityId, data.bloomBlend);
                commandBuffer.SetGlobalFloat(BaseColorBoostId, data.baseColorBoost);
                commandBuffer.SetGlobalFloat(BaseColorBoostThresholdId, data.baseColorBoostThreshold);
                Blitter.BlitCameraTexture(commandBuffer, data.source, data.composite, loadAction, storeAction, data.material, 3);
            }

            private sealed class PassData
            {
                public Material material;
                public TextureHandle source;
                public TextureHandle depth;
                public TextureHandle composite;
                public TextureHandle[] down;
                public TextureHandle[] up;
                public int levelCount;
                public float bloomBlend;
                public float alphaWeight;
                public float pyramidIntensity;
                public float sampleScale;
                public float baseColorBoost;
                public float baseColorBoostThreshold;
            }
        }
    }
}
