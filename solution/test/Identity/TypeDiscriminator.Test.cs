﻿using System;
using System.Linq;
using Fuxion.Identity.Test.Dao;
using Fuxion.Identity.Test.Dto;
using Fuxion.Identity.Test.Dvo;
using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Identity.Test;

static class TypeDiscriminatorFactoryExtensions
{
	public static void Reset(this TypeDiscriminatorFactory me)
	{
		me.AllowMoreThanOneTypeByDiscriminator = false;
		me.ClearRegistrations();
	}
}

public class TypeDiscriminatorTest(ITestOutputHelper output) : BaseTest<TypeDiscriminatorTest>(output)
{
	[Fact(DisplayName = "TypeDiscriminator - Allow more than one type by discriminator")]
	public void AllowMoreThanOneTypeByDiscriminator()
	{
		var fac = new TypeDiscriminatorFactory {
			AllowMoreThanOneTypeByDiscriminator = true
		};
		fac.RegisterTree(typeof(BaseDvo<>), typeof(BaseDvo<>).Assembly.DefinedTypes.ToArray());
		var res = fac.AllFromId(TypeDiscriminatorIds.Person);
		Assert.Single(res);
	}
	[Fact(DisplayName = "TypeDiscriminator - Create")]
	public void Create()
	{
		var fac = new TypeDiscriminatorFactory {
			GetIdFunction = (type, att) => att?.Id ?? type.Name, GetNameFunction = (type, att) => att?.Name ?? type.Name.ToUpper()
		};
		fac.RegisterTree<BaseDao>(typeof(BaseDao).Assembly.DefinedTypes.ToArray());
		var dis = fac.FromType<DocumentDao>(true);
		Assert.NotNull(dis);
		Assert.Equal(Helpers.TypeDiscriminatorIds.Document, dis.Id);
		Assert.Equal(Helpers.TypeDiscriminatorIds.Document, dis.Name);
		Assert.Equal(TypeDiscriminator.TypeDiscriminatorId, dis.TypeKey);
		Assert.Equal(fac.DiscriminatorTypeName, dis.TypeName);
		Assert.Equal(2, dis.Inclusions.Count());
		Assert.Single(dis.Exclusions);
	}
	[Fact(DisplayName = "TypeDiscriminator - Equality")]
	public void Equality()
	{
		var fac = new TypeDiscriminatorFactory {
			AllowMoreThanOneTypeByDiscriminator = true
		};
		// Register from Base
		fac.RegisterTree<BaseDao>();
		fac.RegisterTree(typeof(BaseDvo<>));
		var d1 = fac.FromType<FileDao>(true);
		var d2 = fac.FromType(typeof(FileDvo<>));
		Assert.True(d1 == d2);
		Assert.Equal(d1, d2);
		Assert.Same(d1, d2);
		var d1c = new[] {
			d1
		};
		Assert.Contains(d2, d1c);
	}
	[Fact(DisplayName = "TypeDiscriminator - Many classes, same discriminators")]
	public void ManyClassesSameDiscriminators()
	{
		var fac = new TypeDiscriminatorFactory {
			AllowMoreThanOneTypeByDiscriminator = true
		};
		// Register from Base
		fac.RegisterTree(typeof(BaseDvo<>));
		var d2 = fac.FromType(typeof(BaseDvo<>), true);
		Assert.NotNull(d2);
		var res = d2.Inclusions.Where(d => d.Name == "Person");
		Assert.Single(res);
	}
	[Fact(DisplayName = "TypeDiscriminator - New test")]
	public void NewTest()
	{
		var fac = new TypeDiscriminatorFactory();
		fac.Register(typeof(BaseDao));
		fac.Register(typeof(LocationDao));
		fac.Register(typeof(CityDao));
		fac.Register(typeof(CountryDao));
		var dao = fac.FromType<BaseDao>(true);
		fac.Reset();
		fac.Register(typeof(BaseDao));
		fac.Register(typeof(FileDao));
		fac.Register(typeof(DocumentDao));
		fac.Register(typeof(WordDocumentDao));
		dao = fac.FromType<BaseDao>(true);
	}
	[Fact(DisplayName = "TypeDiscriminator - Not allow two same ids")]
	public void NotAllowTwoSameIds()
	{
		var fac = new TypeDiscriminatorFactory();
		Assert.Throws<Exception>(() => {
			fac.Register(typeof(BaseDao));
			fac.Register(typeof(BaseDto));
		});
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - All tree")]
	public void RegisterAllTree()
	{
		var fac = new TypeDiscriminatorFactory();
		// Register from Base
		fac.RegisterTree<BaseDao>();
		var dis = fac.FromType<DocumentDao>(true);
		Assert.NotNull(dis);
		Assert.Equal(2, dis.Inclusions.Count());
		Assert.Single(dis.Exclusions);
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - Disable")]
	public void RegisterDisabgleState()
	{
		var fac = new TypeDiscriminatorFactory();
		fac.RegisterTree<BaseDao>();
		var dis = fac.FromType<FileDao>(true);
		Assert.NotNull(dis);
		Assert.Equal(3, dis.Inclusions.Count());
		Assert.Single(dis.Exclusions);
		dis = fac.FromType<BaseDao>();
		Assert.NotNull(dis);
		Assert.Equal(7, dis.Inclusions.Count());
		Assert.Empty(dis.Exclusions);
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - File tree")]
	public void RegisterFileTree()
	{
		var fac = new TypeDiscriminatorFactory();
		// Register from File
		fac.RegisterTree<FileDao>();
		Assert.Throws<TypeDiscriminatorRegistrationValidationException>(() => { fac.FromType<DocumentDao>(); });
		fac.Reset();
		fac.RegisterTree<FileDao>();
		fac.Register<BaseDao>();
		var dis = fac.FromType<DocumentDao>(true);
		Assert.NotNull(dis);
		Assert.Equal(2, dis.Inclusions.Count());
		Assert.Single(dis.Exclusions);
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - Generic")]
	public void RegisterGeneric()
	{
		var fac = new TypeDiscriminatorFactory();
		fac.RegisterTree(typeof(FileDvo<>));
		fac.Register(typeof(BaseDvo<>));
		var dis = fac.FromType(typeof(DocumentDvo<>), true);
		Assert.NotNull(dis);
		Assert.Equal(2, dis.Inclusions.Count());
		Assert.Single(dis.Exclusions);
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - Only File")]
	public void RegisterOnlyttttFile()
	{
		var fac = new TypeDiscriminatorFactory {
			AllowMoreThanOneTypeByDiscriminator = true
		};
		// Register from generic type BaseDvo<>
		fac.RegisterTree(typeof(BaseDvo<>));
		var dis = fac.FromType(typeof(LocationDvo<>), true);
		Assert.NotNull(dis);
		Assert.Equal(3, dis.Inclusions.Count());
		Assert.Single(dis.Exclusions);
		fac.Reset();
		fac.AllowMoreThanOneTypeByDiscriminator = false;
		// Register from generic type LocationDvo<>
		fac.RegisterTree(typeof(LocationDvo<>));
		dis = fac.FromType<CityDvo>(true);
		Assert.NotNull(dis);
		Assert.Empty(dis.Inclusions);
		Assert.Single(dis.Exclusions);
		fac.Reset();
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - Twice")]
	public void RegisterTwice()
	{
		var fac = new TypeDiscriminatorFactory();
		// Register from Base
		fac.Register<BaseDao>();
		Assert.Throws<Exception>(() => { fac.Register<BaseDao>(); });
	}
	[Fact(DisplayName = "TypeDiscriminator - Register - Two trees in parallel")]
	public void RegisterTwoTrees()
	{
		var fac = new TypeDiscriminatorFactory {
			AllowMoreThanOneTypeByDiscriminator = true
		};
		// Register from Base
		fac.RegisterTree<BaseDao>();
		fac.RegisterTree(typeof(BaseDvo<>));
		fac.FromType<BaseDao>();
	}
	[Fact(DisplayName = "TypeDiscriminator - Attribute")]
	public void TypeDiscriminatorAttribute()
	{
		var fac = new TypeDiscriminatorFactory();
		fac.Register(typeof(BaseDao));
		fac.Register(typeof(LocationDao));
		fac.Register(typeof(CityDao));
		fac.Register(typeof(CountryDao));
		var dao = fac.FromType<CityDao>(true);
		Assert.NotNull(dao);
		fac.Reset();
		fac.Register(typeof(BaseDto));
		fac.Register(typeof(LocationDto));
		fac.Register(typeof(CityDto));
		fac.Register(typeof(CountryDto));
		var dto = fac.FromType<CityDto>(true);
		Assert.NotNull(dto);
		Assert.True(dao.Id == dto.Id, $"Type discriminators DAO & DTO must have same Id. Values are '{dao.Id}' and '{dto.Id}'");
		fac.ClearRegistrations();
		fac.Register(typeof(BaseDvo<>));
		fac.Register(typeof(LocationDvo<>));
		fac.Register(typeof(CityDvo));
		fac.Register(typeof(CountryDvo));
		var dvo = fac.FromId(TypeDiscriminatorIds.City);
		Assert.NotNull(dvo);
		var dvo2 = fac.FromId(TypeDiscriminatorIds.Location);
		Assert.NotNull(dvo2);
		var dvo3 = fac.FromType(typeof(LocationDvo<>));
		Assert.NotNull(dvo3);
		Assert.True(dao.Id == dvo.Id, $"Type discriminators DAO & DVO must have same Id. Values are '{dao.Id}' and '{dvo.Id}'");
		Assert.True(dvo2.Id == dvo3.Id, $"Type discriminators DAO & DVO must have same Id. Values are '{dvo2.Id}' and '{dvo3.Id}'");
		Assert.True(dvo2.Inclusions.Contains(dvo), "Type discriminator 'Location' must include 'City'");
	}
}