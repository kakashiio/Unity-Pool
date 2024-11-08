using System.Threading.Tasks;

namespace IO.Unity3D.Source.Pool
{
    //******************************************
    //  
    //
    // @Author: Kakashi
    // @Email: john.cha@qq.com
    // @Date: 2024-10-15 22:14
    //******************************************
    public interface ITaskPool<T>
    {
        Task<T> Borrow();

        void Return(T t);

        void Destroy();
    }
}