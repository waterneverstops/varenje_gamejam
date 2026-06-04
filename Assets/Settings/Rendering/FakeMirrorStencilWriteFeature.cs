using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class FakeMirrorStencilWriteFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Settings
    {
        public string passTag = "Fake Mirror Layer Render";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public RenderQueueType renderQueueType = RenderQueueType.Opaque;
        public LayerMask layerMask = 0;
        public int stencilRef = 64;
        public CompareFunction depthTest = CompareFunction.LessEqual;
        public bool depthWrite = true;
        public bool clearDepthInsideStencil = true;
    }

    [SerializeField] private Settings settings = new Settings();
    [SerializeField] private Shader depthClearShader;

    private StencilDepthClearPass depthClearPass;
    private RenderObjectsPass pass;
    private Material depthClearMaterial;

    private static readonly int StencilRefId = Shader.PropertyToID("_StencilRef");

    public override void Create()
    {
        if (settings.renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
            settings.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

        Shader shader = depthClearShader != null
            ? depthClearShader
            : Shader.Find("Hidden/Fake Mirror/Stencil Depth Clear");
        if (shader != null)
        {
            if (depthClearMaterial == null || depthClearMaterial.shader != shader)
                depthClearMaterial = CoreUtils.CreateEngineMaterial(shader);

            depthClearPass = new StencilDepthClearPass(depthClearMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }
        else
        {
            depthClearPass = null;
        }

        pass = new RenderObjectsPass(
            settings.passTag,
            settings.renderPassEvent,
            null,
            settings.renderQueueType,
            settings.layerMask,
            new RenderObjects.CustomCameraSettings());

        pass.overrideMaterial = null;
        pass.overrideShader = null;
        pass.SetDepthState(settings.depthWrite, settings.depthTest);
        pass.SetStencilState(settings.stencilRef, CompareFunction.Equal, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || settings.layerMask == 0)
            return;

        if (renderingData.cameraData.cameraType == CameraType.Preview
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;

        if (settings.clearDepthInsideStencil && depthClearPass != null && depthClearMaterial != null)
        {
            depthClearMaterial.SetFloat(StencilRefId, settings.stencilRef);
            renderer.EnqueuePass(depthClearPass);
        }

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(depthClearMaterial);
        depthClearMaterial = null;
        depthClearPass = null;
        pass = null;
    }

    private sealed class StencilDepthClearPass : ScriptableRenderPass
    {
        private readonly Material material;

        public StencilDepthClearPass(Material material)
        {
            this.material = material;
            profilingSampler = new ProfilingSampler("Fake Mirror Depth Clear");
        }

        private sealed class PassData
        {
            public TextureHandle depthTarget;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                passData.depthTarget = resourceData.activeDepthTexture;
                passData.material = material;

                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Execute(context.cmd, data.depthTarget, data.material);
                });
            }
        }

        private static void Execute(RasterCommandBuffer cmd, RTHandle depthTarget, Material material)
        {
            Vector2Int scaledViewportSize = depthTarget.GetScaledSize(depthTarget.rtHandleProperties.currentViewportSize);
            cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1);
        }
    }
}
