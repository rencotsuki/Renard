using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Renard.Sample
{
    using QRCode;

    [Serializable]
    public class CreateLicenseApp : MonoBehaviourCustom
    {
        [SerializeField] private Button buttonCreate = default;
        [SerializeField] private TMP_InputField inputFieldUuid = default;
        [SerializeField] private Button buttonDefaultContentsId = default;
        [SerializeField] private TMP_InputField inputFieldContentsId = default;
        [SerializeField] private TextMeshProUGUI textValidityDays = default;
        [SerializeField] private TextMeshProUGUI textCreateDate = default;
        [SerializeField] private TextMeshProUGUI textExpiryDate = default;
        [SerializeField] private TMP_Dropdown dropdownValidityDays = default;
        [SerializeField] private Vector2Int createQRCodeSize = new Vector2Int(512, 512);
        [SerializeField] private Button buttonQRCodeReader = default;
        [SerializeField] private RawImage imageCreateQRCode = default;

        private LicenseData editData = new LicenseData();
        private string uuid => editData.Uuid;
        private string contentsId => editData.ContentsId;
        private string validityDays => editData.ValidityDays <= 0 ? "---" : $"{editData.ValidityDays}Day";
        private string createDate => $"{editData.CreateDate:yyyy-MM-dd}";
        private string expiryDate => $"{editData.ExpiryDate:yyyy-MM-dd}";

        private bool activeCreate
        {
            get
            {
                if (!string.IsNullOrEmpty(editData.Uuid) &&
                    !string.IsNullOrEmpty(editData.ContentsId) &&
                    editData.ExpiryDate > LicenseHandler.GetNow())
                {
                    return true;
                }
                return false;
            }
        }

        private SampleQRCamera sampleQRCamera = new SampleQRCamera();

        private string strReadUuid = string.Empty;
        private WebCamTexture qrCameraTexture = null;
        private Texture2D createQRCodeTexture = null;

        private void Start()
        {
            buttonCreate.onClick.AddListener(CreateLicense);
            buttonDefaultContentsId.onClick.AddListener(SetDefaultContentsId);

            inputFieldUuid.onEndEdit.AddListener(ChangedUuid);
            inputFieldContentsId.onEndEdit.AddListener(ChangedContentsId);

            dropdownValidityDays.options = new List<TMP_Dropdown.OptionData>();

            for (int i = 0; i < LicenseHandler.ValidityDaysList.Length; i++)
            {
                dropdownValidityDays.options.Add(new TMP_Dropdown.OptionData() { text = $"{LicenseHandler.ValidityDaysList[i]}day" });
            }

            dropdownValidityDays.value = 0;
            dropdownValidityDays.onValueChanged.AddListener(ChangedValidityDays);

            buttonQRCodeReader.onClick.AddListener(OpenQRCodeReader);

            ResetEditData();
        }

        private void Update()
        {
            if (buttonCreate != null && buttonCreate.gameObject.activeSelf != activeCreate)
                buttonCreate.gameObject.SetActive(activeCreate);

            if (textValidityDays != null && textValidityDays.text != validityDays)
                textValidityDays.text = validityDays;

            if (textCreateDate != null && textCreateDate.text != createDate)
                textCreateDate.text = createDate;

            if (textExpiryDate != null && textExpiryDate.text != expiryDate)
                textExpiryDate.text = expiryDate;

            if (imageCreateQRCode != null && imageCreateQRCode.texture != createQRCodeTexture)
                imageCreateQRCode.texture = createQRCodeTexture;
        }

        private void ResetEditData()
        {
            editData.Uuid = string.Empty;
            inputFieldUuid.text = editData.Uuid;

            editData.CreateDate = LicenseHandler.GetNow();
            editData.ValidityDays = LicenseHandler.ValidityDaysList[0];

            createQRCodeTexture = null;

            sampleQRCamera?.Stop();

            SetDefaultContentsId();
        }

        private void CreateLicense()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

            editData.CreateDate = LicenseHandler.GetNow();

            createQRCodeTexture = null;

            if (LicenseHandler.Create(editData, out createQRCodeTexture, createQRCodeSize, IsDebugLog))
            {
                SystemConsoleHandler.SystemWindow
                    .SetMessage("ライセンス生成", "成功しました")
                    .OnActionDone(null, "ＯＫ")
                    .Show();
            }
            else
            {
                SystemConsoleHandler.SystemWindow
                    .SetMessage("ライセンス生成", "失敗しました")
                    .OnActionDone(null, "ＯＫ")
                    .Show();
            }

#else

            SystemConsoleHandler.SystemWindow
                .SetMessage("ライセンス生成", $"現在のプラットフォームでは処理できません\n\rWindowまたはMacにて処理をお願いします")
                .OnActionDone(null, "ＯＫ")
                .Show();

#endif
        }

        private void SetDefaultContentsId()
        {
            editData.ContentsId = LicenseHandler.ConfigContentsId;
            inputFieldContentsId.text = editData.ContentsId;
        }

        private void ChangedUuid(string value)
        {
            editData.Uuid = value;
        }

        private void ChangedContentsId(string value)
        {
            editData.ContentsId = value;
        }

        private void ChangedValidityDays(int index)
        {
            if (LicenseHandler.ValidityDaysList.Length > index)
            {
                editData.ValidityDays = LicenseHandler.ValidityDaysList[index];
                return;
            }

            editData.ValidityDays = LicenseHandler.ValidityDaysList[0];
            dropdownValidityDays.value = 0;
        }

        private void OpenQRCodeReader()
        {
            qrCameraTexture = sampleQRCamera?.Setup(createQRCodeSize.x, createQRCodeSize.y);
            sampleQRCamera?.Play();

            SystemConsoleHandler.LicenseWindow
                .SetMessage("QRコード読込み", "ライセンスＱＲコードを\n\rカメラで読み取ります", qrCameraTexture, true)
                .OnActionMain(() =>
                {
                    strReadUuid = QRCodeHelper.Read(qrCameraTexture);
                    if (!string.IsNullOrEmpty(strReadUuid))
                    {
                        editData.Uuid = strReadUuid;
                        SystemConsoleHandler.LicenseWindow.Close();
                    }
                },
                "読み取り", false)
                .OnActionSub(() =>
                {
                    sampleQRCamera?.Stop();
                },
                "閉じる")
                .Show();
        }
    }
}
