﻿using Fuxion.Identity.Test.Dao;
using Fuxion.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fuxion.Identity.DatabaseTest;

class IdentityDatabaseRepository : DbContext, IKeyValueRepository<string, IIdentity>
{
	//public DbSet<Demo1> Demo1 { get; set; }
	//public DbSet<Demo2> Demo2 { get; set; }
	public bool Exist(string key) => throw new NotImplementedException();
	public IIdentity? Find(string key) => Identity.Include(i => i.Permissions).SingleOrDefault(i => i.UserName == key);
	public IIdentity Get(string key) => Identity.Single(i => i.UserName == key);
	public void Remove(string key) => throw new NotImplementedException();
	public void Set(string key, IIdentity value) => throw new NotImplementedException();
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseInMemoryDatabase("MEMORY");
		//optionsBuilder.UseSqlServer(@"Data Source=.\sqlexpress;Initial Catalog=FuxionIdentity;Integrated Security=True");
		base.OnConfiguring(optionsBuilder);
	}
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		//modelBuilder.Entity<RolGroup>()
		//    .HasKey(t => new { t.RolId, t.GroupId });
		//modelBuilder.Entity<RolGroup>()
		//    .HasOne(rg => rg.Rol)
		//    .WithMany(rol => rol.RolGroups)
		//    .ForeignKey(pt => pt.RolId);
		//modelBuilder.Entity<RolGroup>()
		//    .HasOne(pt => pt.Group)
		//    .WithMany(t => t.RolGroups)
		//    .ForeignKey(pt => pt.RolId);
	}
	public Task<bool> ExistAsync(string key) => throw new NotImplementedException();
	public async Task<IIdentity?> FindAsync(string key) => await Identity.SingleOrDefaultAsync(i => i.UserName == key);
	public async Task<IIdentity> GetAsync(string key) => await Identity.SingleAsync(i => i.UserName == key);
	public Task RemoveAsync(string key) => throw new NotImplementedException();
	public Task SetAsync(string key, IIdentity value) => throw new NotImplementedException();
#nullable disable
	public DbSet<IdentityDao> Identity { get; set; }
	public DbSet<RolDao> Rols { get; set; }
	public DbSet<GroupDao> Groups { get; set; }
#nullable enable
}