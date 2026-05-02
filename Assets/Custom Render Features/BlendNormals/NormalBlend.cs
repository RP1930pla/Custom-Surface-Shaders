using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class NormalBlend : ScriptableRendererFeature
{
    [SerializeField] NormalBlendSettings settings;
    NormalBlendPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new NormalBlendPass(settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        //m_ScriptablePass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);

        //m_ScriptablePass.requiresIntermediateTexture = true;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    // Use this class to pass around settings from the feature to the pass
    [Serializable]
    public class NormalBlendSettings
    {
        public LayerMask layer;
        public bool debug = false;
        public Material debugMaterial;
        public Material blitMaterial;
    }

    class NormalBlendPass : ScriptableRenderPass
    {
        readonly NormalBlendSettings settings;

        public NormalBlendPass(NormalBlendSettings settings)
        {
            this.settings = settings;
        }

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class RenderNormalData
        {
            public RendererListHandle rendererListHandle;
        }

        private class StochasticNormalData 
        {
            public TextureHandle texture;
            public Material blitMaterial;
        }

        private class RenderDepthData
        {
            public RendererListHandle rendererListHandle;
        }

        private class RenderObjectsData 
        {
            public RendererListHandle rendererListHandle;
            public TextureHandle normalTex;
            public TextureHandle depthTex;
        }

        private class DebugData 
        {
            public TextureHandle sourceTexture;
            public Material material;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecuteRenderNormalPass(RenderNormalData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        static void ExecuteRenderDepthPass(RenderDepthData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        static void ExecuteBlendedObjectsPass(RenderObjectsData data, RasterGraphContext context)
        {
            //context.cmd.ClearRenderTarget(true, true, Color.clear);
            Shader.SetGlobalTexture("_NormalBlended", data.normalTex);
            Shader.SetGlobalTexture("_DepthBlended", data.depthTex);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        static void ExecuteStochasticSamplingPass(StochasticNormalData data, RasterGraphContext context) 
        {
            Blitter.BlitTexture(context.cmd, data.texture, new Vector4(0.5f, 0.5f, 0, 0), data.blitMaterial, 0);
            Blitter.BlitTexture(context.cmd, data.texture, new Vector4(0.5f, 0.5f, 0, 0), data.blitMaterial, 1);
            Blitter.BlitTexture(context.cmd, data.texture, new Vector4(0.5f, 0.5f, 0, 0), data.blitMaterial, 1);

        }

        static void ExecuteDebugPass(DebugData data, RasterGraphContext context) 
        {
            Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(0.5f, 0.5f, 0, 0), data.material, 0);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passNameRender = "Render Depth/Normal";
            const string passNameStochastic = "Stochastic Sampling";
            const string passNameDebug = "Debug Blit Pass";
            const string normalShaderTag = "SmoothNormalsPass";
            const string depthShaderTag = "SmoothNormalsDepthPass";
            const string blendedObjectsShaderTag = "blendedObjectsPass";

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle normalRenderTexture;
            TextureHandle depthRenderTexture;

            using (var builder = renderGraph.AddRasterRenderPass<RenderNormalData>(passNameRender, out var passData)) 
            {
                RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;
                TextureDesc textureDesc = new TextureDesc(cameraDesc.width/2, cameraDesc.height/2)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SNorm,
                    depthBufferBits = 0,
                    useMipMap = false,
                    msaaSamples = MSAASamples.None,
                    name = "NormalBlur",
                    clearBuffer = true,
                    clearColor = Color.clear
                };

                TextureDesc textureDescDepth = new TextureDesc(cameraDesc.width/2, cameraDesc.height/2)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat,
                    depthBufferBits = DepthBits.Depth32,
                    useMipMap = false,
                    msaaSamples = MSAASamples.None,
                    name = "NormalDepth",
                    clearBuffer = true,
                    clearColor = Color.clear
                };

                normalRenderTexture = renderGraph.CreateTexture(textureDesc);
                depthRenderTexture = renderGraph.CreateTexture(textureDescDepth);

                builder.SetRenderAttachment(normalRenderTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthRenderTexture, AccessFlags.Write);

                SortingSettings sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.RenderQueue
                };

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId(normalShaderTag), sortingSettings);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHandle);

                builder.SetRenderFunc((RenderNormalData data, RasterGraphContext context) => ExecuteRenderNormalPass(data, context));

            }

            using (var builder = renderGraph.AddRasterRenderPass<RenderDepthData>("Render Main Depth", out var passData)) 
            {
                builder.SetRenderAttachmentDepth(depthRenderTexture, AccessFlags.Write);
                SortingSettings sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.RenderQueue
                };

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.layer);
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId(depthShaderTag), sortingSettings);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderFunc((RenderDepthData data, RasterGraphContext context) => ExecuteRenderDepthPass(data, context));

                //builder.AllowPassCulling(false);

            }

            using (var builder = renderGraph.AddRasterRenderPass<StochasticNormalData>(passNameStochastic, out var passData))
            {
                if (!normalRenderTexture.IsValid()) return;
                if (settings.blitMaterial == null) return;
                passData.texture = normalRenderTexture;

                builder.SetRenderAttachment(passData.texture, 0, AccessFlags.Write);

                passData.blitMaterial = settings.blitMaterial;
                builder.SetRenderFunc((StochasticNormalData data, RasterGraphContext context) => ExecuteStochasticSamplingPass(data, context));
            }

            using (var builder = renderGraph.AddRasterRenderPass<RenderObjectsData>("Render Blended Objects", out var passData))
            {
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                SortingSettings sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.RenderQueue
                };

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId(blendedObjectsShaderTag), sortingSettings);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHandle);
                builder.UseTexture(normalRenderTexture);
                builder.UseTexture(depthRenderTexture);
                passData.depthTex = depthRenderTexture;
                passData.normalTex = normalRenderTexture;
                builder.SetRenderFunc((RenderObjectsData data, RasterGraphContext context) => ExecuteBlendedObjectsPass(data, context));

                builder.AllowPassCulling(false);

            }


            if (settings.debug) 
            {
                using (var builder = renderGraph.AddRasterRenderPass<DebugData>(passNameDebug, out var passData)) 
                {
                    if (!normalRenderTexture.IsValid()) return;
                    if (settings.debugMaterial == null) return;
                    passData.sourceTexture = normalRenderTexture;
                    builder.UseTexture(passData.sourceTexture, AccessFlags.Read);

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    passData.material = settings.debugMaterial;
                    builder.SetRenderFunc((DebugData data, RasterGraphContext context) => ExecuteDebugPass(data, context));
                }
            }

        }
    }
}
