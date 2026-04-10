using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RenderHighlight : ScriptableRendererFeature
{
    [SerializeField] RenderHighlightSettings settings;
    RenderHighlightPass m_RenderHighlightPass;

    [Serializable]
    public class RenderHighlightSettings
    {
        public Color enemyColor;
        public Color importantColor;
        public Material material;
        public RenderPassEvent renderPassEvent;
        public string shaderTagID;

        public bool isAdditive = false;

    }
    public override void Create()
    {
        m_RenderHighlightPass = new RenderHighlightPass(settings);
        m_RenderHighlightPass.renderPassEvent = settings.renderPassEvent;

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            Debug.LogWarning("Highlight Pass needs a blit material");
            return;
        }
        renderer.EnqueuePass(m_RenderHighlightPass);
    }



    class RenderHighlightPass : ScriptableRenderPass
    {
        readonly RenderHighlightSettings settings;

        public RenderHighlightPass(RenderHighlightSettings settings)
        {
            this.settings = settings;
        }

     
        private class HighlightPassData
        {
            public RendererListHandle rendererListHandle;
        }

        private class BlitPassData 
        {
            public TextureHandle sourceTexture;
            public Material material;
            public bool isAdditive;
        }

        static void ExecuteHighlightPass(HighlightPassData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

        static void ExecuteBlitPass(BlitPassData data, RasterGraphContext context)
        {
            if (data.isAdditive)
            {
                Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1,1,0,0), data.material,1);
            }
            else 
            {
                Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle highlightRenderTexture;

            //Pass 
            const string passRenderName = "Render Highlight";
            using (var builder = renderGraph.AddRasterRenderPass<HighlightPassData>(passRenderName, out var passData))
            {
                RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;
                TextureDesc textureDesc = new TextureDesc(cameraDesc.width, cameraDesc.height)
                {
                    colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    depthBufferBits = 0,
                    useMipMap = false,
                    msaaSamples = MSAASamples.None,
                    name = "_HighlightBuffer",
                    clearBuffer = true,
                    clearColor = Color.clear
                };

                highlightRenderTexture = renderGraph.CreateTexture(textureDesc);
                builder.SetRenderAttachment(highlightRenderTexture, 0, AccessFlags.Write);

                SortingSettings sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.CommonOpaque
                };

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId(settings.shaderTagID),sortingSettings);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                passData.rendererListHandle = renderGraph.CreateRendererList(listParams);
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderFunc((HighlightPassData data, RasterGraphContext context) => ExecuteHighlightPass(data, context));
            }

            const string passBlitName = "Blit Highlight";
            using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>(passBlitName, out var passData))
            {
                if (!highlightRenderTexture.IsValid()) return;

                passData.sourceTexture = highlightRenderTexture;
                passData.material = settings.material;

                passData.material.SetColor("", settings.importantColor);
                passData.material.SetColor("", settings.enemyColor);
                passData.isAdditive = settings.isAdditive;

                builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) => ExecuteBlitPass(data, context));
            }
        }
    }
}
