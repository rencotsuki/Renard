using System;
using UnityEngine;
using UnityEngine.UI;

namespace Renard
{
    [Serializable]
    public class SampleScene : MonoBehaviourCustom
    {
        [SerializeField] private ExternalDisplayHandler externalDisplay = null;
        [SerializeField] private Camera tragetCamera = null;
        [SerializeField] private RawImage outputView = null;

        private void Start()
        {
            if (externalDisplay != null)
            {
                externalDisplay.Setup(tragetCamera, new Vector2(1920f, 1080f), 0f);

                if (outputView != null)
                {
                    outputView.texture = externalDisplay.OutputTexture;
                    outputView.enabled = (outputView.texture != null);
                }
            }
        }
    }
}
