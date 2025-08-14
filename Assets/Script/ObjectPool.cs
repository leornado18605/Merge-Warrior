
using System.Collections.Generic;
using UnityEngine;

namespace ObjectPooling
{

    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
    [DisallowMultipleComponent]
    internal class PooledObject : MonoBehaviour
    {
        internal int prefabId;              
        internal Pool owner;                
        public void Release()
        {
            if (owner != null)
            {
                PoolManager.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    [DefaultExecutionOrder(-5000)]
    public sealed class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;
        private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>(256);

        [Header("Defaults for new pools created at runtime")]
        [SerializeField] private bool defaultAutoExpand = true;
        [SerializeField] private int defaultInitialSize = 8;
        [SerializeField] private int defaultMaxSize = -1; 

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PoolManager");
                    _instance = go.AddComponent<PoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #region Pool Creation & Config

        public static void CreatePool(GameObject prefab, int initialSize = -1, int maxSize = -1, bool autoExpand = true, Transform parent = null)
        {
            if (prefab == null) { Debug.LogError("CreatePool: prefab is null"); return; }
            var id = prefab.GetInstanceID();
            var mgr = Instance;
            if (!mgr._pools.TryGetValue(id, out var pool))
            {
                pool = new Pool(prefab, parent != null ? parent : mgr.transform)
                {
                    AutoExpand = autoExpand,
                    MaxSize = maxSize,
                };
                mgr._pools.Add(id, pool);
                pool.Prewarm(initialSize >= 0 ? initialSize : mgr.defaultInitialSize);
            }
            else
            {
                // Update settings if pool already exists
                pool.AutoExpand = autoExpand;
                pool.MaxSize = maxSize;
                if (initialSize > 0)
                    pool.Prewarm(initialSize);
            }
        }
        private static Pool GetOrCreatePool(GameObject prefab)
        {
            var mgr = Instance;
            var id = prefab.GetInstanceID();
            if (!mgr._pools.TryGetValue(id, out var pool))
            {
                pool = new Pool(prefab, mgr.transform)
                {
                    AutoExpand = mgr.defaultAutoExpand,
                    MaxSize = mgr.defaultMaxSize
                };
                mgr._pools.Add(id, pool);
                pool.Prewarm(mgr.defaultInitialSize);
            }
            return pool;
        }

        #endregion

        #region Public API

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) { Debug.LogError("Spawn: prefab is null"); return null; }
            var pool = GetOrCreatePool(prefab);
            var go = pool.Get(position, rotation, parent);
            return go;
        }

        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            var go = Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
            if (go != null && parent != null)
            {
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            return go;
        }

        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        public static void Release(GameObject instance)
        {
            if (instance == null) return;
            var po = instance.GetComponent<PooledObject>();
            if (po == null || po.owner == null)
            {
                // Not from a pool, destroy to avoid dangling objects.
                Destroy(instance);
                return;
            }
            po.owner.Return(instance);
        }

        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            var pool = GetOrCreatePool(prefab);
            pool.Prewarm(count);
        }

        public static void DespawnAll(GameObject prefab)
        {
            if (prefab == null) return;
            var id = prefab.GetInstanceID();
            if (Instance._pools.TryGetValue(id, out var pool))
            {
                pool.ReturnAllActive();
            }
        }

        public static void GetStats(GameObject prefab, out int active, out int inactive)
        {
            active = inactive = 0;
            if (prefab == null) return;
            var id = prefab.GetInstanceID();
            if (Instance._pools.TryGetValue(id, out var pool))
            {
                active = pool.ActiveCount;
                inactive = pool.InactiveCount;
            }
        }

        #endregion
    }

    internal sealed class Pool
    {
        private readonly GameObject _prefab;
        private readonly Transform _root; // parent container under PoolManager

        private readonly Queue<GameObject> _inactive = new Queue<GameObject>(32);
        private readonly HashSet<GameObject> _active = new HashSet<GameObject>();

        internal bool AutoExpand = true;
        internal int MaxSize = -1; // -1 unlimited total (active + inactive)

        internal int ActiveCount => _active.Count;
        internal int InactiveCount => _inactive.Count;

        internal Pool(GameObject prefab, Transform root)
        {
            _prefab = prefab;
            _root = CreateContainer(root, prefab.name);
        }

        private static Transform CreateContainer(Transform root, string prefabName)
        {
            var go = new GameObject($"Pool - {prefabName}");
            go.transform.SetParent(root, false);
            go.SetActive(false); // Keep container hidden
            return go.transform;
        }

        internal void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (MaxSize >= 0 && (_inactive.Count + _active.Count) >= MaxSize) break;
                var inst = CreateInstance();
                _inactive.Enqueue(inst);
            }
        }

        private GameObject CreateInstance()
        {
            var go = Object.Instantiate(_prefab);
            go.name = _prefab.name; // keep clean naming

            var po = go.GetComponent<PooledObject>();
            if (po == null) po = go.AddComponent<PooledObject>();
            po.prefabId = _prefab.GetInstanceID();
            po.owner = this;

            // Ensure default inactive under pool root
            go.transform.SetParent(_root, false);
            go.SetActive(false);
            return go;
        }

        internal GameObject Get(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (_inactive.Count == 0)
            {
                int total = _inactive.Count + _active.Count;
                if (MaxSize >= 0 && total >= MaxSize && !AutoExpand)
                {
                    // Reuse the oldest active (optional strategy). Here we refuse and log.
                    Debug.LogWarning($"Pool for '{_prefab.name}' is at max size {MaxSize}. Consider enabling AutoExpand or releasing instances.");
                    return null;
                }
                // Create new if allowed
                var created = CreateInstance();
                _inactive.Enqueue(created);
            }

            var go = _inactive.Dequeue();
            _active.Add(go);
            go.transform.position = position;
            go.transform.rotation = rotation;

            // Parent & transform
            if (parent != null)
            {
                go.transform.SetParent(parent, true);
            }
            else
            {
                go.transform.SetParent(null, true); // world root
            }


            // Activate and notify
            go.SetActive(true);

            foreach (var p in go.GetComponentsInChildren<IPoolable>(true))
                p.OnSpawned();

            return go;
        }

        internal void Return(GameObject instance)
        {
            if (instance == null) return;

            if (!_active.Remove(instance))
            {
                // Already inactive or from a different pool: destroy to be safe.
                Object.Destroy(instance);
                return;
            }

            foreach (var p in instance.GetComponentsInChildren<IPoolable>(true))
                p.OnDespawned();

            instance.SetActive(false);
            instance.transform.SetParent(_root, false);

            // If we have a strict MaxSize and too many cached, destroy extras.
            if (MaxSize >= 0 && (_inactive.Count + _active.Count) >= MaxSize)
            {
                Object.Destroy(instance);
                return;
            }

            _inactive.Enqueue(instance);
        }

        internal void ReturnAllActive()
        {
            // Snapshot to avoid modification during iteration
            var toReturn = ListBuffer;
            toReturn.Clear();
            foreach (var go in _active)
                toReturn.Add(go);

            foreach (var go in toReturn)
                Return(go);
        }

        // Small static buffer to minimize allocs
        private static readonly List<GameObject> ListBuffer = new List<GameObject>(256);
    }
}
