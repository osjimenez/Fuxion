﻿using Fuxion.Collections;
using Fuxion.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fuxion.Test.Collections
{
	public class TreeSynchronizerTest : BaseTest
	{
		public TreeSynchronizerTest(ITestOutputHelper output) : base(output) { }
		[Fact(DisplayName = nameof(TreeSynchronizer<MockDto, int>) + " - First")]
		public void First()
		{
			var items = new ObservableCollection<MockDto>();
			var sync = new TreeSynchronizer<MockDto, int>(
				items,
				_ => _.Id,
				_ => _.ParentId,
				_ => _.Children.Select(c => c.Id).ToArray()
				//new GenericEqualityComparer<MockDto>((a, b) => a.Id == b.Id, _ => _.Id.GetHashCode())
				);
			items.Add(new MockDto(1, "Fuxion"));

			Assert.Single(sync.Tree);
		}
	}
	public class MockDto
	{
		public MockDto(int id, string name)
		{
			Id = id;
			Name = name;
		}
		public int Id { get; set; }
		public int? ParentId { get; set; }
		public string Name { get; set; }
		public ObservableCollection<MockDto> Children { get; set; } = new ObservableCollection<MockDto>();
	}
}
