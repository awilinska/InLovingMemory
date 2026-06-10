using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class EdgeDetectionOutlineFeature : ScriptableRendererFeature
{
    [Serializable]
    public sealed class OutlineSettings
    {
        public Shader shader;

        [Range(0f, 15f)]
        public float thickness = 3f;

        [Range(0f, 1f)]
        public float depthMinThreshold = 0.005f;

        [Range(0f, 1f)]
        public float depthMaxThreshold = 0.01f;

        [Range(0f, 1f)]
        public float normalMinThreshold = 0.2f;

        [Range(0f, 1f)]
        public float normalMaxThreshold = 0.4f;

        [Range(0f, 1f)]
        public float luminanceMinThreshold = 0.1f;

        [Range(0f, 1f)]
        public float luminanceMaxThreshold = 0.2f;

        public Color outlineColor = Color.black;
    }

    [SerializeField]
    private OutlineSettings settings = new();

    private OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass?.Dispose();
        outlinePass = new OutlinePass(settings);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (outlinePass == null
            || settings.shader == null
            || renderingData.cameraData.cameraType == CameraType.Preview
            || renderingData.cameraData.cameraType == CameraType.Reflection
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;

        renderer.EnqueuePass(outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        outlinePass?.Dispose();
        outlinePass = null;
    }

    private sealed class OutlinePass : ScriptableRenderPass, IDisposable
    {
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int DepthMinThresholdId = Shader.PropertyToID("_DepthMinThreshold");
        private static readonly int DepthMaxThresholdId = Shader.PropertyToID("_DepthMaxThreshold");
        private static readonly int NormalMinThresholdId = Shader.PropertyToID("_NormalMinThreshold");
        private static readonly int NormalMaxThresholdId = Shader.PropertyToID("_NormalMaxThreshold");
        private static readonly int LuminanceMinThresholdId = Shader.PropertyToID("_LuminanceMinThreshold");
        private static readonly int LuminanceMaxThresholdId = Shader.PropertyToID("_LuminanceMaxThreshold");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        private readonly OutlineSettings settings;
        private Material material;

        public OutlinePass(OutlineSettings settings)
        {
            this.settings = settings;
            ConfigureInput(
                ScriptableRenderPassInput.Depth
                | ScriptableRenderPassInput.Normal
                | ScriptableRenderPassInput.Color);
            requiresIntermediateTexture = true;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            if (settings.shader == null)
                return;

            if (material == null || material.shader != settings.shader)
            {
                CoreUtils.Destroy(material);
                material = CoreUtils.CreateEngineMaterial(settings.shader);
            }

            UpdateMaterialProperties();

            var resourceData = frameData.Get<UniversalResourceData>();
            using var builder =
                renderGraph.AddRasterRenderPass<PassData>("Edge Detection Outline", out _);

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(
                (PassData _, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, Vector2.one, material, 0));
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        private void UpdateMaterialProperties()
        {
            material.SetFloat(ThicknessId, settings.thickness);
            material.SetFloat(DepthMinThresholdId, settings.depthMinThreshold);
            material.SetFloat(DepthMaxThresholdId, settings.depthMaxThreshold);
            material.SetFloat(NormalMinThresholdId, settings.normalMinThreshold);
            material.SetFloat(NormalMaxThresholdId, settings.normalMaxThreshold);
            material.SetFloat(LuminanceMinThresholdId, settings.luminanceMinThreshold);
            material.SetFloat(LuminanceMaxThresholdId, settings.luminanceMaxThreshold);
            material.SetColor(OutlineColorId, settings.outlineColor);
        }

        private sealed class PassData
        {
        }
    }
}
