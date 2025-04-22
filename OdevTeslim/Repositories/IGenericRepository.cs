using System.Linq.Expressions;
using OdevTeslim.Models; // BaseEntity için gerekli

namespace OdevTeslim.Repositories
{
    // T'nin BaseEntity'den miras alan bir sınıf olmasını şart koşuyoruz.
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity); // EF Core değişikliği takip ettiği için async değil
        void Delete(T entity); // EF Core state'i değiştirdiği için async değil
        void DeleteRange(IEnumerable<T> entities);
    }
}