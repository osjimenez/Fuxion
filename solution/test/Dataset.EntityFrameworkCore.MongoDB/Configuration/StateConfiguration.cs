//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using MongoDB.EntityFrameworkCore.Extensions;
//using Test.Dataset.Daos;

//namespace Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;

//public class StateConfiguration : IEntityTypeConfiguration<StateDao>
//{
//	public void Configure(EntityTypeBuilder<StateDao> builder)
//	{
//		builder
//		.ToCollection("states")
//		.HasKey(x => x.StateId);

//		builder
//		.Property(x => x.StateId)
//		.HasElementName("_id");
//	}
//}
