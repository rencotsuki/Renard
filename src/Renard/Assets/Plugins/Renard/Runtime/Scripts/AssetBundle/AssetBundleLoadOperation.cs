using System.Collections;
using UnityEngine;
#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif

using Renard;
using Renard.Debuger;

public abstract class AssetBundleLoadOperation : IEnumerator
{
    protected AssetBundleManager manager => AssetBundleManager.Singleton;
    protected bool isDebugLog => manager != null ? manager.IsDebugLog : false;
    protected bool isSetup => manager != null ? manager.IsSetup : false;
    protected bool isInit => manager != null ? manager.IsInit : false;

    protected string _bundleName = string.Empty;
    public string BundleName
    {
        get
        {
            return _bundleName;
        }
    }

    protected string _assetLevelName = string.Empty;
    public string AssetLevelName
    {
        get
        {
            return _assetLevelName;
        }
    }

    protected float _progress = 0f;
    public float Progress
    {
        get
        {
            return _progress;
        }
    }

    protected string _error = string.Empty;
    public string Error
    {
        get
        {
            return _error;
        }
    }

    public object Current
    {
        get
        {
            return null;
        }
    }

    protected void Log(DebugerLogType logType, string methodName, string message)
    {
        if (!isDebugLog)
        {
            if (logType == DebugerLogType.Info)
                return;
        }

        DebugLogger.Log(this.GetType(), logType, methodName, message);
    }

    public virtual bool MoveNext()
    {
        return !IsDone();
    }

    public virtual void Reset() { }

    public abstract bool Update();

    public abstract bool IsDone();
}

public abstract class AssetBundleLoadAssetOperation : AssetBundleLoadOperation
{
    public abstract T GetAsset<T>() where T : UnityEngine.Object;
}

namespace Renard.AssetBundleUniTask
{
    public class LoadedAssetBundle
    {
        public AssetBundle AssetBundle;
        public int ReferencedCount;

        public LoadedAssetBundle(AssetBundle assetBundle)
        {
            AssetBundle = assetBundle;
            ReferencedCount = 1;
        }
    }

#if UNITY_EDITOR

    public class AssetBundleLoadLevelSimulationOperation : AssetBundleLoadOperation
    {
        private AsyncOperation _operation = null;

        public AssetBundleLoadLevelSimulationOperation(string bundleName, string levelName, bool isAdditive)
        {
            _bundleName = bundleName;
            _assetLevelName = levelName;

            var levelPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(_bundleName, _assetLevelName);
            if (levelPaths.Length == 0)
            {
                Log(DebugerLogType.Info, "", $"There is no scene with name \"{_assetLevelName}\" in {_bundleName}");
                return;
            }

            if (isAdditive)
            {
                var parameters = new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive };
                _operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(levelPaths[0], parameters);
            }
            else
            {
                var parameters = new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single };
                _operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(levelPaths[0], parameters);
            }
        }

        public override bool Update ()
        {
            if(_operation != null)
                _progress = _operation.progress;

            return false;
        }

        public override bool IsDone ()
        {
            return _operation == null || _operation.isDone || !string.IsNullOrEmpty(_error);
        }
    }

