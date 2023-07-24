using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Adapted from a textbook example
namespace Karayote.Database
{
    internal class Repository<T> : IRepository<T> where T : class
    {
        protected KYContext context { get; set; }
        private DbSet<T> dbset { get; set; }

        public Repository(KYContext ctx)
        {
            context = ctx;
            dbset = context.Set<T>();
        }

        public void Delete(T item)
        {
            dbset.Remove(item);
        }

        public T? Get(int id)
        {
            return dbset.Find(id);
        }

        public void Insert(T item)
        {
            dbset.Add(item);
        }

        public virtual IEnumerable<T> List(QueryOptions<T> options)
        {
            IQueryable<T> query = dbset;
            if (options.HasIncludes)
            {
                foreach (string i in options.Includes)
                {
                    query = query.Include(i);
                }
            }
            if (options.HasWhere) query = query.Where(options.Where!);
            if (options.HasOrderBy) query = query.OrderBy(options.OrderBy!);

            return query.ToList();
        }

        public async Task Save()
        {
            await context.SaveChangesAsync();
        }

        public void Update(T item)
        {
            dbset.Update(item);
        }
    }
}
