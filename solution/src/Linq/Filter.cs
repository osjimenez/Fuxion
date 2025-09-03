using System;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Fuxion.Linq;

[JsonConverter(typeof(FilterConverterFactory))]
public abstract class Filter<TEntity> : IFilter
{
	public abstract Expression<Func<TEntity, bool>> Predicate { get; }
	// Indica si el filtro contiene algún criterio activo (se sobrescribe en los generados)
	public abstract bool HasAny();
}