using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RenderBrushstrokes : ScriptableRendererFeature
{
    [SerializeField] RenderBrushstrokesSettings settings;
    RenderBrushstrokesPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new RenderBrushstrokesPass(settings);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (settings.material == null)
        {
            Debug.LogWarning("Highlight Pass needs a blit material");
            return;
        }

        renderer.EnqueuePass(m_ScriptablePass);
    }

    [Serializable]
    public class RenderBrushstrokesSettings
    {
        public string shaderTagID;
        public Material material;
    }

    class RenderBrushstrokesPass : ScriptableRenderPass
    {
        readonly RenderBrushstrokesSettings settings;

        public RenderBrushstrokesPass(RenderBrushstrokesSettings settings)
        {
            this.settings = settings;
        }


        private class BrushPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class BlitPassData 
        {
            public TextureHandle sourceTexture;
            public TextureHandle bufferTexture;
            public Material material;

        }

        static void ExecuteRenderPass(BrushPassData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, new Color(0.5f,0.5f,0.5f, 1));
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        static void ExecuteBlitPass(BlitPassData data, RasterGraphContext context) 
        {
            data.material.SetTexture(Shader.PropertyToID("_PaintBuffer"), data.bufferTexture);
            Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
        }


        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle brushStrokesRenderTexture;
            TextureHandle brushStrokesRenderTextureDepth;

            const string passBrush = "Render Brushstrokes";
            using (var builder = renderGraph.AddRasterRenderPass<BrushPassData>(passBrush, out var passData))
            {
                RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;
                TextureDesc textureDesc = new TextureDesc(cameraDesc.width, cameraDesc.height)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    depthBufferBits = 0,
                    useMipMap = false,
                    msaaSamples = MSAASamples.None,
                    name = "_BrushStrokeBuffer",
                    clearBuffer = true,
                    clearColor = Color.clear
                };

                TextureDesc textureDescDepth = new TextureDesc(cameraDesc.width, cameraDesc.height)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat,
                    depthBufferBits = DepthBits.Depth32,
                    useMipMap = false,
                    msaaSamples = MSAASamples.None,
                    name = "_BrushStrokeBuffer",
                    clearBuffer = true,
                    clearColor = Color.clear
                };

                brushStrokesRenderTexture = renderGraph.CreateTexture(textureDesc);
                brushStrokesRenderTextureDepth = renderGraph.CreateTexture(textureDescDepth);

                builder.SetRenderAttachment(brushStrokesRenderTexture, 0, AccessFlags.Write);

                SortingSettings sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.RenderQueue
                };

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId(settings.shaderTagID), sortingSettings);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachmentDepth(brushStrokesRenderTextureDepth, AccessFlags.Write);

                builder.SetRenderFunc((BrushPassData data, RasterGraphContext context) => ExecuteRenderPass(data, context));
            }

            const string passBlit = "Paint Distort";
            using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>(passBlit, out var passData)) 
            {
                if (!brushStrokesRenderTexture.IsValid()) return;

                passData.sourceTexture = resourceData.activeColorTexture;
                passData.bufferTexture = brushStrokesRenderTexture;
                passData.material = settings.material;

                builder.UseTexture(passData.bufferTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                //passData.material.SetTexture(Shader.PropertyToID("_PaintBuffer"), passData.bufferTexture);
                builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) => ExecuteBlitPass(data, context));

            }
        }
    }
}
