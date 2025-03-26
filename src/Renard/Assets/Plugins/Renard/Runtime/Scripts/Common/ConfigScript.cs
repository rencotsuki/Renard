using System;
using UnityEngine;

namespace Renard
{
    [Serializable]
    [DisallowMultipleComponent]
    public abstract class ConfigScript<T> : SingletonMonoBehaviourCustom<T> where T : ConfigScript<T>
    {
        /// <summary>生成</summary>
        public static void Create(string name = "")
        {
            if (singleton != null) return;
            singleton = OnCreateObject(string.IsNullOrEmpty(name) ? typeof(T).Name : name);
            singleton.destroyObject = true;
        }

        protected static T OnCreateObject(string objName)
        {
            var obj = new GameObject();
            obj.name = objName;
            return obj.AddComponent<T>();
        }

        protected override void Initialized()
        {
            DontDestroyOnLoad(gameObject);
        }

        public static void Load() => singleton?.OnLoad();

        protected virtual void OnLoad()
        {
            Log(DebugerLogType.Info, "OnLoad", "");
        }

        public static void Save() => singleton?.OnSave();

        protected virtual void OnSave()
        {
            Log(DebugerLogType.Info, "OnSave", "");
        }

        public static void Delete() => singleton?.OnDelete();

        protected virtual void OnDelete()
        {
            Log(DebugerLogType.Info, "OnDelete", "");
        }
    }
}
