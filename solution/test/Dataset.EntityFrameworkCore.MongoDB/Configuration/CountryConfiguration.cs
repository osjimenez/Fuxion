//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using MongoDB.EntityFrameworkCore.Extensions;
//using Test.Dataset.Daos;

//namespace Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;

//public class CountryConfiguration : IEntityTypeConfiguration<CountryDao>
//{
//	public void Configure(EntityTypeBuilder<CountryDao> builder)
//	{
//		builder
//		.ToCollection("countries")
//		.HasKey(x => x.CountryId);

//		builder
//		.Property(x => x.CountryId)
//		.HasElementName("_id");
//	}
//}
