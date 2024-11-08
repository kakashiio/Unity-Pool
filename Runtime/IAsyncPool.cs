
using System;

namespace IO.Unity3D.Source.Pool
{
    //******************************************
    //  
    //
    // @Author: Kakashi
    // @Email: john.cha@qq.com
    // @Date: 2024-10-15 22:14
    //******************************************
    public interface IAsyncPool<T>
    {
        void Borrow(Action<T> onBorrow);

        void Return(T t);

        void Destroy();
    }
}