using System.Text;
using UnityEngine;
using TMPro;
using Renard;

public class DeviceUuidUI : MonoBehaviourCustom
{
    [SerializeField] private TextMeshProUGUI _textUI = default;

    private bool debugView
    {
        get
        {
            if (Debug.isDebugBuild)
                return true;
            return false;
        }
    }

    private StringBuilder _deviceUuid = new StringBuilder();

    private void OnEnable()
    {
        _deviceUuid.Length = 0;
        _deviceUuid.Append("ID:");

        if (!string.IsNullOrEmpty(DeviceUuidHandler.Uuid))
        {
            if (debugView)
            {
                _deviceUuid.Append(DeviceUuidHandler.Uuid);
            }
            else
            {
                _deviceUuid.Append("****");

                for (int i = 0; i < DeviceUuidHandler.Uuid.Length; i++)
                {
                    if (DeviceUuidHandler.Uuid.Length - 4 > i)
                        continue;

                    _deviceUuid.Append(DeviceUuidHandler.Uuid[i]);
                }
            }
        }
        else
        {
            _deviceUuid.Append("----");
        }

        _textUI.text = _deviceUuid.ToString();
    }
}
