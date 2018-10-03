﻿using Fuxion.Identity.Test.Dao;
using Fuxion.Identity.Test.Dto;
using Fuxion.Identity.Test.Dvo;
using Fuxion.Identity.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Fuxion.Identity.Test
{
	internal static class TypeDiscriminatorFactoryExtensions
	{
		public static void Reset(this TypeDiscriminatorFactory me)
		{
			me.AllowMoreThanOneTypeByDiscriminator = false;
			me.ClearRegistrations();
		}
	}
	public class TypeDiscriminatorTetst : Fuxion.Test.BaseTest
	{
		public TypeDiscriminatorTetst(ITestOutputHelper output) : base(output) { this.output = output; }

		private readonly ITestOutputHelper output;
		[Fact(DisplayName = "TypeDiscriminator - Equality")]
		public void Equality()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory
			{
				AllowMoreThanOneTypeByDiscriminator = true
			};
			// Register from Base
			fac.RegisterTree<BaseDao>();
			fac.RegisterTree(typeof(BaseDvo<>));

			TypeDiscriminator d1 = fac.FromType<FileDao>();
			TypeDiscriminator d2 = fac.FromType(typeof(FileDvo<>));

			Assert.True(d1 == d2);
			Assert.Equal(d1, d2);
			Assert.Same(d1, d2);

			TypeDiscriminator[] d1c = new[] { d1 };
			Assert.Contains(d2, d1c);
		}
		[Fact(DisplayName = "TypeDiscriminator - Many classes, same discriminators")]
		public void ManyClassesSameDiscriminators()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory
			{
				AllowMoreThanOneTypeByDiscriminator = true
			};
			// Register from Base
			fac.RegisterTree(typeof(BaseDvo<>));

			TypeDiscriminator d2 = fac.FromType(typeof(BaseDvo<>));

			Assert.Single(d2.Inclusions.Where(d => d.Name == "Person"));

		}
		[Fact(DisplayName = "TypeDiscriminator - Register - Twice")]
		public void RegisterTwice()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			// Register from Base
			fac.Register<BaseDao>();
			Assert.Throws<Exception>(() =>
			{
				fac.Register<BaseDao>();
			});
		}
		[Fact(DisplayName = "TypeDiscriminator - Register - Two trees in parallel")]
		public void RegisterTwoTrees()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory
			{
				AllowMoreThanOneTypeByDiscriminator = true
			};
			// Register from Base
			fac.RegisterTree<BaseDao>();
			fac.RegisterTree(typeof(BaseDvo<>));

			fac.FromType<BaseDao>();
		}
		[Fact(DisplayName = "TypeDiscriminator - Register - All tree")]
		public void RegisterAllTree()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			// Register from Base
			fac.RegisterTree<BaseDao>();
			TypeDiscriminator dis = fac.FromType<DocumentDao>();
			Assert.Equal(2, dis.Inclusions.Count());
			Assert.Single(dis.Exclusions);
		}
		[Fact(DisplayName = "TypeDiscriminator - Register - File tree")]
		public void RegisterFileTree()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			// Register from File
			fac.RegisterTree<FileDao>();
			TypeDiscriminator dis = null;
			Assert.Throws<TypeDiscriminatorRegistrationValidationException>(() =>
			{
				dis = fac.FromType<DocumentDao>();
			});
			fac.Reset();
			fac.RegisterTree<FileDao>();
			fac.Register<BaseDao>();
			dis = fac.FromType<DocumentDao>();
			Assert.Equal(2, dis.Inclusions.Count());
			Assert.Single(dis.Exclusions);
		}
		[Fact(DisplayName = "TypeDiscriminator - Register - Generic")]
		public void RegisterGeneric()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			fac.RegisterTree(typeof(FileDvo<>));
			fac.Register(typeof(BaseDvo<>));
			TypeDiscriminator dis = fac.FromType(typeof(DocumentDvo<>));
			Assert.Equal(2, dis.Inclusions.Count());
			Assert.Single(dis.Exclusions);
		}
		[Fact(DisplayName = "TypeDiscriminator - Register - Only File")]
		public void RegisterOnlyttttFile()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory
			{
				AllowMoreThanOneTypeByDiscriminator = true
			};
			// Register from generic type BaseDvo<>
			fac.RegisterTree(typeof(BaseDvo<>));
			TypeDiscriminator dis = fac.FromType(typeof(LocationDvo<>));
			Assert.Equal(3, dis.Inclusions.Count());
			Assert.Single(dis.Exclusions);

			fac.Reset();
			fac.AllowMoreThanOneTypeByDiscriminator = false;
			// Register from generic type LocationDvo<>
			fac.RegisterTree(typeof(LocationDvo<>));
			dis = fac.FromType<CityDvo>();
			Assert.Empty(dis.Inclusions);
			Assert.Single(dis.Exclusions);

			fac.Reset();
		}
		[Fact(DisplayName = "TypeDiscriminator - Register - Disable")]
		public void RegisterDisabgleState()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			fac.RegisterTree<BaseDao>();
			TypeDiscriminator dis = fac.FromType<FileDao>();
			Assert.Equal(3, dis.Inclusions.Count());
			Assert.Single(dis.Exclusions);
			dis = fac.FromType<BaseDao>();
			Assert.Equal(7, dis.Inclusions.Count());
			Assert.Empty(dis.Exclusions);
		}
		[Fact(DisplayName = "TypeDiscriminator - Create")]
		public void Create()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory
			{
				GetIdFunction = (type, att) => att?.Id ?? type.Name,
				GetNameFunction = (type, att) => att?.Name ?? type.Name.ToUpper()
			};

			fac.RegisterTree<BaseDao>(typeof(BaseDao).Assembly.DefinedTypes.ToArray());
			TypeDiscriminator dis = fac.FromType<DocumentDao>();
			Assert.Equal(TypeDiscriminatorIds.Document, dis.Id);
			Assert.Equal(TypeDiscriminatorIds.Document, dis.Name);
			Assert.Equal(TypeDiscriminator.TypeDiscriminatorId, dis.TypeId);
			Assert.Equal(fac.DiscriminatorTypeName, dis.TypeName);
			Assert.Equal(2, dis.Inclusions.Count());
			Assert.Single(dis.Exclusions);
		}
		[Fact(DisplayName = "TypeDiscriminator - New test")]
		public void NewTest()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			fac.Register(typeof(BaseDao));
			fac.Register(typeof(LocationDao));
			fac.Register(typeof(CityDao));
			fac.Register(typeof(CountryDao));

			TypeDiscriminator dao = fac.FromType<BaseDao>();

			fac.Reset();
			fac.Register(typeof(BaseDao));
			fac.Register(typeof(FileDao));
			fac.Register(typeof(DocumentDao));
			fac.Register(typeof(WordDocumentDao));

			dao = fac.FromType<BaseDao>();
		}
		[Fact(DisplayName = "TypeDiscriminator - Attribute")]
		public void TypeDiscriminatorAttribute()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();

			fac.Register(typeof(BaseDao));
			fac.Register(typeof(LocationDao));
			fac.Register(typeof(CityDao));
			fac.Register(typeof(CountryDao));

			TypeDiscriminator dao = fac.FromType<CityDao>();
			fac.Reset();
			fac.Register(typeof(BaseDto));
			fac.Register(typeof(LocationDto));
			fac.Register(typeof(CityDto));
			fac.Register(typeof(CountryDto));

			TypeDiscriminator dto = fac.FromType<CityDto>();

			Assert.True(dao.Id == dto.Id, $"Type discriminators DAO & DTO must have same Id. Values are '{dao.Id}' and '{dto.Id}'");

			fac.ClearRegistrations();
			fac.Register(typeof(BaseDvo<>));
			fac.Register(typeof(LocationDvo<>));
			fac.Register(typeof(CityDvo));
			fac.Register(typeof(CountryDvo));

			TypeDiscriminator dvo = fac.FromId(TypeDiscriminatorIds.City);
			TypeDiscriminator dvo2 = fac.FromId(TypeDiscriminatorIds.Location);
			TypeDiscriminator dvo3 = fac.FromType(typeof(LocationDvo<>));

			Assert.True(dao.Id == dvo.Id, $"Type discriminators DAO & DVO must have same Id. Values are '{dao.Id}' and '{dvo.Id}'");
			Assert.True(dvo2.Id == dvo3.Id, $"Type discriminators DAO & DVO must have same Id. Values are '{dvo2.Id}' and '{dvo3.Id}'");

			Assert.True(dvo2.Inclusions.Contains(dvo), $"Type discriminator 'Location' must include 'City'");
		}
		[Fact(DisplayName = "TypeDiscriminator - Not allow two same ids")]
		public void NotAllowTwoSameIds()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory();
			Assert.Throws<Exception>(() =>
			{
				fac.Register(typeof(BaseDao));
				fac.Register(typeof(BaseDto));
			});
		}
		[Fact(DisplayName = "TypeDiscriminator - Allow more than one type by discriminator")]
		public void AllowMoreThanOneTypeByDiscriminator()
		{
			TypeDiscriminatorFactory fac = new TypeDiscriminatorFactory
			{
				AllowMoreThanOneTypeByDiscriminator = true
			};

			fac.RegisterTree(typeof(BaseDvo<>), typeof(BaseDvo<>).Assembly.DefinedTypes.ToArray());

			IEnumerable<TypeDiscriminator> res = fac.AllFromId(TypeDiscriminatorIds.Person);

			Assert.Single(res);
		}
	}
}
