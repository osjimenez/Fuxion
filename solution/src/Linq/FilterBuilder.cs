namespace Fuxion.Linq;

public static class FilterBuilder
{
	public static FilterEntityBuilder<TEntity> For<TEntity>() => new();
}