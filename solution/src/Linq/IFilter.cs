namespace Fuxion.Linq;

using System;
using System.Linq.Expressions;

public interface IFilter;

public interface IFilter<TEntity> : IFilter
{
	Expression<Func<TEntity, bool>> Predicate { get; }
}