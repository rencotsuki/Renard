using UnityEngine;
using UnityEditor;
using System.IO;

namespace Renard.QRCode
{
    public class QRCodeHelperEditor : EditorWindow
    {
        private QRImageSize _size = QRImageSize.SIZE_256;
        private string _content = "";

        [UnityEditor.MenuItem("Renard/License/CreateQRCode", false, 3)]
        private static void Init()
        {
            var window = EditorWindow.GetWindow<QRCodeHelperEditor>();
            window.Show();
        }

        private void OnGUI()
        {
            var savePath = $"{Application.dataPath}/../../{QRCodeHelper.Path}/{QRCodeHelper.FileName}.{QRCodeHelper.FileExtension}";

            _content = GUILayout.TextArea(_content, GUILayout.Height(30f));
            _size = (QRImageSize)EditorGUILayout.EnumPopup(_size);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_content));

            if (GUILayout.Button("Create"))
            {
                var size = (int)Mathf.Pow(2f, (int)_size);
                var tex = QRCodeHelper.CreateQRCode(_content, size, size);

                using (var fs = new FileStream(savePath, FileMode.OpenOrCreate))
                {
                    var data = tex.EncodeToPNG();
                    fs.Write(data, 0, data.Length);
                }

                AssetDatabase.Refresh();
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
