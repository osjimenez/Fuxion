﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Fuxion.Resources;
namespace Fuxion.Test
{
    public class SystemExtensionsTest : BaseTest
    {
		public SystemExtensionsTest(ITestOutputHelper output) : base(output) { }
		[Fact(DisplayName = "IsBetween - First")]
		public void IsBetween()
		{
			Assert.True(3.IsBetween(2, 4)); // With margin
			Assert.True(3.IsBetween(3, 4)); // Low limited
			Assert.True(3.IsBetween(3, 3)); // High limited
			Assert.False(3.IsBetween(1, 2)); // Low out of range
			Assert.False(3.IsBetween(4, 5)); // High out of range
		}
		[Fact(DisplayName = "Object - IsNullOrDefault")]
        public void IsNullOrDefaultTest()
        {
            string s = null;
            Assert.True(s.IsNullOrDefault());
            s = "";
            Assert.False(s.IsNullOrDefault());
            int i = 0;
            Assert.True(i.IsNullOrDefault());
            i = 1;
            Assert.False(i.IsNullOrDefault());
            Guid g = Guid.Empty;
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
		[Fact(DisplayName = "Exception - ToJson")]
		public void ExceptionToJson()
		{
			try
			{
				Output.WriteLine(nameof(GenerateException));
				GenerateException();
			}catch(Exception ex)
			{
				var json = ex.ToJson();
				Output.WriteLine(json);
			}
			finally
			{
				Output.WriteLine("");
			}
			try
			{
				Output.WriteLine(nameof(GenerateExceptionWithInner));
				GenerateExceptionWithInner();
			}
			catch (Exception ex)
			{
				var json = ex.ToJson();
				Output.WriteLine(json);
			}
			finally
			{
				Output.WriteLine("");
			}
		}
		void GenerateException() => throw new NotImplementedException("Test method for testing");
		void GenerateExceptionWithInner()
		{
			try
			{
				GenerateException();
			}
			catch (Exception ex)
			{
				throw new NotImplementedException("Test method for testing", ex);
			}
		}
		#region CloneWithJson
		[Fact(DisplayName = "System - CloneWithJson")]
        public void CloneWithJsonTest()
        {
            Base b = new Derived();
            var res = b.CloneWithJson();
            Output.WriteLine("res.GetType() = " + res.GetType().Name);
            Assert.Equal(nameof(Derived), res.GetType().Name);
        }
        class Base { }
        class Derived : Base { }
        #endregion
        #region Transform
        [Fact(DisplayName = "Object - Transform")]
        public void TransfromTest()
        {
            var res = new TransformationSource
            {
                Integer = 123,
                String = "test"
            }.Transform(source => source.Integer);

            Assert.Equal(123, res);
        }
        class TransformationSource
        {
            public int Integer { get; set; }
            public string String { get; set; }
        }
        #endregion
        #region TimeSpan
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
            Output.WriteLine("ToTimeString: "+res);

            // Only letters

            res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(onlyLetters: true);
            Assert.Contains($"1 d", res);
            Assert.Contains($"18 h", res);
            Assert.Contains($"53 m", res);
            Assert.Contains($"58 s", res);
            Assert.Contains($"123 ms", res);

            res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(3, true);
            Assert.Contains($"1 d", res);
            Assert.Contains($"18 h", res);
            Assert.Contains($"53 m", res);
            Assert.DoesNotContain($"58 s", res);
            Assert.DoesNotContain($"123 ms", res);

            res = TimeSpan.Parse("0.18:53:58.1234567").ToTimeString(3, true);
            Assert.DoesNotContain($"0 d", res);
            Assert.Contains($"18 h", res);
            Assert.Contains($"53 m", res);
            Assert.Contains($"58 s", res);
            Assert.DoesNotContain($"123 ms", res);

            res = TimeSpan.Parse("1.18:53:58.1234567").ToTimeString(6, true);
            Output.WriteLine("ToTimeString (onlyLetters): " + res);
        }
		#endregion
		#region Math
		[Fact(DisplayName = "Math - Pow")]
		public void Pow()
		{
			Assert.Equal(8, 2.Pow(3));
			Assert.Equal(8, 2L.Pow(3));
			Assert.Equal(8, 2D.Pow(3));
		}
		[Fact(DisplayName = "Math - DivisionByPowerOfTwo")]
		public void FromLong()
		{
			// Long
			Assert.Equal(26_326_605, 496_088_653L.DivisionByPowerOfTwo(25).Remainder);
			Assert.Equal(14, 496_088_653L.DivisionByPowerOfTwo(25).Quotient);
			// Bytes
			var value = new byte[] { 0x4D, 0xB6, 0x91, 0x1D, 0x00, 0x00, 0x00 };
			Assert.Equal(26_326_605, value.DivisionByPowerOfTwo(25).Remainder);
			Assert.Equal(14, value.DivisionByPowerOfTwo(25).Quotient);
		}
		[Fact(DisplayName = "String - ToByteArrayFromHexadecimal")]
		public void StringToByteArrayFromHexadecimal()
		{
			byte[] value = "FD2EAC14000000".ToByteArrayFromHexadecimal();
			Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
			Assert.Equal("FD:2E:AC:14:00:00:00", value.ToHexadecimal(separatorChar: ':'));
			Assert.Equal("00000014AC2EFD", value.ToHexadecimal(asBigEndian: true));
		}
		[Fact(DisplayName = "Bytes - FromHexadecimal")]
		public void BytesFromHexadecimal()
		{
			byte[] value = new byte[] { 0xFD, 0x2E, 0xAC, 0x14, 0x00, 0x00, 0x00 };
			Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
			value = "FD-2E-AC-14-00-00-00".ToByteArrayFromHexadecimal(separatorChar: '-');
			Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
			value = "00000014AC2EFD".ToByteArrayFromHexadecimal(isBigEndian: true);
			Assert.Equal("FD2EAC14000000", value.ToHexadecimal());
		}
		#endregion
		#region String
		[Fact(DisplayName = "String - ContainsWithComparison")]
		public void StringContainsWithComparison()
		{
			Assert.True("abc".Contains("ABC", StringComparison.InvariantCultureIgnoreCase));
			Assert.False("abc".Contains("ABC", StringComparison.InvariantCulture));
		}
		[Fact(DisplayName = "String - SearchTextInElements")]
		public void StringSearchTextInElements()
		{
			var res = new string[] { "this is ", "my t", "ex", "t for you" }.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
			Assert.Single(res);
			Assert.Equal(1, res[0].Start.ItemIndex);
			Assert.Equal(3, res[0].Start.PositionIndex);
			Assert.Equal(3, res[0].End.ItemIndex);
			Assert.Equal(0, res[0].End.PositionIndex);

			res = new string[] { "this is ", "my t", "ex", "t for you and more te", "xt" }.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
			Assert.Equal(2, res.Count);
			Assert.Equal(1, res[0].Start.ItemIndex);
			Assert.Equal(3, res[0].Start.PositionIndex);
			Assert.Equal(3, res[0].End.ItemIndex);
			Assert.Equal(0, res[0].End.PositionIndex);
			Assert.Equal(3, res[1].Start.ItemIndex);
			Assert.Equal(19, res[1].Start.PositionIndex);
			Assert.Equal(4, res[1].End.ItemIndex);
			Assert.Equal(1, res[1].End.PositionIndex);

			res = new string[] { "text" }.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
			Assert.Single(res);
			Assert.Equal(0, res[0].Start.ItemIndex);
			Assert.Equal(0, res[0].Start.PositionIndex);
			Assert.Equal(0, res[0].End.ItemIndex);
			Assert.Equal(3, res[0].End.PositionIndex);

			res = new string[] { "more text" }.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
			Assert.Single(res);
			Assert.Equal(0, res[0].Start.ItemIndex);
			Assert.Equal(5, res[0].Start.PositionIndex);
			Assert.Equal(0, res[0].End.ItemIndex);
			Assert.Equal(8, res[0].End.PositionIndex);

			res = new string[] { "more text more" }.SearchTextInElements("tExt", StringComparison.InvariantCultureIgnoreCase);
			Assert.Single(res);
			Assert.Equal(0, res[0].Start.ItemIndex);
			Assert.Equal(5, res[0].Start.PositionIndex);
			Assert.Equal(0, res[0].End.ItemIndex);
			Assert.Equal(8, res[0].End.PositionIndex);
		}
		#endregion
	}
	public struct MockStruct { }
	public class MockClass { }
	public enum MockEnum { One, Two }
}
