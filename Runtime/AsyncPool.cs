using System;
using System.Collections.Generic;
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
    public class AsyncPool<T> : IAsyncPool<T>
    {
        protected Stack<T> _Cache = new ();

        private readonly int _InitSize;
        private readonly int _MaxSize;
        private readonly Action<Action<T>> _Creator;
        private readonly Action<T> _OnReturn;
        private readonly Action<T> _OnDestroy;

        private bool _Destroyed;
        
        public static AsyncPool<T> Build(int initSize, int maxSize, Action<Action<T>> creator, Action<T> onReturn, Action<T> onDestroy)
        {
            var pool = new AsyncPool<T>(initSize, maxSize, creator, onReturn, onDestroy);
            pool.Init();
            return pool;
        }

        protected AsyncPool(int initSize, int maxSize, Action<Action<T>> creator, Action<T> onReturn, Action<T> onDestroy)
        {
            _MaxSize = Mathf.Max(maxSize, initSize);
            _InitSize = initSize;
            _Creator = creator;
            _OnReturn = onReturn ?? _DoNothing;
            _OnDestroy = onDestroy ?? _DoNothing;
        }

        private void _DoNothing(T obj)
        {
        }

        protected void Init()
        {
            for (int i = 0; i < _InitSize; i++)
            {
                _Creator(Return);
            }
        }

        public void Borrow(Action<T> onBorrow)
        {
            if (_Destroyed)
            {
                Debug.LogError("Can not borrow from a destroyed pool");
                onBorrow(default(T));
                return;
            }

            if (_Cache.TryPop(out T t))
            {
                onBorrow(t);
                return;
            }

            _Creator(onBorrow);
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