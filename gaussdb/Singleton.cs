using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussdb
{
    public class Singleton<T> : IDisposable where T : class, new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T());
        public static T Instance
        {
            get { return instance.Value; }
        }
        public virtual void Dispose()
        {
            // Implement any necessary cleanup here
        }
    }
}
