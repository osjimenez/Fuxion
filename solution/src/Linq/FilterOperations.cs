using System.Collections.Generic;
using Fuxion.Linq.Filter.Operations;

namespace Fuxion.Linq;

public abstract class FilterOperations<T> :
	IFilterOperation<T>,
	IEqualityFilterOperations<T>,
	IInFilterOperation<T>,
	IRelationalFilterOperations<T>,
	INullabilityFilterOperations,
	ITextFilterOperations<T>,
	IHasFlagFilterOperation<T>
{
	// Backing fields
	private IReadOnlyCollection<T>? _in;

	public T? Equal
	{
		get;
		set
		{
			field = value;
			EqualSpecified = true;
		}
	}

	public T? NotEqual
	{
		get;
		set
		{
			field = value;
			NotEqualSpecified = true;
		}
	}

	public IReadOnlyCollection<T>? In
	{
		get => _in;
		set
		{
			_in = value;
			InSpecified = true;
		}
	}

	public T? GreaterThan
	{
		get;
		set
		{
			field = value;
			GreaterThanSpecified = true;
		}
	}

	public T? GreaterOrEqual
	{
		get;
		set
		{
			field = value;
			GreaterOrEqualSpecified = true;
		}
	}

	public T? LessThan
	{
		get;
		set
		{
			field = value;
			LessThanSpecified = true;
		}
	}

	public T? LessOrEqual
	{
		get;
		set
		{
			field = value;
			LessOrEqualSpecified = true;
		}
	}

	public T? BetweenFrom
	{
		get;
		set
		{
			field = value;
			BetweenFromSpecified = true;
		}
	}

	public T? BetweenTo
	{
		get;
		set
		{
			field = value;
			BetweenToSpecified = true;
		}
	}

	public bool? IsNull
	{
		get;
		set
		{
			field = value;
			IsNullSpecified = true;
		}
	}

	public bool? IsNotNull
	{
		get;
		set
		{
			field = value;
			IsNotNullSpecified = true;
		}
	}

	public T? Contains
	{
		get;
		set
		{
			field = value;
			ContainsSpecified = true;
		}
	}

	public T? StartsWith
	{
		get;
		set
		{
			field = value;
			StartsWithSpecified = true;
		}
	}

	public T? EndsWith
	{
		get;
		set
		{
			field = value;
			EndsWithSpecified = true;
		}
	}

	public bool? Empty
	{
		get;
		set
		{
			field = value;
			EmptySpecified = true;
		}
	}

	public bool? NotEmpty
	{
		get;
		set
		{
			field = value;
			NotEmptySpecified = true;
		}
	}

	public bool? CaseInsensitive
	{
		get;
		set
		{
			field = value;
			CaseInsensitiveSpecified = true;
		}
	}

	public T? HasFlag
	{
		get;
		set
		{
			field = value;
			HasFlagSpecified = true;
		}
	}

	public bool EqualSpecified { get; private set; }
	public bool NotEqualSpecified { get; private set; }
	public bool InSpecified { get; private set; }
	public bool GreaterThanSpecified { get; private set; }
	public bool GreaterOrEqualSpecified { get; private set; }
	public bool LessThanSpecified { get; private set; }
	public bool LessOrEqualSpecified { get; private set; }
	public bool BetweenFromSpecified { get; private set; }
	public bool BetweenToSpecified { get; private set; }
	public bool IsNullSpecified { get; private set; }
	public bool IsNotNullSpecified { get; private set; }
	public bool ContainsSpecified { get; private set; }
	public bool StartsWithSpecified { get; private set; }
	public bool EndsWithSpecified { get; private set; }
	public bool EmptySpecified { get; private set; }
	public bool NotEmptySpecified { get; private set; }
	public bool CaseInsensitiveSpecified { get; private set; }
	public bool HasFlagSpecified { get; private set; }

	internal bool HasSomeOperationsDefined => ((IFilterOperation)this).IsDefined;
	bool IFilterOperation.IsDefined =>
		EqualSpecified || NotEqualSpecified || (InSpecified && _in is { Count: > 0 }) ||
			GreaterThanSpecified || GreaterOrEqualSpecified || LessThanSpecified || LessOrEqualSpecified ||
			BetweenFromSpecified || BetweenToSpecified ||
			IsNullSpecified || IsNotNullSpecified || ContainsSpecified || StartsWithSpecified || EndsWithSpecified ||
			EmptySpecified || NotEmptySpecified || CaseInsensitiveSpecified || HasFlagSpecified;
}