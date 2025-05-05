using System.Linq.Expressions;

namespace BambooCard.Domain.Abstractions.Base;

public interface IRepository<T> where T : class
{
    IQueryable<T> AsQueryable();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    Task<T> GetByIdAsync(int id);
    Task<T> GetAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task DeleteAsync(int id);
    Task<int> SaveChangesAsync();
}