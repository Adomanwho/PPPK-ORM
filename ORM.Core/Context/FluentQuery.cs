using ORM.Core.Metadata;
using ORM.Core.Querying;
using System.Linq.Expressions;

namespace ORM.Core.Context;

/// <summary>
/// Posrednički objekt između DbSet i QueryBuilder.
/// Omogućuje ulančavanje Where/OrderBy/Include/Skip/Take
/// bez izvršavanja upita sve do poziva ToList().
/// </summary>
public class FluentQuery<T> where T : class, new()
{
    private readonly DbSet<T> _dbSet;
    private readonly QueryBuilder<T> _builder;

    internal FluentQuery(DbSet<T> dbSet, EntityMetadata metadata)
    {
        _dbSet   = dbSet;
        _builder = new QueryBuilder<T>(metadata);
    }

    public FluentQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _builder.Where(predicate);
        return this;
    }

    public FluentQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        _builder.OrderBy(keySelector);
        return this;
    }

    public FluentQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        _builder.OrderByDescending(keySelector);
        return this;
    }

    public FluentQuery<T> Include<TNav>(Expression<Func<T, TNav>> navigation)
    {
        _builder.Include(navigation);
        return this;
    }

    public FluentQuery<T> Skip(int count)
    {
        _builder.Skip(count);
        return this;
    }

    public FluentQuery<T> Take(int count)
    {
        _builder.Take(count);
        return this;
    }

    /// <summary>Izvršava upit i vraća listu rezultata.</summary>
    public List<T> ToList() => _dbSet.BuildAndExecute(_builder);

    /// <summary>Vraća prvi rezultat ili null.</summary>
    public T? FirstOrDefault()
    {
        _builder.Take(1);
        return _dbSet.BuildAndExecute(_builder).FirstOrDefault();
    }
}
