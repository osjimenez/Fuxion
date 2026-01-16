using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fuxion.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Fuxion.AspNetCore;

/// <summary>
///    Defines the contract for endpoint mapping in ASP.NET Core minimal APIs.
/// </summary>
/// <remarks>
///    <para>
///       Implement this interface to create endpoint definitions that can be automatically
///       discovered and registered during application startup. Endpoints implementing this
///       interface are registered directly on the <see cref="IEndpointRouteBuilder"/> without
///       any route group association.
///    </para>
///    <para>
///       <strong>Key features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Automatic discovery:</strong> Endpoints are automatically discovered via assembly scanning
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Type-safe registration:</strong> Strongly-typed endpoint definitions
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Modular organization:</strong> Encapsulate endpoint logic in dedicated classes
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic endpoint implementation:</strong>
///    <code>
/// public class UserEndpoints : IEndpoint
/// {
///     public void MapEndpoint(IEndpointRouteBuilder builder)
///     {
///         builder.MapGet("/api/users", async () =>
///         {
///             // Return user list
///             return Results.Ok(new[] { "User1", "User2" });
///         });
///         
///         builder.MapGet("/api/users/{id}", async (int id) =>
///         {
///             // Return specific user
///             return Results.Ok($"User {id}");
///         });
///     }
/// }
/// </code>
/// </example>
public interface IEndpoint
{
	/// <summary>
	///    Maps the endpoint routes to the specified route builder.
	/// </summary>
	/// <param name="builder">
	///    The <see cref="IEndpointRouteBuilder"/> used to register HTTP endpoints.
	/// </param>
	/// <remarks>
	///    Implement this method to define your endpoint's HTTP routes using extension methods
	///    like MapGet, MapPost, MapPut, MapDelete, etc.
	/// </remarks>
	void MapEndpoint(IEndpointRouteBuilder builder);
}

/// <summary>
///    Defines the contract for endpoint mapping within a specific route group in ASP.NET Core minimal APIs.
/// </summary>
/// <typeparam name="TRouteGroup">
///    The type of the route group that implements <see cref="IRouteGroup"/>.
///    This determines the route prefix and configuration for the endpoint.
/// </typeparam>
/// <remarks>
///    <para>
///       Implement this interface to create endpoints that belong to a specific route group.
///       The generic type parameter establishes a compile-time relationship between the endpoint
///       and its parent route group, ensuring type-safe endpoint organization.
///    </para>
///    <para>
///       <strong>Benefits of grouped endpoints:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Shared prefixes:</strong> All endpoints in a group share a common route prefix
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Common middleware:</strong> Apply middleware once to all endpoints in the group
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Logical organization:</strong> Group related endpoints together (e.g., all user operations)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Shared metadata:</strong> Apply tags, authorization, or other metadata to all group endpoints
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Endpoint within a route group:</strong>
///    <code>
/// public class ApiV1Group : IRouteGroup
/// {
///     public RouteGroupBuilder Group(IEndpointRouteBuilder builder)
///     {
///         return builder.MapGroup("/api/v1")
///             .WithTags("API Version 1");
///     }
/// }
/// 
/// public class UserEndpoints : IEndpoint&lt;ApiV1Group&gt;
/// {
///     public void MapEndpoint(IEndpointRouteBuilder builder)
///     {
///         // These will be registered under /api/v1/users
///         builder.MapGet("/users", async () => Results.Ok(new[] { "User1", "User2" }));
///         builder.MapGet("/users/{id}", async (int id) => Results.Ok($"User {id}"));
///     }
/// }
/// </code>
/// </example>
// ReSharper disable once UnusedTypeParameter
public interface IEndpoint<TRouteGroup> : IEndpoint where TRouteGroup : IRouteGroup, new();

/// <summary>
///    Defines the contract for creating route groups in ASP.NET Core minimal APIs.
/// </summary>
/// <remarks>
///    <para>
///       Implement this interface to define a route group that can contain multiple endpoints.
///       Route groups provide a way to apply common configuration (prefixes, middleware, metadata)
///       to a collection of related endpoints.
///    </para>
///    <para>
///       <strong>Route group features:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>Route prefixes:</strong> Define common URL prefixes for all endpoints in the group
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Middleware application:</strong> Apply middleware to all endpoints in the group
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Metadata configuration:</strong> Set tags, authorization policies, CORS, etc.
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Hierarchical organization:</strong> Groups can be nested using <see cref="IRouteGroup{TRouteGroup}"/>
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Basic route group:</strong>
///    <code>
/// public class ApiGroup : IRouteGroup
/// {
///     public RouteGroupBuilder Group(IEndpointRouteBuilder builder)
///     {
///         return builder.MapGroup("/api")
///             .WithTags("API")
///             .RequireAuthorization();
///     }
/// }
/// </code>
/// </example>
public interface IRouteGroup
{
	/// <summary>
	///    Creates and configures a route group.
	/// </summary>
	/// <param name="builder">
	///    The <see cref="IEndpointRouteBuilder"/> used to create the route group.
	/// </param>
	/// <returns>
	///    A <see cref="RouteGroupBuilder"/> that can be used to configure the group
	///    or register endpoints within it.
	/// </returns>
	/// <remarks>
	///    Implement this method to define the route prefix and configuration for your group.
	///    The returned <see cref="RouteGroupBuilder"/> can be further configured with
	///    middleware, metadata, filters, etc.
	/// </remarks>
	RouteGroupBuilder Group(IEndpointRouteBuilder builder);
}

