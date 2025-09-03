using System.Collections.Generic;
using System.Linq;
using Fuxion.Collections.Generic;

namespace Fuxion.Test.Collections.Generic;

public class IEnumerableExtensionsTest : BaseTest<IEnumerableExtensionsTest>
{
	public IEnumerableExtensionsTest(ITestOutputHelper output) : base(output) { }
	[Fact(DisplayName = "IEnumerableExtensions - IsNullOrEmpty")]
	public void IsNullOrEmpty()
	{
		var col = new[] {
			"uno", "dos"
		};
		Assert.False(col.IsNullOrEmpty(), "Collection is not null or empty");
		col = [];
		Assert.True(col.IsNullOrEmpty(), "Collection is empty");
		col = null!;
		Assert.True(col.IsNullOrEmpty(), "Collection is null");
	}
	[Fact(DisplayName = "IEnumerableExtensions - RemoveNulls")]
	public void RemoveNulls()
	{
		// IEnumerable<ref>
		IEnumerable<string?> ieRef = ["", " ", "uno", null];
		Assert.Equal(3, ieRef.WhereNotNull().Count());
		Assert.Single(ieRef.WhereNeitherNullNorWhiteSpace());

		// IEnumerable<value>
		IEnumerable<int?> ieValue = [0, 1, null];
		Assert.Equal(2, ieValue.WhereNotNull().Count());
		Assert.Single(ieValue.WhereNeitherNullNorDefault());

		// IQueryable<ref>
		IQueryable<string?> queRef = new List<string?>(["", " ", "uno", null]).AsQueryable();
		Assert.Equal(3, queRef.WhereNotNull().Count());
		Assert.Single(queRef.WhereNeitherNullNorWhiteSpace());

		// IQueryable<value>
		IQueryable<int?> queValue = new List<int?>([0, 1, null]).AsQueryable();
		Assert.Equal(2, queValue.WhereNotNull().Count());
		Assert.Single(queValue.WhereNeitherNullNorDefault());

		// ICollection<ref>
		ICollection<string?> colRef = ["", " ", "uno", null];
		Assert.Equal(3, colRef.WhereNotNull().Count());
		Assert.Single(colRef.WhereNeitherNullNorWhiteSpace());

		// ICollection<value>
		ICollection<int?> colValue = [0, 1, null];
		Assert.Equal(2, colValue.WhereNotNull().Count());
		Assert.Single(colValue.WhereNeitherNullNorDefault());

		// Array<ref>
		string?[] arrRef = ["", " ", "uno", null];
		Assert.Equal(3, arrRef.WhereNotNull().Count());
		Assert.Single(arrRef.WhereNeitherNullNorWhiteSpace());

		// Array<value>
		int?[] arrValue = [0, 1, null];
		Assert.Equal(2, arrValue.WhereNotNull().Count());
		Assert.Single(arrValue.WhereNeitherNullNorDefault());
	}
	[Fact(DisplayName = "IEnumerableExtensions - RemoveOutliers")]
	public void RemoveOutliersTest()
	{
		var list = new[] {
			165, 165, 166, 167, 168, 169, 170, 170, 171, 172, 172, 174, 175, 176, 177, 178, 181
		};
		//var list = new int[] { 41, 50, 29, 33, 40, 42, 53, 35, 28, 39, 37, 43, 34, 31, 44, 57, 32, 45, 46, 48};
		//var list = new int[] { 2, 3, 3, 3, 4, 5, 5, 15 };
		var res = list.RemoveOutliers(m => Printer.WriteLine(m));
		Printer.WriteLine("");
	}
}