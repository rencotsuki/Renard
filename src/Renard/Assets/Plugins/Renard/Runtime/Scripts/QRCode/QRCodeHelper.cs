using System;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

namespace Renard.QRCode
{
    using Debuger;

    public enum QRImageSize : int
    {
        SIZE_128 = 7,
        SIZE_256,
        SIZE_512,
        SIZE_1024,
        SIZE_2048,
        SIZE_4096
    }

    public class QRCodeHelper
    {
        public const string Path = "Assets/Resources";
        public const string FileName = "LicenseQR";
        public const string FileExtension = "png";

        private static bool _isDebugLog = false;

        private static void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!_isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(QRCodeHelper), logType, methodName, message);
        }

        public static string Read(Texture2D tex, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            if (tex != null)
                return OnRead(tex.width, tex.height, tex.GetPixels32());
            return string.Empty;
        }

        public static string Read(WebCamTexture tex, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            if (tex != null)
                return OnRead(tex.width, tex.height, tex.GetPixels32());
            return string.Empty;
        }

        private static string OnRead(int width, int height, Color32[] pixel32s)
        {
            try
            {
                BarcodeReader reader = new BarcodeReader();
                var result = reader?.Decode(pixel32s, width, height);
                return result.Text;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnRead", $"{ex.Message}");
                return string.Empty;
            }
        }

        public static Texture2D CreateQRCode(string data, int width, int height, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            if (!string.IsNullOrEmpty(data))
                return OnWrite(data, width, height);
            return null;
        }

        private static Texture2D OnWrite(string data, int width, int height)
        {
            try
            {
                if (width <= 0 || height <= 0)
                    throw new Exception($"size error. width={width}, height={height}");

                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions() { Width = width, Height = height }
                };

                var content = writer.Write(data);

                var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
                tex.SetPixels32(content);
                tex.Apply();

                return tex;
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "OnWrite", $"{ex.Message}");
                return null;
            }
        }
    }
}