/// <summary>
///    Defines the contract for creating hierarchical (nested) route groups in ASP.NET Core minimal APIs.
/// </summary>
/// <typeparam name="TRouteGroup">
///    The type of the parent route group that implements <see cref="IRouteGroup"/>.
///    This establishes a parent-child relationship between route groups.
/// </typeparam>
/// <remarks>
///    <para>
///       Implement this interface to create route groups that are nested within other route groups.
///       This enables building complex route hierarchies where groups inherit prefixes and
///       configuration from their parent groups.
///    </para>
///    <para>
///       <strong>Hierarchical structure benefits:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>
///             <strong>URL composition:</strong> Child routes combine parent and child prefixes (e.g., /api/v1/users)
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Configuration inheritance:</strong> Child groups inherit parent middleware and metadata
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Logical organization:</strong> Model complex API structures with nested resources
///          </description>
///       </item>
///       <item>
///          <description>
///             <strong>Versioning support:</strong> Create version-specific groups within a base API group
///          </description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Nested route groups:</strong>
///    <code>
/// public class ApiGroup : IRouteGroup
/// {
///     public RouteGroupBuilder Group(IEndpointRouteBuilder builder)
///     {
///         return builder.MapGroup("/api")
///             .WithTags("API");
///     }
/// }
/// 
/// public class V1Group : IRouteGroup&lt;ApiGroup&gt;
/// {
///     public RouteGroupBuilder Group(IEndpointRouteBuilder builder)
///     {
///         // This will create /api/v1
///         return builder.MapGroup("/v1")
///             .WithTags("Version 1");
///     }
/// }
/// 
/// public class UsersEndpoints : IEndpoint&lt;V1Group&gt;
/// {
///     public void MapEndpoint(IEndpointRouteBuilder builder)
///     {
///         // This will create /api/v1/users
///         builder.MapGet("/users", async () => Results.Ok(new[] { "User1", "User2" }));
///     }
/// }
/// </code>
/// </example>
// ReSharper disable once UnusedTypeParameter
public interface IRouteGroup<TRouteGroup> : IRouteGroup where TRouteGroup : IRouteGroup, new();

