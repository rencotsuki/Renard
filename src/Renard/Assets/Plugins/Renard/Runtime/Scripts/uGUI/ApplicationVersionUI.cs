using UnityEngine;
using TMPro;
using Renard;

public class ApplicationVersionUI : MonoBehaviourCustom
{
    [SerializeField] private bool _viewCommitHash = false;
    [SerializeField] private TextMeshProUGUI _textUI = default;

    private void OnEnable()
    {
        var commitHash = string.Empty;
        if (_viewCommitHash)
        {
            var appVersion = ApplicationVersionAsset.Load();
            commitHash = appVersion != null ? " " + appVersion.CommitHashShort : string.Empty;

            if (string.IsNullOrEmpty(commitHash))
                commitHash = " ---";
        }

        _textUI.text = $"ver {ApplicationVersion.Version}({ApplicationVersion.BuildVersion}){commitHash}";
    }
}
