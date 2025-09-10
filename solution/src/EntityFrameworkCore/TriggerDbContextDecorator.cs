﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fuxion.EntityFrameworkCore;

public interface ITriggerDbContextDecorator
{
	DbContext Context { get; }
	int SaveChangesTriggered();
	Task<int> SaveChangesTriggeredAsync(CancellationToken cancellationToken = default);
	DbSet<TEntity> Set<TEntity>() where TEntity : class;
	EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
	ValueTask<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity) where TEntity : class;
	void AddRange(params object[] entities);
	Task AddRangeAsync(params object[] entities);
	EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;
	void AttachRange(params object[] entities);
	TEntity? Find<TEntity>(params object?[]? keyValues) where TEntity : class;
	object? Find(Type entityType, params object?[]? keyValues);
	ValueTask<TEntity?> FindAsync<TEntity>(Type entityType, params object?[]? keyValues) where TEntity : class;
	ValueTask<object?> FindAsync(Type entityType, params object?[]? keyValues);
	EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
	void UpdateRange(params object[] entities);
	EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;
	void RemoveRange(params object[] entities);
	EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
	IQueryable<TResult> RemoveRange<TResult>(Expression<Func<IQueryable<TResult>>> expression);
}
public class TriggerDbContextDecorator<TContext> : ITriggerDbContextDecorator, IDisposable, IAsyncDisposable where TContext : DbContext
{
	public TriggerDbContextDecorator(TContext context, IEnumerable<IBeforeSaveTrigger<TContext>> beforeSaveTriggers, IEnumerable<IAfterSaveTrigger<TContext>> afterSaveTriggers)
	{
		Context = context;
		_beforeSaveTriggers = beforeSaveTriggers;
		_afterSaveTriggers = afterSaveTriggers;
	}
	readonly IEnumerable<IAfterSaveTrigger<TContext>> _afterSaveTriggers;
	readonly IEnumerable<IBeforeSaveTrigger<TContext>> _beforeSaveTriggers;
	DbContext ITriggerDbContextDecorator.Context => Context;
	public TContext Context { get; }
	public DatabaseFacade Database => Context.Database;
	public IModel Model => Context.Model;
	public ChangeTracker ChangeTracker => Context.ChangeTracker;
	public DbContextId ContextId => Context.ContextId;
	public ValueTask DisposeAsync() => Context.DisposeAsync();
	public void Dispose() => Context.Dispose();
	public int SaveChangesTriggered()
	{
		var state = new TriggerState<TContext>(Context);
		foreach (var trigger in _beforeSaveTriggers) trigger.Run(state).Wait();
		var res = state.SaveChanges();
		foreach (var trigger in _afterSaveTriggers) trigger.Run(state).Wait();
		return res;
	}
	public async Task<int> SaveChangesTriggeredAsync(CancellationToken cancellationToken = default)
	{
		var state = new TriggerState<TContext>(Context);
		foreach (var trigger in _beforeSaveTriggers) await trigger.Run(state, cancellationToken);
		var res = await state.SaveChangesAsync(cancellationToken);
		foreach (var trigger in _afterSaveTriggers) await trigger.Run(state, cancellationToken);
		return res;
	}
	public DbSet<TEntity> Set<TEntity>() where TEntity : class => Context.Set<TEntity>();
	public EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class => Context.Add(entity);
	public ValueTask<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity) where TEntity : class => Context.AddAsync(entity);
	public void AddRange(params object[] entities) => Context.AddRange(entities);
	public Task AddRangeAsync(params object[] entities) => Context.AddRangeAsync(entities);
	public EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class => Context.Attach(entity);
	public void AttachRange(params object[] entities) => Context.AttachRange(entities);
	public TEntity? Find<TEntity>(params object?[]? keyValues) where TEntity : class => Context.Find<TEntity>(keyValues);
	public object? Find(Type entityType, params object?[]? keyValues) => Context.Find(entityType, keyValues);
	public ValueTask<TEntity?> FindAsync<TEntity>(Type entityType, params object?[]? keyValues) where TEntity : class => Context.FindAsync<TEntity>(entityType, keyValues);
	public ValueTask<object?> FindAsync(Type entityType, params object?[]? keyValues) => Context.FindAsync(entityType, keyValues);
	public EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class => Context.Update(entity);
	public void UpdateRange(params object[] entities) => Context.UpdateRange(entities);
	public EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class => Context.Remove(entity);
	public void RemoveRange(params object[] entities) => Context.RemoveRange(entities);
	public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class => Context.Entry(entity);
	public IQueryable<TResult> RemoveRange<TResult>(Expression<Func<IQueryable<TResult>>> expression) => Context.FromExpression(expression);
}