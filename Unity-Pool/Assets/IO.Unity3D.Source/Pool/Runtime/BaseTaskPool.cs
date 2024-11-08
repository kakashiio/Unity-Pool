using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace IO.Unity3D.Source.Pool
{
    //******************************************
    //  
    //
    // @Author: Kakashi
    // @Email: john.cha@qq.com
    // @Date: 2024-10-15 22:17
    //******************************************
    public class BaseTaskPool<T> : ITaskPool<T>
    {
        protected Stack<T> _Cache = new ();

        private readonly int _InitSize;
        private readonly int _MaxSize;
        private readonly Func<Task<T>> _Creator;
        private readonly Action<T> _OnReturn;
        private readonly Action<T> _OnDestroy;
        private readonly Action<T> _OnBorrow;

        private bool _Destroyed;
        
        public static async Task<BaseTaskPool<T>> Build(int initSize, int maxSize, Func<Task<T>> creator, Action<T> onBorrow, Action<T> onReturn, Action<T> onDestroy)
        {
            var pool = new BaseTaskPool<T>(initSize, maxSize, creator, onBorrow, onReturn, onDestroy);
            await pool.Init();
            return pool;
        }

        protected BaseTaskPool(int initSize, int maxSize, Func<Task<T>> creator, Action<T> onBorrow, Action<T> onReturn, Action<T> onDestroy)
        {
            _MaxSize = Mathf.Max(maxSize, initSize);
            _InitSize = initSize;
            _Creator = creator;
            _OnBorrow = onBorrow ?? _DoNothing;
            _OnReturn = onReturn ?? _DoNothing;
            _OnDestroy = onDestroy ?? _DoNothing;
        }

        private void _DoNothing(T obj)
        {
        }

        protected async Task Init()
        {
            for (int i = 0; i < _InitSize; i++)
            {
                var t = await _Creator();
                Return(t);
            }
        }

        public async Task<T> Borrow()
        {
            if (_Destroyed)
            {
                Debug.LogError("Can not borrow from a destroyed pool");
                return default(T);
            }

            if (_Cache.TryPop(out T t))
            {
                _OnBorrow(t);
                return t;
            }

            t = await _Creator();
            _OnBorrow(t);
            return t;
        }

        public void Return(T t)
        {
            if (_Cache.Count < _MaxSize && !_Destroyed)
            {
                _OnReturn(t);
                _Cache.Push(t);
            }
            else
            {
                _OnDestroy(t);
            }
        }

        public void Destroy()
        {
            _Destroyed = true;
            foreach (var t in _Cache)
            {
                _OnDestroy(t);
            }
            _Cache.Clear();
        }
    }
}