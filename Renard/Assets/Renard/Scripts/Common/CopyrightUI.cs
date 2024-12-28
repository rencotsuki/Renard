using UnityEngine;
using TMPro;
using Renard;

public class CopyrightUI : MonoBehaviourCustom
{
    [SerializeField] private TextMeshProUGUI _textUI = default;
    [SerializeField] private bool _firstYear = false;
    [SerializeField] private bool _nowYear = false;

    private void OnEnable()
    {
        if (_nowYear)
        {
            _textUI.text = $"{ApplicationCopyright.Copyright3}";
        }
        else if (_firstYear)
        {
            _textUI.text = $"{ApplicationCopyright.Copyright2}";
        }
        else
        {
            _textUI.text = $"{ApplicationCopyright.Copyright1}";
        }
    }
}

#if UNITY_EDITOR

[UnityEditor.CustomEditor(typeof(CopyrightUI))]
public class CopyrightUIEditor : UnityEditor.Editor
{
    private UnityEditor.SerializedProperty firstYearProperty = null;
    private UnityEditor.SerializedProperty nowYearProperty = null;

    private void OnEnable()
    {
        firstYearProperty = serializedObject.FindProperty("_firstYear");
        nowYearProperty = serializedObject.FindProperty("_nowYear");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (firstYearProperty != null && nowYearProperty != null)
        {
            if (!firstYearProperty.boolValue)
                nowYearProperty.boolValue = false;
        }

        serializedObject.ApplyModifiedProperties();

        base.OnInspectorGUI();
    }
}

#endif