/// <summary>
///    Provides extension methods for automatic endpoint discovery and registration.
/// </summary>
/// <remarks>
///    <para>
///       This class enables convention-based endpoint registration by scanning assemblies
///       for types that implement <see cref="IEndpoint"/> or <see cref="IRouteGroup"/> interfaces.
///       It automatically handles route group hierarchies and ensures proper registration order.
///    </para>
///    <para>
///       <strong>Registration process:</strong>
///    </para>
///    <list type="number">
///       <item>
///          <description>Validates that types don't implement conflicting interfaces</description>
///       </item>
///       <item>
///          <description>Registers endpoints without groups directly on the builder</description>
///       </item>
///       <item>
///          <description>Discovers all route groups and builds the hierarchy</description>
///       </item>
///       <item>
///          <description>Registers route groups in hierarchical order (parent before child)</description>
///       </item>
///       <item>
///          <description>Registers endpoints within their respective groups</description>
///       </item>
///    </list>
///    <para>
///       <strong>Requirements:</strong>
///    </para>
///    <list type="bullet">
///       <item>
///          <description>All endpoint and group types must be non-abstract, non-interface classes</description>
///       </item>
///       <item>
///          <description>All types must have a public parameterless constructor</description>
///       </item>
///       <item>
///          <description>Types can only implement one of: IEndpoint, IEndpoint&lt;T&gt;, IRouteGroup, IRouteGroup&lt;T&gt;</description>
///       </item>
///    </list>
/// </remarks>
/// <example>
///    <strong>Usage in Program.cs:</strong>
///    <code>
/// var builder = WebApplication.CreateBuilder(args);
/// var app = builder.Build();
/// 
/// // Automatically discover and register all endpoints from the current assembly
/// app.MapEndpointsForAssembly(typeof(Program).Assembly);
/// 
/// app.Run();
/// </code>
///    <strong>Complete example with groups:</strong>
///    <code>
/// // Define route groups
/// public class ApiGroup : IRouteGroup
/// {
///     public RouteGroupBuilder Group(IEndpointRouteBuilder builder)
///         => builder.MapGroup("/api");
/// }
/// 
/// public class V1Group : IRouteGroup&lt;ApiGroup&gt;
/// {
///     public RouteGroupBuilder Group(IEndpointRouteBuilder builder)
///         => builder.MapGroup("/v1");
/// }
/// 
/// // Define endpoints
/// public class UserEndpoints : IEndpoint&lt;V1Group&gt;
/// {
///     public void MapEndpoint(IEndpointRouteBuilder builder)
///     {
///         builder.MapGet("/users", () => Results.Ok(new[] { "User1", "User2" }));
///     }
/// }
/// 
/// public class ProductEndpoints : IEndpoint&lt;V1Group&gt;
/// {
///     public void MapEndpoint(IEndpointRouteBuilder builder)
///     {
///         builder.MapGet("/products", () => Results.Ok(new[] { "Product1", "Product2" }));
///     }
/// }
/// 
/// // In Program.cs
/// app.MapEndpointsForAssembly(typeof(Program).Assembly);
/// // Result: /api/v1/users and /api/v1/products are registered
/// </code>
/// </example>
public static class EndpointsExtensions
{
	/// <summary>
	///    Discovers and registers all endpoints and route groups from the specified assembly.
	/// </summary>
	/// <param name="builder">
	///    The <see cref="IEndpointRouteBuilder"/> to register endpoints on.
	/// </param>
	/// <param name="assembly">
	///    The assembly to scan for endpoint and route group implementations.
	/// </param>
	/// <exception cref="InvalidOperationException">
	///    Thrown when:
	///    <list type="bullet">
	///       <item>
	///          <description>A type implements more than one endpoint or group interface</description>
	///       </item>
	///       <item>
	///          <description>A type doesn't have a parameterless constructor</description>
	///       </item>
	///    </list>
	/// </exception>
	/// <remarks>
	///    <para>
	///       This method performs the following steps:
	///    </para>
	///    <list type="number">
	///       <item>
	///          <description>
	///             <strong>Validation:</strong> Ensures no type implements conflicting interfaces
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Ungrouped endpoints:</strong> Registers endpoints that don't belong to any group
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Group discovery:</strong> Finds all route groups and builds parent-child relationships
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Hierarchical registration:</strong> Registers groups from root to leaf
	///          </description>
	///       </item>
	///       <item>
	///          <description>
	///             <strong>Grouped endpoints:</strong> Registers endpoints within their respective groups
	///          </description>
	///       </item>
	///    </list>
	///    <para>
	///       <strong>Performance consideration:</strong> This method uses reflection to scan the assembly.
	///       It's designed to be called once during application startup.
	///    </para>
	/// </remarks>
	/// <example>
	///    <strong>Basic usage:</strong>
	///    <code>
	/// var app = builder.Build();
	/// app.MapEndpointsForAssembly(typeof(Program).Assembly);
	/// </code>
	///    <strong>Multiple assemblies:</strong>
	///    <code>
	/// app.MapEndpointsForAssembly(typeof(Program).Assembly);
	/// app.MapEndpointsForAssembly(typeof(ExternalModule).Assembly);
	/// </code>
	/// </example>
	public static void MapEndpointsForAssembly(this IEndpointRouteBuilder builder, Assembly assembly)
	{
		// Compruebo que no haya endpoints o grupos mal configurados
		foreach (var res in assembly.GetTypes()
			.Where(t => t is { IsInterface: false, IsAbstract: false })
			.Select(t =>
			{
				var isEndpoint = t.GetInterfaces().Any(i => i == typeof(IEndpoint));
				var isEndpointGroup = t.GetInterfaces().Any(i => i.IsSubclassOfGenericDefinition(typeof(IEndpoint<>)));
				var isGroup = t.GetInterfaces().Any(i => i == typeof(IRouteGroup));
				var isGroupWithParent = t.GetInterfaces().Any(i => i.IsSubclassOfGenericDefinition(typeof(IRouteGroup<>)));
				List<bool> res = [isEndpoint, isEndpointGroup, isGroup, isGroupWithParent];
				return (Type: t, Count: res.Count(r => r));
			}))
			if (res.Count > 2)
				throw new InvalidOperationException($"The type '{res.Type.GetSignature()}' can't implement more than one of IEndpoint, IEndpoint<TRouteGroup>, IRouteGroup, IRouteGroup<TRouteGroup> at the same time");

		// Primero busco los endpoints sin grupo
		var endpoints = assembly.GetTypes()
			.Where(t => t is { IsInterface: false, IsAbstract: false })
			.Where(t => t.GetInterfaces().Any(i => i == typeof(IEndpoint))
				&& !t.GetInterfaces().Any(i => i.IsSubclassOfGenericDefinition(typeof(IEndpoint<>))))
			.Select(t =>
			{
				if (t.GetConstructors().Any(c => c.GetParameters().Length != 0))
					throw new InvalidOperationException($"The type '{t.GetSignature()}' only can has one parameterless constructor");
				var endpoint = (IEndpoint)Activator.CreateInstance(t)!;
				return endpoint;
			})
			.ToList();
		// Registro los endpoints sin grupo
		foreach (var endpoint in endpoints) endpoint.MapEndpoint(builder);

		// Busco los grupos
		var allGroups = assembly.GetTypes()
			.Where(t => t is { IsInterface: false, IsAbstract: false })
			.Where(t => t.GetInterfaces().Any(i => i == typeof(IRouteGroup)))
			.Select(t =>
			{
				if (t.GetConstructors().Any(c => c.GetParameters().Length != 0))
					throw new InvalidOperationException($"The type '{t.GetSignature()}' only can has one parameterless constructor");
				var group = (IRouteGroup)Activator.CreateInstance(t)!;
				Type? parentGroupType = null;
				IRouteGroup? parentGroup = null;
				if (t.GetInterfaces().Any(i => i.IsSubclassOfGenericDefinition(typeof(IRouteGroup<>))))
				{
					parentGroupType = t.GetInterfaces()
						.First(i => i.IsSubclassOfGenericDefinition(typeof(IRouteGroup<>)))
						.GetGenericArguments()[0];
					parentGroup = (IRouteGroup)Activator.CreateInstance(parentGroupType)!;
				}

				return new MapEndpointsForAssemblyData
				{
					GroupType = t,
					Group = group,
					ParentGroupType = parentGroupType,
					ParentGroup = parentGroup
				};
			})
			.ToList();

		// Registro los grupos con su jerarquía
		var currentGroups = allGroups.Where(g => g.ParentGroup == null).ToList();
		while (currentGroups.Count != 0)
		{
			var data = currentGroups.First();
			data.Builder = data.Group.Group(data.ParentGroup != null
				? allGroups.First(g => g.GroupType == data.ParentGroupType).Builder!
				: builder);
			currentGroups.Remove(data);
			currentGroups.AddRange(allGroups.Where(g => g.ParentGroupType == data.GroupType));
		}

		// Busco los endpoints con grupo
		var endpointsWithGroups = assembly.GetTypes()
			.Where(t => t is { IsInterface: false, IsAbstract: false })
			.Where(t => t.GetInterfaces().Any(i => i.IsSubclassOfGenericDefinition(typeof(IEndpoint<>))))
			.Select(t =>
			{
				if (t.GetConstructors().Any(c => c.GetParameters().Length != 0))
					throw new InvalidOperationException($"The type '{t.GetSignature()}' only can has one parameterless constructor");
				var endpoint = (IEndpoint)Activator.CreateInstance(t)!;
				return endpoint;
			})
			.ToList();

		// Registro los endpoints con grupo
		foreach (var endpoint in endpointsWithGroups)
		{
			var groupType = endpoint.GetType()
				.GetInterfaces().First(i => i.IsSubclassOfGenericDefinition(typeof(IEndpoint<>)))
				.GetGenericArguments()[0];
			endpoint.MapEndpoint(allGroups.First(g => g.GroupType == groupType).Builder!);
		}
	}
}

