using UnityEngine;
using TMPro;
using Renard;

public class ApplicationVersionUI : MonoBehaviourCustom
{
    [SerializeField] private TextMeshProUGUI _textUI = default;

    private void OnEnable()
    {
        _textUI.text = $"ver {ApplicationVersion.Version}({ApplicationVersion.BuildVersion})";
    }
}
