//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using MongoDB.EntityFrameworkCore.Extensions;
//using Test.Dataset.Daos;

//namespace Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;

//public class AddressConfiguration : IEntityTypeConfiguration<AddressDao>
//{
//	public void Configure(EntityTypeBuilder<AddressDao> builder)
//	{
//		builder
//		.ToCollection("addresses")
//		.HasKey(x => x.AddressId);

//		builder
//		.Property(x => x.AddressId)
//		.HasElementName("_id");
//	}
//}
