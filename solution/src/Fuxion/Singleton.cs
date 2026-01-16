using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Fuxion.Reflection;
using Fuxion.Threading;

namespace Fuxion;

/// <summary>
/// Provides a thread-safe singleton registry for managing global instances of objects.
/// Supports keyed instances, subscriptions to changes, and automatic instance creation.
/// </summary>
/// <remarks>
/// This class implements a registry pattern that allows storing and retrieving singleton instances
/// by type and optional key. It provides thread-safe operations and supports subscribing to
/// instance lifecycle events (Add, Remove, Set).
/// </remarks>
/// <example>
/// <code>
/// // Add a singleton instance
/// var config = new Configuration();
/// Singleton.Add(config);
/// 
/// // Retrieve the singleton instance
/// var retrieved = Singleton.Get&lt;Configuration&gt;();
/// 
/// // Subscribe to changes
/// Singleton.Subscribe&lt;Configuration&gt;(args =>
/// {
///     Console.WriteLine($"Configuration {args.Action}");
/// });
/// </code>
/// </example>
public class Singleton
{
	readonly Locker<Dictionary<SingletonKey, object?>> objects = new(new());

	#region Constants
	/// <summary>
	/// Gets the singleton constants interface for managing singleton infrastructure.
	/// </summary>
	public static ISingletonConstants Constants => null!;
	#endregion

