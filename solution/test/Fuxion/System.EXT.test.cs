using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fuxion.Collections.Generic;
using Fuxion.Resources;
using Fuxion.Threading.Tasks;
using Fuxion.Xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace Fuxion.Test;

public class SystemExtensionsTest(ITestOutputHelper output) : BaseTest<SystemExtensionsTest>(output)
{
	void GenerateException() => throw new NotImplementedException("Fuxion.Test method for testing");
	void GenerateExceptionWithInner()
	{
		try
		{
			GenerateException();
		} catch (Exception ex)
		{
			throw new NotImplementedException("Fuxion.Test method for testing", ex);
		}
	}

	class Base { }

	class Derived : Base { }

	class TransformationSource
	{
		public TransformationSource(int integer, string @string)
		{
			Integer = integer;
			String = @string;
		}
		public int Integer { get; set; }
		public string String { get; set; }
	}

	[Fact(DisplayName = "Bytes - FromHexadecimal")]
	public void BytesFromHexadecimal()
	{
		var value = new byte[] {
			0xFD, 0x2E, 0xAC, 0x14, 0x00, 0x00, 0x00
		};
		Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
		value = "FD-2E-AC-14-00-00-00".ToByteArrayFromHexadecimal('-');
		Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
		value = "00000014AC2EFD".ToByteArrayFromHexadecimal(isBigEndian: true);
		Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
	}
	[Fact(DisplayName = "System - CloneWithJson")]
	public void CloneWithJsonTest()
	{
		Base b = new Derived();
		var res = b.CloneWithJson();
		Output.WriteLine("res.GetType() = " + res?.GetType().Name);
		Assert.Equal(nameof(Derived), res?.GetType().Name);
	}
	[Fact(DisplayName = "Exception - ToJson")]
	public void ExceptionToJson()
	{
		try
		{
			Output.WriteLine(nameof(GenerateException));
			GenerateException();
		} catch (Exception ex)
		{
			var json = ex.SerializeToJson(true);
			Output.WriteLine(json);
		} finally
		{
			Output.WriteLine("");
		}
		try
		{
			Output.WriteLine(nameof(GenerateExceptionWithInner));
			GenerateExceptionWithInner();
		} catch (Exception ex)
		{
			var json = ex.SerializeToJson(true);
			Output.WriteLine(json);
		} finally
		{
			Output.WriteLine("");
		}
	}
	[Fact(DisplayName = "Math - DivisionByPowerOfTwo")]
	public void FromLong()
	{
		// Long
		Assert.Equal(26_326_605, 496_088_653L.DivisionByPowerOfTwo(25).Remainder);
		Assert.Equal(14, 496_088_653L.DivisionByPowerOfTwo(25).Quotient);
		// Bytes
		var value = new byte[] {
			0x4D, 0xB6, 0x91, 0x1D, 0x00, 0x00, 0x00
		};
		Assert.Equal(26_326_605, value.DivisionByPowerOfTwo(25).Remainder);
		Assert.Equal(14, value.DivisionByPowerOfTwo(25).Quotient);
	}
	[Fact(DisplayName = "IsBetween - First")]
	public void IsBetween()
	{
		uint x = 2;
		var t = x.Seconds;
		// INTEGERS
		IsTrue(3.IsBetween(2, 4)); // With margin
		IsTrue(3.IsBetween(3, 4)); // Low limited
		IsFalse(3.IsBetween(true, 3, 4)); // Low limited exclusive
		IsTrue(3.IsBetween(3, 3)); // High limited
		IsFalse(3.IsBetween(1, true, 3)); // High limited exclusive
		IsFalse(3.IsBetween(4, 5)); // Low out of range
		IsFalse(3.IsBetween(1, 2)); // High out of range
		
		// DOUBLES
		IsTrue(3D.IsBetween(2, 4)); // With margin
		IsTrue(3D.IsBetween(3, 4)); // Low limited
		IsTrue(3D.IsBetween(3, 3)); // High limited
		IsFalse(3D.IsBetween(1, 2)); // Low out of range
		IsFalse(3D.IsBetween(4, 5)); // High out of range
		
		// DECIMALS
		IsTrue(3.1M.IsBetween(2, 4)); // With margin
		IsTrue(3.1M.IsBetween(3.1M, 4M)); // Low limited
		IsTrue(3.1M.IsBetween(3M, 3.1M)); // High limited
		IsFalse(3.1M.IsBetween(3.2M, 4)); // Low out of range
		IsFalse(3.1M.IsBetween(2, 3)); // High out of range

		// TIMESPAN
		IsTrue(3.Seconds.IsBetween(2.Seconds, 4.Seconds)); // With margin
		IsTrue(3.Seconds.IsBetween(3.Seconds, 4.Seconds)); // Low limited
		IsTrue(3.Seconds.IsBetween(3.Seconds, 3.Seconds)); // High limited
		IsFalse(3.Seconds.IsBetween(1.Seconds, 2.Seconds)); // Low out of range
		IsFalse(3.Seconds.IsBetween(4.Seconds, 5.Seconds)); // High out of range

		// DATE
		IsTrue(DateTime.Parse("2024/01/03 10:00:00")
			.IsBetween(DateTime.Parse("2024/01/02 10:00:00"), DateTime.Parse("2024/01/04 10:00:00"))); // With margin
		IsTrue(DateTime.Parse("2024/01/03 10:00:00")
			.IsBetween(DateTime.Parse("2024/01/03 10:00:00"), DateTime.Parse("2024/01/04 10:00:00"))); // Low limited
		IsTrue(DateTime.Parse("2024/01/03 10:00:00")
			.IsBetween(DateTime.Parse("2024/01/03 10:00:00"), DateTime.Parse("2024/01/03 10:00:00"))); // High limited
		IsFalse(DateTime.Parse("2024/01/03 10:00:00")
			.IsBetween(DateTime.Parse("2024/01/01 10:00:00"), DateTime.Parse("2024/01/02 10:00:00"))); // Low out of range
		IsFalse(DateTime.Parse("2024/01/03 10:00:00")
			.IsBetween(DateTime.Parse("2024/01/04 10:00:00"), DateTime.Parse("2024/01/05 10:00:00"))); // High out of range

	}
	[Fact(DisplayName = "Object - IsNullOrDefault")]
	public void IsNullOrDefaultTest()
	{
		string? s = null;
		Assert.True(s.IsNullOrDefault());
		s = "";
		Assert.False(s.IsNullOrDefault());
		var i = 0;
		Assert.True(i.IsNullOrDefault());
		i = 1;
		Assert.False(i.IsNullOrDefault());
		decimal? d = 0M;
		Assert.True(d.IsNullOrDefault());
		d = 1M;
		Assert.False(d.IsNullOrDefault());
		var g = Guid.Empty;
		Assert.True(g.IsNullOrDefault());
		g = Guid.NewGuid();
		Assert.False(g.IsNullOrDefault());
		int? i2 = null;
		Assert.True(i2.IsNullOrDefault());
		i2 = null;
		Assert.True(i2.IsNullOrDefault());
		i2 = 1;
		Assert.False(i2.IsNullOrDefault());
	}
	[Fact(DisplayName = "Math - Pow")]
	public void Pow()
	{
		Assert.Equal(8, 2.Pow(3));
		Assert.Equal(8, 2L.Pow(3));
		Assert.Equal(8, 2D.Pow(3));
	}
	[Fact(DisplayName = "String - RandomString")]
	public void StringRandomString()
	{
		Output.WriteLine($"{"".RandomString(10)}");
		Output.WriteLine($"{"".RandomString(10)}");
		Output.WriteLine($"{"".RandomString(10)}");
	}
	[Fact(DisplayName = "String - ContainsWithComparison")]
	public void StringContainsWithComparison()
	{
		Assert.True("abc".Contains("ABC", StringComparison.InvariantCultureIgnoreCase));
		Assert.False("abc".Contains("ABC", StringComparison.InvariantCulture));
	}
	[Fact(DisplayName = "String - SearchTextInElements")]
	public void StringSearchTextInElements()
	{
		var res = new[] {
			"this is ", "my t", "ex", "t for you"
		}.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
		Assert.Single(res);
		Assert.Equal(1, res[0].Start.ItemIndex);
		Assert.Equal(3, res[0].Start.PositionIndex);
		Assert.Equal(3, res[0].End.ItemIndex);
		Assert.Equal(0, res[0].End.PositionIndex);
		res = new[] {
			"this is ", "my t", "ex", "t for you and more te", "xt"
		}.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
		Assert.Equal(2, res.Count);
		Assert.Equal(1, res[0].Start.ItemIndex);
		Assert.Equal(3, res[0].Start.PositionIndex);
		Assert.Equal(3, res[0].End.ItemIndex);
		Assert.Equal(0, res[0].End.PositionIndex);
		Assert.Equal(3, res[1].Start.ItemIndex);
		Assert.Equal(19, res[1].Start.PositionIndex);
		Assert.Equal(4, res[1].End.ItemIndex);
		Assert.Equal(1, res[1].End.PositionIndex);
		res = new[] {
			"text"
		}.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
		Assert.Single(res);
		Assert.Equal(0, res[0].Start.ItemIndex);
		Assert.Equal(0, res[0].Start.PositionIndex);
		Assert.Equal(0, res[0].End.ItemIndex);
		Assert.Equal(3, res[0].End.PositionIndex);
		res = new[] {
			"more text"
		}.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
		Assert.Single(res);
		Assert.Equal(0, res[0].Start.ItemIndex);
		Assert.Equal(5, res[0].Start.PositionIndex);
		Assert.Equal(0, res[0].End.ItemIndex);
		Assert.Equal(8, res[0].End.PositionIndex);
		res = new[] {
			"more text more"
		}.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
		Assert.Single(res);
		Assert.Equal(0, res[0].Start.ItemIndex);
		Assert.Equal(5, res[0].Start.PositionIndex);
		Assert.Equal(0, res[0].End.ItemIndex);
		Assert.Equal(8, res[0].End.PositionIndex);
	}
	[Fact(DisplayName = "String - ToByteArrayFromHexadecimal")]
	public void StringToByteArrayFromHexadecimal()
	{
		var value = "FD2EAC14000000".ToByteArrayFromHexadecimal();
		Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
		Assert.Equal("FD:2E:AC:14:00:00:00", value.ToHexadecimal(':'));
		Assert.Equal("00000014AC2EFD", value.ToHexadecimal(asBigEndian: true));
	}
	[Fact(DisplayName = "String - SplitInLines")]
	public void StringSplitInLines()
	{
		var str = "start\r\nline\r\n\r\n trim \r\nend";
		var lines = str.SplitInLines(false);
		Assert.Equal(5, lines.Length);
		Assert.Equal("start", lines[0]);
		Assert.Equal("line", lines[1]);
		Assert.Equal("", lines[2]);
		Assert.Equal(" trim ", lines[3]);
		Assert.Equal("end", lines[4]);

		lines = str.SplitInLines(true);
		Assert.Equal(4, lines.Length);
		Assert.Equal("start", lines[0]);
		Assert.Equal("line", lines[1]);
		Assert.Equal(" trim ", lines[2]);
		Assert.Equal("end", lines[3]);
#if !STANDARD_OR_OLD_FRAMEWORKS
		lines = str.SplitInLines(false, true);
		Assert.Equal(5, lines.Length);
		Assert.Equal("start", lines[0]);
		Assert.Equal("line", lines[1]);
		Assert.Equal("", lines[2]);
		Assert.Equal("trim", lines[3]);
		Assert.Equal("end", lines[4]);

		lines = str.SplitInLines(true, true);
		Assert.Equal(4, lines.Length);
		Assert.Equal("start", lines[0]);
		Assert.Equal("line", lines[1]);
		Assert.Equal("trim", lines[2]);
		Assert.Equal("end", lines[3]);
#endif
	}
	[Fact(DisplayName = "String - IsNullOrWhiteSpace")]
	public void StringIsNullOrWhiteSpace()
	{
		string? test = null;
		Output.WriteLine("test = null");
		IsTrue(test.IsNullOrEmpty());
		IsTrue(test.IsNullOrWhiteSpace());
		IsTrue(test.IsNullOrDefault());
		IsFalse(test.IsNeitherNullNorEmpty());
		IsFalse(test.IsNeitherNullNorWhiteSpace());
		test = "";
		Output.WriteLine("test = \"\"");
		IsTrue(test.IsNullOrEmpty());
		IsTrue(test.IsNullOrWhiteSpace());
		IsFalse(test.IsNullOrDefault());
		IsFalse(test.IsNeitherNullNorEmpty());
		IsFalse(test.IsNeitherNullNorWhiteSpace());
		test = " ";
		Output.WriteLine("test = \" \"");
		IsFalse(test.IsNullOrEmpty());
		IsTrue(test.IsNullOrWhiteSpace());
		IsFalse(test.IsNullOrDefault());
		IsTrue(test.IsNeitherNullNorEmpty());
		IsFalse(test.IsNeitherNullNorWhiteSpace());
		test = "a";
		Output.WriteLine("test = \"a\"");
		IsFalse(test.IsNullOrEmpty());
		IsFalse(test.IsNullOrWhiteSpace());
		IsFalse(test.IsNullOrDefault());
		IsTrue(test.IsNeitherNullNorEmpty());
		IsTrue(test.IsNeitherNullNorWhiteSpace());
	}
	[Fact(DisplayName = "TimeSpan - ToTimeString")]
	public void TimeSpan_ToTimeString()
	{
		var res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString();
		Assert.Contains($"1 {Strings.day}", res);
		Assert.Contains($"18 {Strings.hours}", res);
		Assert.Contains($"53 {Strings.minutes}", res);
		Assert.Contains($"58 {Strings.seconds}", res);
		Assert.Contains($"123 {Strings.milliseconds}", res);
		res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(3);
		Assert.Contains($"1 {Strings.day}", res);
		Assert.Contains($"18 {Strings.hours}", res);
		Assert.Contains($"53 {Strings.minutes}", res);
		Assert.DoesNotContain($"58 {Strings.seconds}", res);
		Assert.DoesNotContain($"123 {Strings.milliseconds}", res);
		res = TimeSpan.Parse("0.18:53:58.1234567").ToTimeString(3);
		Assert.DoesNotContain($"0 {Strings.day}", res);
		Assert.Contains($"18 {Strings.hours}", res);
		Assert.Contains($"53 {Strings.minutes}", res);
		Assert.Contains($"58 {Strings.seconds}", res);
		Assert.DoesNotContain($"123 {Strings.milliseconds}", res);
		res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(6);
		Output.WriteLine("ToTimeString: " + res);

		// Only letters
		res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(onlyLetters: true);
		Assert.Contains("1 d", res);
		Assert.Contains("18 h", res);
		Assert.Contains("53 m", res);
		Assert.Contains("58 s", res);
		Assert.Contains("123 ms", res);
		res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(3, true);
		Assert.Contains("1 d", res);
		Assert.Contains("18 h", res);
		Assert.Contains("53 m", res);
		Assert.DoesNotContain("58 s", res);
		Assert.DoesNotContain("123 ms", res);
		res = TimeSpan.Parse("0.18:53:58.1234567").ToTimeString(3, true);
		Assert.DoesNotContain("0 d", res);
		Assert.Contains("18 h", res);
		Assert.Contains("53 m", res);
		Assert.Contains("58 s", res);
		Assert.DoesNotContain("123 ms", res);
		res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(6, true);
		Output.WriteLine("ToTimeString (onlyLetters): " + res);
		PrintVariable(3.Seconds);
	}
	[Fact(DisplayName = "Object - Transform")]
	public async Task TransformTest()
	{
		var source = new TransformationSource(0, "test");

		source.Transform(s => { s.Integer = 123; });
		Assert.Equal(123, source.Integer);

		var res = source.Transform(s => s.Integer);
		Assert.Equal(123, res);
	}
	[Fact(DisplayName = "Object - ThenTransform")]
	public async Task ThenTransformTest()
	{
		var sourceTask = TaskManager.StartNew(() => new TransformationSource(0, "test"));

		var source = await sourceTask.ThenTransform(s => { s.Integer = 123; });
		Assert.Equal(123, source.Integer);

		var res = await sourceTask.ThenTransform(s => s.Integer);
		Assert.Equal(123, res);
	}
	[Fact(DisplayName = "Object - TransformIfNotNull")]
	public async Task TransformIfNotNullTest()
	{
		TransformationSource? source = null;

		source = source.TransformIfNotNull(s =>
		{
			s.String = "changed";
		});
		Assert.Null(source);
		var res = source.TransformIfNotNull(s => s?.String);
		Assert.Null(res);

		source = new(123, "test");

		source = source.TransformIfNotNull(s =>
		{
			s.String = "changed";
		});
		Assert.Equal("changed", source?.String);
		res = source.TransformIfNotNull(s => s?.String);
		Assert.Equal("changed", res);
	}
	[Fact(DisplayName = "Object - ThenTransformIfNotNull")]
	public async Task ThenTransformIfNotNullTest()
	{
		var sourceTask = TaskManager.StartNew(() => (TransformationSource?)null);

		var source = await sourceTask.ThenTransformIfNotNull(s =>
		{
			s.String = "changed";
		});
		Assert.Null(source);
		var res = await sourceTask.ThenTransformIfNotNull(s => s?.String);
		Assert.Null(res);

		sourceTask = TaskManager.StartNew(() => (TransformationSource?)new TransformationSource(0, "test"));

		source = await sourceTask.ThenTransformIfNotNull(s =>
		{
			s.String = "changed";
		});
		Assert.Equal("changed", source?.String);
		res = await sourceTask.ThenTransformIfNotNull(s => s?.String);
		Assert.Equal("changed", res);
	}
	[Fact(DisplayName = "Type - IsNullable")]
	public void TypeIsNullable()
	{
		Assert.False(typeof(MockStruct).IsNullable());
		Assert.True(typeof(MockStruct?).IsNullable());
		Assert.False(typeof(int).IsNullable());
		Assert.True(typeof(int?).IsNullable());
		Assert.True(typeof(MockClass).IsNullable());
		Assert.False(typeof(MockEnum).IsNullable());
		Assert.True(typeof(MockEnum?).IsNullable());
	}
	[Fact(DisplayName = "Type - IsNullableEnum")]
	public void TypeIsNullableEnum()
	{
		Assert.False(typeof(MockStruct).IsNullableEnum());
		Assert.False(typeof(MockStruct?).IsNullableEnum());
		Assert.False(typeof(int).IsNullableEnum());
		Assert.False(typeof(int?).IsNullableEnum());
		Assert.False(typeof(MockClass).IsNullableEnum());
		Assert.False(typeof(MockEnum).IsNullableEnum());
		Assert.True(typeof(MockEnum?).IsNullableEnum());
	}
	[Fact(DisplayName = "Type - IsNullableValue<T>")]
	public void TypeIsNullableStruct()
	{
		Assert.False(typeof(int).IsNullableValue<int>());
		Assert.True(typeof(int?).IsNullableValue<int>());
		Assert.False(typeof(int).IsNullableValue<long>());
		Assert.False(typeof(int?).IsNullableValue<long>());
		Assert.False(typeof(MockEnum).IsNullableValue<MockEnum>());
		Assert.True(typeof(MockEnum?).IsNullableValue<MockEnum>());
	}
	[Fact(DisplayName = "Type - GetSignature")]
	public void GetSignature()
	{
		Assert.Equal("bool", typeof(bool).GetSignature());
		Assert.Equal("byte", typeof(byte).GetSignature());
		Assert.Equal("byte[]", typeof(byte[]).GetSignature());
		Assert.Equal("List<byte[]>", typeof(List<byte[]>).GetSignature());
		// TODO Esto no va
		//Assert.Equal("List<(string Name, int Age)>", typeof(List<(string Name, int Age)>).GetSignature());
	}
	[Fact(DisplayName = "Range - Custom integer enumerator")]
	public void CustomIntEnumerator()
	{
#if !OLD_FRAMEWORKS
		// TODO hacer que funcione en net472
		Logger.LogInformation($"Enumerate with range:");
		foreach (var i in 0..10) Logger.LogInformation($"\t{i}");
		Assert.Throws<NotSupportedException>(() => {
			foreach (var i in 5..) Logger.LogInformation($"\t{i}");
		});
#endif

		Logger.LogInformation($"Enumerate with int:");
		foreach (var i in 10) Logger.LogInformation($"\t{i}");
		Assert.Throws<ArgumentException>("number", () =>
		{
			foreach (var i in -10) Logger.LogInformation($"\t{i}");
		});
	}
	[Fact(DisplayName = "Enumerable<DateTime> - Average")]
	public void DateTimeEnumerableAverage()
	{
		List<DateTime> list = [
			DateTime.Parse("2025-01-01"),
			DateTime.Parse("2025-01-10")
		];
		PrintVariable(list.AverageDateTime());
	}
	[Fact(DisplayName = "Enumerable<DateTimeOffset> - Average")]
	public void DateTimeOffsetEnumerableAverage()
	{
		List<DateTimeOffset> list = [
			DateTimeOffset.Parse("2025-01-01T10:00:00+2"),
			DateTimeOffset.Parse("2025-01-10T10:00:00-2")
		];
		PrintVariable(list.AverageDateTime());
		list = [
			DateTimeOffset.Parse("2025-01-01T10:00:00+0"),
			DateTimeOffset.Parse("2025-01-10T10:00:00+1")
		];
		PrintVariable(list.AverageDateTime());
	}
	[Fact(DisplayName = "Enumerable - DistributeAsPercentages")]
	public void DistributeAsPercentages()
	{
		Assert.Throws<TrueException>(() => Do(100, [50, 60]));
		Assert.Throws<TrueException>(() => Do(1, [50, 50]));
		Do(5, [0.1d, 9.9d, 20, 40, 30]);
		Do(50, [0.1d, 9.9d, 20, 40, 30]);
		Do(1000, [0.1d, 9.9d, 20, 40, 30]);
		Do(5884, [0.1d, 9.9d, 20, 40, 30]);
		Do(5884, [20, 40, 40]);
		return;
		void Do(int num, List<double> list)
		{
			Logger.LogInformation("================================");
			Logger.LogInformation($"Num: {num}");
			Logger.LogInformation("Percentage List:");
			foreach (var percentage in list) Logger.LogInformation($"\t{percentage}");
			var response = list.DistributeAsPercentages(num);
			IsTrue(response.IsSuccess);
			Assert.NotNull(response.Payload);
			var res = response.Payload;
			Logger.LogInformation("Results:");
			foreach (var r in res) Logger.LogInformation($"\t{r.Percentage:N2}% - {r.Rounded.ToString(),-5} - {r.Exact:N2}");
			Logger.LogInformation($"Sum exact: {res.Sum(_ => _.Exact)}");
			Logger.LogInformation($"Sum roundad: {res.Sum(_ => _.Rounded)}");
			if (res.Sum(_ => _.Rounded) > num) Logger.LogWarning("ATENCION !!!");
		}
	}
	[Fact(DisplayName = "Enumerable - TakeRandomly")]
	public void TakeRandomly()
	{
		List<string> list = new() { "One", "Two", "Three", "Four", "Five" };
		void Do(int take)
		{
			var res = list.TakeRandomly(take);
			Logger.LogInformation($"Ran: {res.Aggregate((a, b) => $"{a},{b}")}");
		}
		Do(1);
		Do(1);
		Do(1);
		Do(1);
		Do(2);
		Do(2);
		Do(2);
		Do(2);
	}
}

public struct MockStruct { }

public class MockClass { }

public enum MockEnum
{
	One,
	Two
}