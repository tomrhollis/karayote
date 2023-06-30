using System.Collections.Generic;
using System.Threading.Tasks;

namespace Karayote.Database
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> List(QueryOptions<T> options);
        T? Get(int id);
        void Insert(T item);
        void Update(T item);
        void Delete(T item);

        Task Save();
    }
}
