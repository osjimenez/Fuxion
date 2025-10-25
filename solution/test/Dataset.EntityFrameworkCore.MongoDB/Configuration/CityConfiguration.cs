//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using MongoDB.EntityFrameworkCore.Extensions;
//using Test.Dataset.Daos;

//namespace Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;

//public class CityConfiguration : IEntityTypeConfiguration<CityDao>
//{
//	public void Configure(EntityTypeBuilder<CityDao> builder)
//	{
//		builder
//		.ToCollection("cities")
//		.HasKey(x => x.CityId);

//		builder
//		.Property(x => x.CityId)
//		.HasElementName("_id");
//	}
//}
