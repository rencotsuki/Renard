using System;
using UnityEngine;

namespace Renard
{
    [Serializable]
    [RequireComponent(typeof(Camera))]
    public class ExternalDisplayHandler : MonoBehaviourCustom
    {
        [SerializeField] protected Material materialExternalDisplay = null;

        protected Camera targetCamera { get; private set; } = null;

        protected RenderTexture targetSource = null;
        protected RenderTexture outputSource = null;
        protected Material material = null;

        public Texture OutputTexture => outputSource;

        public Vector2 DisplaySize { get; private set; } = new Vector2(1920f, 1080f);
        public float DisplayRoll { get; private set; } = 0f;

        private void Awake()
        {
            if (materialExternalDisplay != null)
            {
                material = new Material(materialExternalDisplay);
                material.CopyPropertiesFromMaterial(materialExternalDisplay);
            }

            OnAwake();
        }

        protected virtual void OnAwake() { }

        public void Setup(Camera target) => Setup(target, DisplaySize, DisplayRoll);
        public void Setup(Camera target, Vector2 displaySize) => Setup(target, displaySize, DisplayRoll);
        public void Setup(Camera target, float displayRoll) => Setup(target, DisplaySize, displayRoll);
        public void Setup(Camera target, Vector2 displaySize, float displayRoll)
        {
            targetCamera = target;
            DisplaySize = displaySize;
            DisplayRoll = displayRoll;

            CreateRenderTexture(DisplaySize, DisplayRoll);
        }

        public void SetDisplaySize(Vector2 size) => SetDisplaySize(size, DisplayRoll);
        public void SetDisplaySize(Vector2 size, float roll)
        {
            DisplaySize = size;
            DisplayRoll = roll == 0 ? 0f : Mathf.Clamp(roll, -180f, 180f);
            CreateRenderTexture(DisplaySize, DisplayRoll);
        }

        private void CreateRenderTexture(Vector2 size, float roll)
        {
            if (targetCamera == null)
                return;

            var tmpRoll = Mathf.Abs(roll);
            if (tmpRoll <= 45f && tmpRoll >= 105f)
            {
                targetSource = new RenderTexture((int)size.y, (int)size.x, 0, RenderTextureFormat.ARGB32);
            }
            else
            {
                targetSource = new RenderTexture((int)size.x, (int)size.y, 0, RenderTextureFormat.ARGB32);
            }

            material?.SetFloat("_Rotation", roll);

            targetSource.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            targetSource.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;
            targetSource.autoGenerateMips = true;
            targetSource.Create();

            outputSource = new RenderTexture((int)size.x, (int)size.y, 0, RenderTextureFormat.ARGB32);
            outputSource.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            outputSource.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;
            outputSource.autoGenerateMips = true;
            outputSource.Create();

            targetCamera.targetTexture = targetSource;

            OnCreateRenderTexture();

#if UNITY_IOS
            OnSetup();
#endif
        }

        protected virtual void OnCreateRenderTexture() { }

#if UNITY_IOS

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void SetupExternalDisplay(IntPtr texturePtr);

        private void OnSetup()
        {
            try
            {
                if (outputSource == null)
                    throw new Exception("texture is not assigned!");

                // UnityのTextureをネイティブポインタに変換して渡す
                IntPtr texturePtr = outputSource.GetNativeTexturePtr();

                SetupExternalDisplay(texturePtr);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Info, "OnSetup", $"{ex.Message}");
            }
        }

#endif

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (targetSource != null)
            {
                if (outputSource != null)
                {
                    if (material != null)
                    {
                        Graphics.Blit(targetSource, outputSource, material);
                    }
                    else
                    {
                        Graphics.Blit(targetSource, outputSource);
                    }

                    Graphics.Blit(outputSource, dest);
                }
                else
                {
                    Graphics.Blit(targetSource, dest);
                }
            }
            else
            {
                if (outputSource != null)
                {
                    if (material != null)
                    {
                        Graphics.Blit(src, outputSource, material);
                    }
                    else
                    {
                        Graphics.Blit(src, outputSource);
                    }

                    Graphics.Blit(outputSource, dest);
                }
                else
                {
                    Graphics.Blit(src, dest);
                }
            }
        }
    }
}
