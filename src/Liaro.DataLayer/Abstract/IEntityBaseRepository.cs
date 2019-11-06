using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Liaro.DataLayer.Abstract
{
    public interface IEntityBaseRepository<T> where T : class, new()
    {
        IEnumerable<T> AllIncluding(params Expression<Func<T, object>>[] includeProperties);
        IEnumerable<T> GetAll();
        IQueryable<T> GetAllQueryable();
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        int Count();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        T GetSingle(Expression<Func<T, bool>> predicate);
        Task<T> GetSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);
        T GetSingle(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties);
        IEnumerable<T> FindBy(Expression<Func<T, bool>> predicate);
        Task<List<T>> FindByAsync(Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Update(T entity);
        void CloneEntity(T entity);
        void Delete(T entity);
        void DeleteWhere(Expression<Func<T, bool>> predicate);
        void Commit();
        Task<T> FirstOrDefaultAsync();
        Task CommitAsync();

    }
}