using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace KampusBag.Core.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    Task UpdateAsync(T entity);
    void Delete(T entity);
    // Filtreleme yapabilmemiz için (Örn: Maile göre kullanıcı bulma)
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}