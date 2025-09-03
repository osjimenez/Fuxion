//using System.Linq.Expressions;
//using System.Text.Json;

//namespace Fuxion.Test.Linq.Expressions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Fuxion.Linq.Expressions;
//#if !STANDARD_OR_OLD_FRAMEWORKS
//public class FilterTest(ITestOutputHelper output) : BaseTest<FilterTest>(output)
//{
//	[Fact(DisplayName = "Filter")]
//	public void Filter()
//	{
//		NumberFilter<int> filter = new();
//		filter = 123;
//		List<int> list = [1,2,3,23,123,456];
//		IsTrue(NumberFilter<int>.ApplyFilter(filter, x => x).Compile().Invoke(123));
//		//var res = list.AsQueryable().Filter<int>(filter);
//		List<User> users =
//		[
//			new()
//			{
//				Name = "Bob",
//				Age = 18
//			},
//			new()
//			{
//				Name = "Alice",
//				Age = 25
//			}
//		];
//		UserFilter userFilter = new()
//		{
//			Age = 18
//		};
//		var res = users.AsQueryable().Filter(new UserFilter()
//		{
//			Age = 18
//		}).ToList();
//		IsTrue(res is [{ Name: "Bob", Age: 18 }]);
//		res = users.AsQueryable().Filter(new UserFilter()
//		{
//			Age = new NumberFilter<int>()
//			{
//				GraterOrEqualThan = 18
//			}
//		}).ToList();
//		IsTrue(res is [{ Name: "Bob", Age: 18 }, { Name: "Alice", Age: 25 }]);
//	}

//	[Fact(DisplayName = "JSON")]
//	public void Json()
//	{
//		List<User> users =
//		[
//			new()
//			{
//				Name = "Bob",
//				Age = 18
//			},
//			new()
//			{
//				Name = "Alice",
//				Age = 25
//			}
//		];
//		var json = """
//			{
//				"Age": {
//					"GraterOrEqualThan": 18
//				}
//			}
//			""";
//		var filter = json.DeserializeFromJson<UserFilter>(new JsonSerializerOptions()
//		{
//			Converters = { new FilterConverterFactory() }
//		});
//		IsTrue(filter is not null);
//		var res = users.AsQueryable().Filter(filter).ToList();
//		IsTrue(res is [{ Name: "Bob", Age: 18 }, { Name: "Alice", Age: 25 }]);
//	}
//}
//file class UserFilter : Filter<User>
//{
//	public NumberFilter<int> Age { get; set; } = new();

//	public override Expression<Func<User, bool>> Predicate =>
//		ApplyFilter(Age, u => u.Age);
//}
////public class UserFilter : Filter<User>
////{
////	public Filter<int>? Age { get; set; }
////	public Filter<string>? Name { get; set; }

////	// Esto será autogenerado por el generador de código
////	public override Expression<Func<User, bool>> Predicate =>
////		ApplyFilter(Age, u => u.Age)
////			.And(ApplyFilter(Name, u => u.Name));

////	public (string Name, Expression<Func<User, object>> expression)[] Properties => 
////		[
////			(nameof(Age), u => u.Age),
////			(nameof(Name), u => u.Name)
////		];
////}
//public class User
//{
//	public string Name { get; set; } = string.Empty;
//	public int Age { get; set; }
//}

//public abstract class NewFilter<T>
//{
	
//}
//public partial class UserFilter2 : NewFilter<User>
//{
//	public static (string Name, Expression<Func<User, object>> expression)[] Properties =>
//	[
//		(nameof(User.Age), u => u.Age),
//		(nameof(User.Name), u => u.Name)
//	];
//}

//#endif
