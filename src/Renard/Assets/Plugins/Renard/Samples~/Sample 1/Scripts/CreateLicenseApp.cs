using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Renard.Sample
{
    [Serializable]
    public class CreateLicenseApp : MonoBehaviourCustom
    {
        [SerializeField] private LicenseHandler handler = default;
        [SerializeField] private Button buttonCreate = default;
        [SerializeField] private TMP_InputField inputFieldUuid = default;
        [SerializeField] private Button buttonDefaultContentsId = default;
        [SerializeField] private TMP_InputField inputFieldContentsId = default;
        [SerializeField] private TextMeshProUGUI textValidityDays = default;
        [SerializeField] private TextMeshProUGUI textCreateDate = default;
        [SerializeField] private TextMeshProUGUI textExpiryDate = default;
        [SerializeField] private TMP_Dropdown dropdownValidityDays = default;

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
                    editData.ExpiryDate > DateTime.UtcNow)
                {
                    return true;
                }
                return false;
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            // 動作確認用
            handler.IsDebugLog = true;
#endif

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
        }

        private void ResetEditData()
        {
            editData.Uuid = string.Empty;
            inputFieldUuid.text = editData.Uuid;

            editData.CreateDate = DateTime.UtcNow;
            editData.ValidityDays = LicenseHandler.ValidityDaysList[0];

            SetDefaultContentsId();
        }

        private void CreateLicense()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX

            editData.CreateDate = DateTime.UtcNow;

            if (handler != null && handler.Create(editData))
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
            editData.ContentsId = handler != null ? handler.m_ContentsId : string.Empty;
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
    }
}
