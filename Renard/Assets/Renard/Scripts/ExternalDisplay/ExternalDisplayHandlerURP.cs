using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Renard
{
    [RequireComponent(typeof(Camera))]
    public class ExternalDisplayHandlerURP : ExternalDisplayHandler
    {
        [SerializeField] private UniversalRendererData universalRenderer = null;

        private ExternalDisplayRenderFeature _renderFeature = null;

        protected override void OnAwake()
        {
            if (universalRenderer == null)
                return;

            try
            {
                var rendererFeatures = universalRenderer.rendererFeatures;

                foreach (var feature in rendererFeatures)
                {
                    if (feature is ExternalDisplayRenderFeature myRenderFeature)
                    {
                        _renderFeature = myRenderFeature;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnAwake", $"{ex.Message}");
            }
        }

        protected override void OnCreateRenderTexture()
        {
            _renderFeature?.Setup(targetSource, outputSource, material);
        }
    }
}