	/// <summary>
	/// Internal key class that uniquely identifies a singleton instance by type and optional key.
	/// </summary>
	class SingletonKey
	{
		SingletonKey(Type type, object? key)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type), "El tipo no puede ser null");
			Key = key;
		}
		
		/// <summary>
		/// Gets the type of the singleton instance.
		/// </summary>
		public Type Type { get; }
		
		/// <summary>
		/// Gets the optional key that distinguishes multiple instances of the same type.
		/// </summary>
		public object? Key { get; }
		
		/// <summary>
		/// Returns a hash code for this instance using XOR combination of Type and Key hash codes.
		/// </summary>
		public override int GetHashCode()
		{
			if (Key == null) return Type.GetHashCode();
			return Type.GetHashCode() ^ Key.GetHashCode();
		}
		
		/// <summary>
		/// Determines whether the specified object is equal to the current instance.
		/// </summary>
		public override bool Equals(object? obj)
		{
			if (obj is SingletonKey key) return key.Type == Type && (key.Key == null || (key.Key != null && key.Key.Equals(Key)));
			return false;
		}
		
		static bool Compare<T>(T t1, T t2) => t1 == null && t2 == null || t1 != null && t1.Equals(t2);
		
		public static bool operator ==(SingletonKey key1, SingletonKey key2) => Compare(key1, key2);
		public static bool operator !=(SingletonKey key1, SingletonKey key2) => !Compare(key1, key2);
		
		public static SingletonKey GetKey<T>(object? key) => GetKey(typeof(T), key);
		public static SingletonKey GetKey<T>() => GetKey(typeof(T), null);
		public static SingletonKey GetKey(Type type, object? key) => new(type, key);
	}

	/// <summary>
	/// Represents a subscription to singleton instance changes.
	/// </summary>
	class SubscriptionItem(Type type, SingletonKey key, Delegate action)
	{
		public Type Type { get; } = type;
		public SingletonKey Key { get; } = key;
		public Delegate Action { get; } = action;
		public void Invoke<T>(T previousValue, T actualValue, SingletonAction action) => Action.DynamicInvoke(new SingletonSubscriptionArgs<T>(previousValue, actualValue, action));
	}

	#region Singleton patern
	static Singleton? _instance;
	static readonly object lockObject = new();
	static Singleton Instance
	{
		get
		{
			lock (lockObject)
			{
				if (_instance == null) _instance = new();
				return _instance;
			}
		}
	}
	Singleton() { }
	#endregion

	#region Add
	/// <summary>
	/// Creates and adds a new instance of type <typeparamref name="T"/> to the singleton registry.
	/// </summary>
	/// <typeparam name="T">The type of instance to create and add. Must have a parameterless constructor.</typeparam>
	/// <exception cref="ArgumentException">Thrown when an instance with the same type already exists.</exception>
	public static void Add<T>()
		where T : new()
		=> Add(new T(), SingletonKey.GetKey<T>());
	
	/// <summary>
	/// Adds an instance to the singleton registry.
	/// </summary>
	/// <typeparam name="T">The type of the instance.</typeparam>
	/// <param name="objectInstance">The instance to add.</param>
	/// <returns>The added instance.</returns>
	/// <exception cref="ArgumentException">Thrown when an instance with the same type already exists.</exception>
	public static T Add<T>(T objectInstance) => Add(objectInstance, SingletonKey.GetKey<T>());
	
	/// <summary>
	/// Adds an instance to the singleton registry with a specific key.
	/// </summary>
	/// <typeparam name="T">The type of the instance.</typeparam>
	/// <param name="objectInstance">The instance to add.</param>
	/// <param name="key">The key to associate with this instance, allowing multiple instances of the same type.</param>
	/// <returns>The added instance.</returns>
	/// <exception cref="ArgumentException">Thrown when an instance with the same type and key already exists.</exception>
	public static T Add<T>(T objectInstance, object key) => Add(objectInstance, SingletonKey.GetKey<T>(key));
	
	static T Add<T>(T objectInstance, SingletonKey key)
	{
		Instance.objects.Write(dic =>
		{
			if(dic.ContainsKey(key)) throw new ArgumentException("No se puede agregar el objeto porque la combinación clave/tipo esta en uso");
			dic.Add(key, objectInstance);
		});
		foreach (var sub in Instance.subscriptions.Where(sub => sub.Type == objectInstance?.GetType() && sub.Key == key)) sub.Invoke(default!, objectInstance, SingletonAction.Add);
		return objectInstance;
	}
	
	/// <summary>
	/// Creates and adds a new instance of type <typeparamref name="T"/> to the singleton registry, 
	/// or skips if an instance already exists.
	/// </summary>
	/// <typeparam name="T">The type of instance to create and add. Must have a parameterless constructor.</typeparam>
	public static void AddOrSkip<T>()
		where T : new()
		=> AddOrSkip(new T(), SingletonKey.GetKey<T>());
	
	/// <summary>
	/// Adds an instance to the singleton registry, or skips if an instance already exists.
	/// </summary>
	/// <typeparam name="T">The type of the instance.</typeparam>
	/// <param name="objectInstance">The instance to add.</param>
	public static void AddOrSkip<T>(T objectInstance) => AddOrSkip(objectInstance, SingletonKey.GetKey<T>());
	
	/// <summary>
	/// Adds an instance to the singleton registry with a specific key, or skips if an instance already exists.
	/// </summary>
	/// <typeparam name="T">The type of the instance.</typeparam>
	/// <param name="objectInstance">The instance to add.</param>
	/// <param name="key">The key to associate with this instance.</param>
	public static void AddOrSkip<T>(T objectInstance, object key) => AddOrSkip(objectInstance, SingletonKey.GetKey<T>(key));
	
	static void AddOrSkip<T>(T objectInstance, SingletonKey key)
	{
		var added = Instance.objects.Write(dic =>
		{
			if (!dic.ContainsKey(key))
			{
				dic.Add(key,objectInstance);
				return true;
			}
			return false;
		});
		if (added)
			foreach (var sub in Instance.subscriptions.Where(sub => sub.Type == objectInstance?.GetType() && sub.Key == key))
				sub.Invoke(default!, objectInstance, SingletonAction.Add);
	}
	#endregion

	#region Remove
	/// <summary>
	/// Removes the singleton instance of type <typeparamref name="T"/> from the registry.
	/// </summary>
	/// <typeparam name="T">The type of instance to remove.</typeparam>
	/// <returns>true if the instance was found and removed; otherwise, false.</returns>
	public static bool Remove<T>() => Remove<T>(SingletonKey.GetKey<T>());
	
	/// <summary>
	/// Removes the singleton instance of type <typeparamref name="T"/> with the specified key from the registry.
	/// </summary>
	/// <typeparam name="T">The type of instance to remove.</typeparam>
	/// <param name="key">The key of the instance to remove.</param>
	/// <returns>true if the instance was found and removed; otherwise, false.</returns>
	public static bool Remove<T>(object key) => Remove<T>(SingletonKey.GetKey<T>(key));
	
	static bool Remove<T>(SingletonKey key)
	{
		var (value, result) = Instance.objects.Write(_ =>
		{
			if (!_.ContainsKey(key)) return (null, false);
			return (_[key], _.Remove(key));
		});
		if (!result) return false;
		foreach (var sub in Instance.subscriptions.Where(sub => sub.Type == value?.GetType() && sub.Key == key)) sub.Invoke(value, default, SingletonAction.Remove);
		return true;
	}
	#endregion

	#region Contains
	/// <summary>
	/// Determines whether the registry contains an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to check for.</typeparam>
	/// <returns>true if an instance exists; otherwise, false.</returns>
	public static bool Contains<T>() => Contains<T>(SingletonKey.GetKey<T>());
	
	/// <summary>
	/// Determines whether the registry contains an instance of type <typeparamref name="T"/> with the specified key.
	/// </summary>
	/// <typeparam name="T">The type to check for.</typeparam>
	/// <param name="key">The key to check for.</param>
	/// <returns>true if an instance exists; otherwise, false.</returns>
	public static bool Contains<T>(object key) => Contains<T>(SingletonKey.GetKey<T>(key));
	
	static bool Contains<T>(SingletonKey key) => Instance.objects.Read(_ => _.ContainsKey(key));
	#endregion

	#region Get
	/// <summary>
	/// Gets the singleton instance of type <typeparamref name="T"/>.
	/// If the type has a <see cref="DefaultSingletonInstanceAttribute"/>, a default instance will be created and added if not found.
	/// </summary>
	/// <typeparam name="T">The type of instance to retrieve.</typeparam>
	/// <returns>The singleton instance, or default if not found and no default instance is specified.</returns>
	public static T Get<T>() => Get<T>(null);
	
	/// <summary>
	/// Gets the singleton instance of type <typeparamref name="T"/> with the specified key.
	/// If the type has a <see cref="DefaultSingletonInstanceAttribute"/>, a default instance will be created and added if not found.
	/// </summary>
	/// <typeparam name="T">The type of instance to retrieve.</typeparam>
	/// <param name="key">The key of the instance to retrieve.</param>
	/// <param name="throwExceptionIfNotFound">If true, throws an exception when the instance is not found; otherwise, returns default.</param>
	/// <returns>The singleton instance.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the instance is not found and <paramref name="throwExceptionIfNotFound"/> is true.</exception>
	public static T Get<T>(object? key, bool throwExceptionIfNotFound = false)
	{
		var res = Get(SingletonKey.GetKey<T>(key), typeof(T));
		if (res != null) return (T)res;
		if (throwExceptionIfNotFound) throw new KeyNotFoundException("No se ha encontrado el objecto de tipo '" + typeof(T).Name + " con la clave '" + (key ?? "null") + "'.");
		return default!;
	}
	
	static object? Get(SingletonKey key, Type requestedType)
		=> Instance.objects.ReadUpgradeable(_ =>
		{
			if (_.TryGetValue(key, out var instance)) return instance;
			var att = requestedType.GetCustomAttribute<DefaultSingletonInstanceAttribute>(true, false);
			if (att == null) return _[key];
			var ins = Activator.CreateInstance(att.Type);
			Add(ins, key);
			return ins;
		});
	#endregion

	#region Find
	/// <summary>
	/// Finds the singleton instance of type <typeparamref name="T"/>.
	/// If the type has a <see cref="DefaultSingletonInstanceAttribute"/>, a default instance will be created and added if not found.
	/// </summary>
	/// <typeparam name="T">The type of instance to find.</typeparam>
	/// <returns>The singleton instance, or null if not found and no default instance is specified.</returns>
	public static T Find<T>() => Find<T>(null);
	
	/// <summary>
	/// Finds the singleton instance of type <typeparamref name="T"/> with the specified key.
	/// If the type has a <see cref="DefaultSingletonInstanceAttribute"/>, a default instance will be created and added if not found.
	/// </summary>
	/// <typeparam name="T">The type of instance to find.</typeparam>
	/// <param name="key">The key of the instance to find.</param>
	/// <param name="throwExceptionIfNotFind">If true, throws an exception when the instance is not found; otherwise, returns default.</param>
	/// <returns>The singleton instance, or null if not found.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the instance is not found and <paramref name="throwExceptionIfNotFind"/> is true.</exception>
	public static T Find<T>(object? key, bool throwExceptionIfNotFind = false)
	{
		var res = Find(SingletonKey.GetKey<T>(key), typeof(T));
		if (res != null) return (T)res;
		if (throwExceptionIfNotFind) throw new KeyNotFoundException("No se ha encontrado el objecto de tipo '" + typeof(T).Name + " con la clave '" + (key ?? "null") + "'.");
		return default!;
	}
	
	static object? Find(SingletonKey key, Type requestedType)
		=> Instance.objects.ReadUpgradeable(dic =>
		{
			if (dic.TryGetValue(key, out var expression)) return expression;
			var att = requestedType.GetCustomAttribute<DefaultSingletonInstanceAttribute>(true, false);
			if (att != null)
			{
				var ins = Activator.CreateInstance(att.Type);
				Add(ins, key);
				return ins;
			}
			return null;
		});
	#endregion

	#region Set
	/// <summary>
	/// Replaces an existing singleton instance of type <typeparamref name="T"/> with a new instance.
	/// </summary>
	/// <typeparam name="T">The type of instance to replace.</typeparam>
	/// <param name="substitute">The new instance to set.</param>
	/// <returns>true if the instance was found and replaced; otherwise, false.</returns>
	public static bool Set<T>(T substitute) => Set(SingletonKey.GetKey<T>(), substitute);
	
	/// <summary>
	/// Replaces an existing singleton instance of type <typeparamref name="T"/> with the specified key.
	/// </summary>
	/// <typeparam name="T">The type of instance to replace.</typeparam>
	/// <param name="substitute">The new instance to set.</param>
	/// <param name="key">The key of the instance to replace.</param>
	/// <returns>true if the instance was found and replaced; otherwise, false.</returns>
	public static bool Set<T>(T substitute, object key) => Set(SingletonKey.GetKey<T>(key), substitute);
	
	static bool Set<T>(SingletonKey key, T substitute)
	{
		var (previous, setted) = Instance.objects.Write(_ =>
		{
			if (_.ContainsKey(key))
			{
				var previous = (T)_[key]!;
				_[key] = substitute;
				return (previous, true);
			}
			return (default(T)!, false);
		});
		if (setted)
			foreach (var sub in Instance.subscriptions)
				if (sub.Type == typeof(T) && sub.Key == key)
					sub.Invoke(previous, substitute, SingletonAction.Set);
		return setted;
	}
	#endregion

	#region Subscriptions
	readonly List<SubscriptionItem> subscriptions = new();
	
	/// <summary>
	/// Subscribes to changes for singleton instances of type <typeparamref name="T"/>.
	/// The subscription will be notified when instances are added, removed, or replaced.
	/// </summary>
	/// <typeparam name="T">The type of instances to monitor.</typeparam>
	/// <param name="changeAction">The action to invoke when changes occur.</param>
	/// <param name="raiseAddIfAlreadyAdded">If true and an instance already exists, immediately invokes the action with an Add event.</param>
	/// <example>
	/// <code>
	/// Singleton.Subscribe&lt;Configuration&gt;(args =>
	/// {
	///     Console.WriteLine($"Configuration {args.Action}: Previous={args.PreviousValue}, Current={args.ActualValue}");
	/// });
	/// </code>
	/// </example>
	public static void Subscribe<T>(Action<SingletonSubscriptionArgs<T>> changeAction, bool raiseAddIfAlreadyAdded = true) => Subscribe(changeAction, SingletonKey.GetKey<T>(), raiseAddIfAlreadyAdded);
	
	/// <summary>
	/// Subscribes to changes for singleton instances of type <typeparamref name="T"/> with the specified key.
	/// The subscription will be notified when instances are added, removed, or replaced.
	/// </summary>
	/// <typeparam name="T">The type of instances to monitor.</typeparam>
	/// <param name="changeAction">The action to invoke when changes occur.</param>
	/// <param name="key">The key of the specific instance to monitor.</param>
	/// <param name="raiseAddIfAlreadyAdded">If true and an instance already exists, immediately invokes the action with an Add event.</param>
	public static void Subscribe<T>(Action<SingletonSubscriptionArgs<T>> changeAction, object key, bool raiseAddIfAlreadyAdded = true)
		=> Subscribe(changeAction, SingletonKey.GetKey<T>(key), raiseAddIfAlreadyAdded);
	
	static void Subscribe<T>(Action<SingletonSubscriptionArgs<T>> changeAction, SingletonKey key, bool raiseAddIfAlreadyAdded = true)
	{
		Instance.subscriptions.Add(new(typeof(T), key, changeAction));
		Instance.objects.Read(dic =>
		{
			if (raiseAddIfAlreadyAdded && dic.TryGetValue(key, out var value)) changeAction(new(default!, (T)value!, SingletonAction.Add));
		});
	}
	#endregion
}

