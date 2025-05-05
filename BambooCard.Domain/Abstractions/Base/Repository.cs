using BambooCard.Domain.DbContext;
using BambooCard.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BambooCard.Domain.Abstractions.Base;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly BambooCardDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(BambooCardDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> AsQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
    {
        return await _dbSet.Where(predicate).CountAsync();
    }

    public async Task<T> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<T> GetAsync(Expression<Func<T, bool>> predicate = null)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id)
            ?? throw new NotFoundException("Data not found");

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}