using KampusBag.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KampusBag.Infrastructure.Persistence;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly KampusBagDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(KampusBagDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    public async Task UpdateAsync(T entity)
    {
        // EF Core'a bu nesnenin güncellendiğini söylüyoruz
        _dbSet.Update(entity);
        // Değişiklikleri kalıcı olarak veritabanına kaydediyoruz
        await _context.SaveChangesAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync(); // Veritabanına kalıcı olarak işleme komutu
    }
    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.Where(predicate).ToListAsync();
}