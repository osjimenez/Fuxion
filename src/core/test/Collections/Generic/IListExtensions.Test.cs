﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fuxion.Test.Collections.Generic
{
	public class IListExtensionsTest : BaseTest
	{
		public IListExtensionsTest(ITestOutputHelper output) : base(output) { }
		[Fact(DisplayName = "IListExtensions - RemoveIf")]
		public void RemoveIf()
		{
			var col = new[] { new Mock
			{
				Id = 1,
				Name = "One"
			}, new Mock
			{
				Id = 1,
				Name = "Two"
			} }.ToList();
			// Remove do not remove the propper element because both are the same id and Remove use Equals overrided for detemine the element to remove
			col.Remove(col.First(m=>m.Name == "Two"));
			// Only element with name "Two" is in collection
			Assert.Single(col);
			Assert.Contains(col, m => m.Name == "Two");

			col = new[] { new Mock
			{
				Id = 1,
				Name = "One"
			}, new Mock
			{
				Id = 1,
				Name = "Two"
			} }.ToList();
			col.RemoveIf(m => m.Name == "Two");
			// Only element with name "One" is in collection
			Assert.Single(col);
			Assert.Contains(col, m => m.Name == "One");
		}
	}
	public class Mock
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is Mock mock) return Id == mock.Id;
			return false;
		}
		public override int GetHashCode() => Id.GetHashCode();
	}
}
