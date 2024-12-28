using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Renard
{
    public class ExternalDisplayRenderFeature : ScriptableRendererFeature
    {
        protected class CustomRenderPass : ScriptableRenderPass
        {
            protected RenderTargetIdentifier source { get; private set; } = default;
            protected RenderTexture targetTexture { get; private set; } = null;
            protected RenderTexture outputTexture { get; private set; } = null;
            protected Material material { get; private set; } = null;

            [HideInInspector] private CommandBuffer cmd = null;

            public CustomRenderPass(RenderTexture target, RenderTexture output, Material mat)
            {
                targetTexture = target;
                outputTexture = output;
                material = mat;
            }

            public void Setup(RenderTargetIdentifier targetSource)
            {
                source = targetSource;
            }

#if UNITY_6000_0_OR_NEWER
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                this.cmd = cmd;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (cmd == null) return;

                try
                {
                    if (material != null)
                    {
                        cmd.Blit(targetTexture, outputTexture, material);
                    }
                    else
                    {
                        cmd.Blit(targetTexture, outputTexture);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"{this.GetType().Name}::RecordRenderGraph - {ex.Message}");
                }
            }
#else
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                try
                {
                    cmd = CommandBufferPool.Get("CustomRenderPass");

                    if (material != null)
                    {
                        cmd.Blit(targetTexture, outputTexture, material);
                    }
                    else
                    {
                        cmd.Blit(targetTexture, outputTexture);
                    }

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
                catch (Exception ex)
                {
                    Debug.Log($"{this.GetType().Name}::Execute - {ex.Message}");
                }
            }
#endif
        }

        protected CustomRenderPass renderPass { get; private set; } = null;
        protected RenderTexture targetRenderTexture { get; private set; } = null;
        protected RenderTexture outputRenderTexture { get; private set; } = null;

        [HideInInspector] private Material material = null;

        public void Setup(RenderTexture target, RenderTexture output, Material mat = null)
        {
            targetRenderTexture = target;
            outputRenderTexture = output;
            material = mat;
            Create();
        }

        public override void Create()
        {
            renderPass = new CustomRenderPass(targetRenderTexture, outputRenderTexture, material);
            renderPass.renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (targetRenderTexture == null)
                return;

            renderer.EnqueuePass(renderPass);
        }
    }
}
