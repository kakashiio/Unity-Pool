using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace IO.Unity3D.Source.Pool.Sample
{
    public class BasicDemo : MonoBehaviour
    {
        public enum CreateType
        {
            Await,
            Async
        }

        public CreateType CreationType;
        public GameObject Prefab;
        
        private DebugTaskPool<A> _cSharpObjectTaskPool;
        private DebugTaskPool<GameObject> _gameObjectObjectTaskPool;

        private Stack<A> _Stack1 = new Stack<A>();
        private Stack<GameObject> _Stack2 = new Stack<GameObject>();
        
        async void Start()
        {
            _cSharpObjectTaskPool = await DebugTaskPool<A>.Build(1, 2, async () => new A(), a => Debug.Log("Borrow " + a), a => Debug.Log("Return " + a), a => Debug.Log("Destroy " + a));


            int id = 0;
            if (CreationType == CreateType.Await)
            {
                _gameObjectObjectTaskPool = await DebugTaskPool<GameObject>.Build(1, 2, async () =>
                {
                    var asyncInstantiateOperation = InstantiateAsync(Prefab);

                    var tcs = new TaskCompletionSource<GameObject>();

                    StartCoroutine(_InstantiateAsync(asyncInstantiateOperation, tcs));
                    
                    var go = await tcs.Task;
                    go.name = id++.ToString();
                    go.transform.position = Random.insideUnitSphere;
                    return go;
                }, a =>
                {
                    a.SetActive(true);
                    Debug.Log("Borrow " + a);
                }, a =>
                {
                    a.SetActive(false);
                    Debug.Log("Return " + a);
                }, a =>
                {
                    Debug.Log("Destroy " + a);
                    GameObject.Destroy(a);
                });
            }
            else if (CreationType == CreateType.Async)
            {
                DebugTaskPool<GameObject>.BuildAsync(1, 2, async () =>
                {
                    var asyncInstantiateOperation = InstantiateAsync(Prefab);

                    var tcs = new TaskCompletionSource<GameObject>();

                    StartCoroutine(_InstantiateAsync(asyncInstantiateOperation, tcs));
                    
                    var go = await tcs.Task;
                    go.name = id++.ToString();
                    go.transform.position = Random.insideUnitSphere;
                    return go;
                }, a =>
                {
                    a.SetActive(true);
                    Debug.Log("Borrow " + a);
                }, a =>
                {
                    a.SetActive(false);
                    Debug.Log("Return " + a);
                }, a =>
                {
                    Debug.Log("Destroy " + a);
                    GameObject.Destroy(a);
                }, t => _gameObjectObjectTaskPool = t);
            }

            
            Debug.Log(_gameObjectObjectTaskPool);
        }

        private IEnumerator _InstantiateAsync(AsyncInstantiateOperation<GameObject> asyncInstantiateOperation, TaskCompletionSource<GameObject> tcs)
        {
            yield return asyncInstantiateOperation;
            tcs.SetResult(asyncInstantiateOperation.Result[0]);
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pool");
            foreach (var a in _cSharpObjectTaskPool.Cache)
            {
                GUILayout.Button(a.ToString());
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Using");
            foreach (var a in _Stack1)
            {
                GUILayout.Button(a.ToString());
            }
            GUILayout.EndHorizontal();

            if (_gameObjectObjectTaskPool != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("GameObject Pool");
                foreach (var a in _gameObjectObjectTaskPool.Cache)
                {
                    GUILayout.Button(a.ToString());
                }
                GUILayout.EndHorizontal();
            
                GUILayout.BeginHorizontal();
                GUILayout.Label("Using");
                foreach (var a in _Stack2)
                {
                    GUILayout.Button(a.ToString());
                }
                GUILayout.EndHorizontal();    
            }
        }

        async void Update()
        {
            if (Input.GetKeyUp(KeyCode.A))
            {
                var a = await _cSharpObjectTaskPool.Borrow();
                if (a != null)
                {
                    _Stack1.Push(a);    
                }
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                if (_Stack1.TryPop(out A a))
                {
                    _cSharpObjectTaskPool.Return(a);
                }
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                _Stack1.Clear();
                _cSharpObjectTaskPool.Destroy();
            }
            
            if (Input.GetKeyUp(KeyCode.Q))
            {
                var a = await _gameObjectObjectTaskPool.Borrow();
                if (a != null)
                {
                    _Stack2.Push(a);    
                }
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                if (_Stack2.TryPop(out GameObject a))
                {
                    _gameObjectObjectTaskPool.Return(a);
                }
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                foreach (var go in _Stack2)
                {
                    GameObject.Destroy(go);
                }
                _Stack2.Clear();
                _gameObjectObjectTaskPool.Destroy();
            }
        }
    }

    class DebugTaskPool<T> : BaseTaskPool<T>
    {
        public IEnumerable<T> Cache => _Cache;
        
        public static async Task<DebugTaskPool<T>> Build(int initSize, int maxSize, Func<Task<T>> creator, Action<T> onBorrow, Action<T> onReturn, Action<T> onDestroy)
        {
            var pool = new DebugTaskPool<T>(initSize, maxSize, creator, onBorrow, onReturn, onDestroy);
            await pool.Init();
            return pool;
        }
        
        
        public static void BuildAsync(int initSize, int maxSize, Func<Task<T>> creator, Action<T> onBorrow, Action<T> onReturn, Action<T> onDestroy, Action<DebugTaskPool<T>> onPoolInited)
        {
            var poolTask = Build(initSize, maxSize, creator, onBorrow, onReturn, onDestroy);
            poolTask.ContinueWith((t) =>
            {
                onPoolInited(t.Result);
            });
        }

        protected DebugTaskPool(int initSize, int maxSize, Func<Task<T>> creator, Action<T> onBorrow, Action<T> onReturn, Action<T> onDestroy) : base(initSize, maxSize, creator, onBorrow, onReturn, onDestroy)
        {
        }
    }

    class A
    {
        private static int _IDG = 0;
        
        private int _ID = _IDG++;

        public override string ToString()
        {
            return $"{_ID}";
        }
    }

}