/// <summary>
/// Defines the types of actions that can occur on a singleton instance.
/// </summary>
public enum SingletonAction
{
	/// <summary>
	/// The instance was added to the registry.
	/// </summary>
	Add,
	
	/// <summary>
	/// The instance was removed from the registry.
	/// </summary>
	Remove,
	
	/// <summary>
	/// The instance was replaced with a new instance.
	/// </summary>
	Set
}

/// <summary>
/// Provides data for singleton subscription events.
/// </summary>
/// <typeparam name="T">The type of the singleton instance.</typeparam>
public class SingletonSubscriptionArgs<T>
{
	internal SingletonSubscriptionArgs(T previousValue, T actualValue, SingletonAction action)
	{
		PreviousValue = previousValue;
		ActualValue = actualValue;
		Action = action;
	}
	
	/// <summary>
	/// Gets the action that occurred on the singleton instance.
	/// </summary>
	public SingletonAction Action { get; }
	
	/// <summary>
	/// Gets the previous value of the instance before the action.
	/// This will be default for Add actions, and the removed/replaced instance for Remove/Set actions.
	/// </summary>
	public T PreviousValue { get; }
	
	/// <summary>
	/// Gets the current value of the instance after the action.
	/// This will be the new instance for Add/Set actions, and default for Remove actions.
	/// </summary>
	public T ActualValue { get; }
}

/// <summary>
/// Defines infrastructure operations for the singleton registry.
/// </summary>
public interface ISingletonConstants
{
	/// <summary>
	/// Performs repair operations on the singleton registry infrastructure.
	/// </summary>
	void Repair();
}

/// <summary>
/// Specifies a default type to instantiate when a singleton instance is requested but not found.
/// </summary>
/// <remarks>
/// When applied to a type, this attribute enables automatic creation of singleton instances
/// when they are accessed via <see cref="Singleton.Get{T}()"/> or <see cref="Singleton.Find{T}()"/>.
/// </remarks>
/// <example>
/// <code>
/// [DefaultSingletonInstance(typeof(DefaultConfiguration))]
/// public interface IConfiguration { }
/// 
/// public class DefaultConfiguration : IConfiguration { }
/// 
/// // This will automatically create and add a DefaultConfiguration instance
/// var config = Singleton.Get&lt;IConfiguration&gt;();
/// </code>
/// </example>
public class DefaultSingletonInstanceAttribute(Type type) : Attribute
{
	/// <summary>
	/// Gets or sets the type to instantiate as the default singleton instance.
	/// </summary>
	public Type Type { get; set; } = type;
}