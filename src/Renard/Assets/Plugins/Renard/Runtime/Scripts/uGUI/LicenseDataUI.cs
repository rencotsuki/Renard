using UnityEngine;
using TMPro;
using Renard;

public class LicenseDataUI : MonoBehaviourCustom
{
    [SerializeField] protected TMP_Text textContentsId = default;
    [SerializeField] protected TMP_Text textCreateDate = default;
    [SerializeField] protected TMP_Text textValidityDays = default;
    [SerializeField] protected TMP_Text textExpiryDate = default;

    protected string licensePassKey => LicenseHandler.LicensePassKey;
    protected string createDate => LicenseHandler.CreateDate;
    protected int validityDays => LicenseHandler.ValidityDays;
    protected string expiryDate => LicenseHandler.ExpiryDate;

    private void OnEnable()
    {
        OnEnableAction();
    }

    protected virtual void OnEnableAction()
        => SetView();

    protected virtual void SetView()
    {
        if (textContentsId != null)
            textContentsId.text = $"licensePassKey: {(string.IsNullOrEmpty(licensePassKey) ? "PassKey empty." : licensePassKey)}";

        if (textCreateDate != null)
            textCreateDate.text = $"createDate: {createDate}";

        if (textValidityDays != null)
            textValidityDays.text = $"validityDays: {validityDays}";

        if (textExpiryDate != null)
            textExpiryDate.text = $"expiryDate: {expiryDate}";
    }
}
