using UnityEngine;

/// <summary>※Renard拡張機能</summary>
public class AppLanguage
{
#if DEBUG && UNITY_EDITOR
    protected static SystemLanguage _type = Application.systemLanguage;
#endif

    public static SystemLanguage Type
    {
        get
        {
#if DEBUG && UNITY_EDITOR
            return _type;
#else
            return Application.systemLanguage;
#endif
        }

#if DEBUG && UNITY_EDITOR
        set
        {
            if (Debug.isDebugBuild)
                _type = value;
        }
#endif
    }
}