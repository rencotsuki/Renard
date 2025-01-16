using System;
using UnityEngine;
using Renard.Debuger;

/// <summary>※Renard拡張機能</summary>
[Serializable]
public abstract class SingletonMonoBehaviourCustom<T> : MonoBehaviourCustom where T : SingletonMonoBehaviourCustom<T>
{
    public static bool Exists => singleton != null;

    protected static T singleton;
    public static T Singleton
    {
        get
        {
            if (singleton == null)
            {
                try
                {
                    singleton = (T)FindFirstObjectByType(typeof(T));
                }
                catch (Exception ex)
                {
                    DebugLogger.Log(typeof(T), DebugerLogType.Error, "Instance", $"FindFirstObjectByType error. {ex.Message}");
                }
            }
            return singleton;
        }
    }

    [SerializeField] protected bool destroyObject = false;

    protected virtual void Awake()
    {
        if (!CheckInstance())
        {
            if (destroyObject)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(this);
            }
            return;
        }

        if (IsDebugLog)
            DebugLogger.Log(typeof(T), DebugerLogType.Info, "Awake", $"{typeof(T)} is create: InstanceId={this.GetInstanceID()}");

        Initialized();
    }

    protected virtual void OnDestroy()
    {
        if (singleton != null && singleton.GetInstanceID() == this.GetInstanceID())
        {
            if (IsDebugLog)
                DebugLogger.Log(typeof(T), DebugerLogType.Info, "OnDestroy", $"{typeof(T)} is destroy: InstanceId={this.GetInstanceID()}");

            singleton = null;
        }
    }

    protected bool CheckInstance()
    {
        if (singleton == null)
        {
            singleton = (T)this;
            return true;
        }
        else if (singleton == this)
        {
            return true;
        }

        return false;
    }

    protected virtual void Initialized() { }

    public void Destruction() => Destruction(destroyObject);
    public void Destruction(bool destroyObject)
    {
        if (destroyObject)
        {
            Destroy(singleton.gameObject);
        }
        else
        {
            Destroy(singleton);
        }
    }
}
