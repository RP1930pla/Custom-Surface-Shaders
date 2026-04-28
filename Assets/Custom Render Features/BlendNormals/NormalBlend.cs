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

        private class RenderDepthData
        {
            public RendererListHandle rendererListHandle;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecuteRenderNormalPass(RenderNormalData data, RasterGraphContext context)
        {
            Debug.Log("HERE");
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        static void ExecuteRenderDepthPass(RenderDepthData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passRenderName = "Render Depth/Normal";
            const string normalShaderTag = "SmoothNormalsPass";
            const string depthShaderTag = "";

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle normalRenderTexture;
            TextureHandle depthRenderTexture;

            using (var builder = renderGraph.AddRasterRenderPass<RenderNormalData>(passRenderName, out var passData)) 
            {
                RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;
                TextureDesc textureDesc = new TextureDesc(cameraDesc.width, cameraDesc.height)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SNorm,
                    depthBufferBits = 0,
                    useMipMap = false,
                    msaaSamples = MSAASamples.None,
                    name = "NormalBlur",
                    clearBuffer = true,
                    clearColor = Color.clear
                };

                TextureDesc textureDescDepth = new TextureDesc(cameraDesc.width, cameraDesc.height)
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
                TextureHandle depthTex = renderGraph.CreateTexture(textureDescDepth);

                builder.SetRenderAttachment(normalRenderTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthTex, AccessFlags.Write);

                SortingSettings sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.RenderQueue
                };

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("SmoothNormalsPass"), sortingSettings);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHandle);

                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderNormalData data, RasterGraphContext context) => ExecuteRenderNormalPass(data, context));

            }


        }
    }
}