#endif

    public class AssetBundleLoadLevelOperation : AssetBundleLoadOperation
    {
        protected bool _isAdditive;
        protected AsyncOperation _request;

        public AssetBundleLoadLevelOperation(string bundleName, string levelName, bool isAdditive)
        {
            _bundleName = bundleName;
            _assetLevelName = levelName;
            _isAdditive = isAdditive;
        }

        public override bool Update()
        {
            if (_request != null)
            {
                _progress = _request.progress;
                return false;
            }

            if (isInit)
            {
                var bundle = manager.GetLoadedAssetBundle(_bundleName, out _error);
                if (!string.IsNullOrEmpty(_error))
                {
                    Log(DebugerLogType.Info, "Update", $"loading error. {_bundleName} - {_assetLevelName}(Additive={_isAdditive}) - {_error}");
                    return false;
                }

                if (bundle != null)
                {
                    if (_isAdditive)
                    {
                        #if UNITY_5_4_OR_NEWER
                        _request = SceneManager.LoadSceneAsync(_assetLevelName, LoadSceneMode.Additive);
                        #else
                        m_Request = Application.LoadLevelAdditiveAsync(_assetLevelName);
                        #endif
                    }
                    else
                    {
                        #if UNITY_5_4_OR_NEWER
                        _request = SceneManager.LoadSceneAsync(_assetLevelName);
                        #else
                        m_Request = Application.LoadLevelAsync(_assetLevelName);
                        #endif
                    }
                    return false;
                }
            }

            return true;
        }

        public override bool IsDone()
        {
            return (_request != null && _request.isDone) || !string.IsNullOrEmpty(_error);
        }
    }

    public class AssetBundleLoadAssetOperationSimulation : AssetBundleLoadAssetOperation
    {
        private Object _simulatedObject;
        private System.IProgress<float> _iProgress = null;

        public AssetBundleLoadAssetOperationSimulation(string bundleName, string assetName, Object simulatedObject, System.IProgress<float> progress)
        {
            _bundleName = bundleName;
            _assetLevelName = assetName;
            _simulatedObject = simulatedObject;
            _iProgress = progress;
            
            #if UNITY_EDITOR
            _progress = _simulatedObject != null ? 1.0f : 0f;
            #endif
        }

        public override T GetAsset<T>()
        {
            return _simulatedObject as T;
        }

        public override bool Update()
        {
            if (_simulatedObject != null)
            {
                _progress = 1.0f;
                _iProgress?.Report(_progress);
                return false;
            }

            #if UNITY_EDITOR

            var assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(_bundleName, _assetLevelName);
            if (assetPaths.Length > 0)
            {
                _simulatedObject = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
                if (_simulatedObject != null)
                    return false;
            }

            #endif

            _error = "LoadAssetAsync: There is no asset with name \"" + _assetLevelName + "\" in " + _bundleName;
            return true;
        }

        public override bool IsDone()
        {
            return true;
        }
    }

    public class AssetBundleLoadAssetOperationFull : AssetBundleLoadAssetOperation
    {
        protected System.Type _type;
        protected AssetBundleRequest _request = null;
        protected System.IProgress<float> _iProgress = null;

        public AssetBundleLoadAssetOperationFull(string bundleName, string assetName, System.Type type, System.IProgress<float> progress)
        {
            _bundleName = bundleName;
            _assetLevelName = assetName;
            _type = type;
            _iProgress = progress;
        }

        public override T GetAsset<T>()
        {
            if (_request != null && _request.isDone)
            {
                return _request.asset as T;
            }
            else
            {
                return default(T);
            }
        }

        public override bool Update()
        {
            if (_request != null)
            {
                _progress = _request.progress;
                _iProgress.Report(_progress);
                return false;
            }

            if (!isSetup)
            {
                _error = "AssetBundleManager instance Error.";
                return false;
            }

            if (isInit)
            {
                var bundle = manager.GetLoadedAssetBundle(_bundleName, out _error);

                if (!string.IsNullOrEmpty(_error))
                {
                    Log(DebugerLogType.Info, "Update", $"loading error. {_bundleName} - {_assetLevelName}(type={_type.ToString()}) - {_error}");
                    return false;
                }

                if (bundle != null)
                {
                    _request = bundle.AssetBundle.LoadAssetAsync(_assetLevelName, _type);
                    return false;
                }
            }

            return true;
        }

        public override bool IsDone()
        {
            return (_request != null && _request.isDone) || !string.IsNullOrEmpty(_error);
        }
    }

    public class AssetBundleLoadManifestOperation : AssetBundleLoadAssetOperationFull
    {
        public AssetBundleLoadManifestOperation(string bundleName, string assetName, System.Type type, System.IProgress<float> progress)
            : base(bundleName, assetName, type, progress)
        {
        }

        public override bool Update()
        {
            base.Update();

            if (_request != null)
            {
                _progress = _request.progress;

                if (_request.isDone)
                {
                    if (manager != null)
                    {
                        manager.Manifest = GetAsset<AssetBundleManifest>();
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool IsDone()
        {
            return (manager != null || manager.Manifest != null) || !string.IsNullOrEmpty(_error);
        }
    }
}