/// <summary>
///    Internal data structure used to track route group hierarchies during endpoint registration.
/// </summary>
/// <remarks>
///    This class is used internally by <see cref="EndpointsExtensions.MapEndpointsForAssembly"/>
///    to maintain information about route groups and their relationships during the registration process.
/// </remarks>
file class MapEndpointsForAssemblyData
{
	/// <summary>
	///    Gets or sets the CLR type of the route group.
	/// </summary>
	public required Type GroupType { get; set; }
	
	/// <summary>
	///    Gets or sets the route group instance.
	/// </summary>
	public required IRouteGroup Group { get; set; }
	
	/// <summary>
	///    Gets or sets the CLR type of the parent route group, if this is a nested group.
	/// </summary>
	/// <value>
	///    The parent group type, or <c>null</c> if this is a root-level group.
	/// </value>
	public Type? ParentGroupType { get; set; }
	
	/// <summary>
	///    Gets or sets the parent route group instance, if this is a nested group.
	/// </summary>
	/// <value>
	///    The parent group instance, or <c>null</c> if this is a root-level group.
	/// </value>
	public IRouteGroup? ParentGroup { get; set; }
	
	/// <summary>
	///    Gets or sets the configured route group builder after registration.
	/// </summary>
	/// <value>
	///    The <see cref="RouteGroupBuilder"/> created when the group was registered,
	///    or <c>null</c> if the group hasn't been registered yet.
	/// </value>
	public RouteGroupBuilder? Builder { get; set; }
}