using Fuxion.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Fuxion.Configuration;

/// <summary>
/// Provides helper extensions for binding and registering options types that implement <see cref="ISectionNamedOptions"/>.
/// </summary>
/// <remarks>
/// These extensions allow an options type to define its own configuration section name through <see cref="ISectionNamedOptions.SectionName"/>,
/// avoiding the need to repeat that section name at each binding or registration site.
/// </remarks>
public static class ConfigurationExtensions
{
	extension(IConfiguration me)
	{
		/// <summary>
		/// Creates and binds an options instance using the section name exposed by the target type.
		/// </summary>
		/// <typeparam name="T">The options type to instantiate and bind.</typeparam>
		/// <returns>A new instance of <typeparamref name="T"/> bound to its configured section.</returns>
		/// <remarks>
		/// The target type must expose a public parameterless constructor because the method creates the instance first,
		/// reads its <see cref="ISectionNamedOptions.SectionName"/>, and then binds the corresponding configuration section.
		/// </remarks>
		public T GetSectionNamedOptions<T>() where T : ISectionNamedOptions, new()
		{
			var t = new T();
			me.GetSection(t.SectionName).Bind(t);
			return t;
		}
	}

	private static readonly MethodInfo ConfigureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
		.GetMethods(BindingFlags.Public | BindingFlags.Static)
		.Where(m => m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure))
		.Where(m => m.IsGenericMethodDefinition)
		.Select(m => new
		{
			Method = m,
			Params = m.GetParameters()
		})
		.Where(x => x.Params.Length == 3)
		.Where(x => x.Params[0].ParameterType == typeof(IServiceCollection))
		.Where(x => x.Params[1].ParameterType == typeof(string))
		.Where(x => typeof(IConfiguration).IsAssignableFrom(x.Params[2].ParameterType))
		.Select(x => x.Method)
		.Single();

	extension(IServiceCollection me)
	{

		/// <summary>
		/// Scans an assembly for concrete options types implementing <see cref="ISectionNamedOptions"/> and registers them in the service collection.
		/// </summary>
		/// <param name="assembly">The assembly to scan for eligible options types.</param>
		/// <param name="config">The configuration root used to resolve each declared section.</param>
		/// <remarks>
		/// Only non-abstract classes with a public parameterless constructor are considered.
		/// Each matching type is registered through AddSectionNamedOptions(Type, IConfiguration)/>.
		/// </remarks>
		public void AddSectionNamedOptionsFromAssembly(Assembly assembly, IConfigurationRoot config)
		{
			foreach (var type in assembly.GetTypes()
							.Where(t => t.IsClass && !t.IsAbstract)
							.Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
							.Where(t => typeof(ISectionNamedOptions).IsAssignableFrom(t)))
				me.AddSectionNamedOptions(type, config);
		}

		/// <summary>
		/// Registers an options type in the service collection using the section name declared by the type itself.
		/// </summary>
		/// <typeparam name="T">The options type to register.</typeparam>
		/// <param name="config">The configuration source used to obtain the target section.</param>
		public void AddSectionNamedOptions<T>(IConfiguration config)
			where T : ISectionNamedOptions
		{
			me.AddSectionNamedOptions(typeof(T), config);
		}

		/// <summary>
		/// Registers an options type in the service collection using the section name declared by the type itself.
		/// </summary>
		/// <param name="type">The concrete options type to register.</param>
		/// <param name="config">The configuration source used to obtain the target section.</param>
		/// <exception cref="InvalidProgramException">
		/// Thrown when <paramref name="type"/> cannot be instantiated as an <see cref="ISectionNamedOptions"/>.
		/// </exception>
		/// <remarks>
		/// This method creates an instance of the supplied type to read <see cref="ISectionNamedOptions.SectionName"/>,
		/// resolves that section from configuration, and then invokes the corresponding generic options registration API.
		/// </remarks>
		public void AddSectionNamedOptions(Type type, IConfiguration config)
		{
			if (Activator.CreateInstance(type) is not ISectionNamedOptions options)
				throw new InvalidProgramException($"El tipo '{type.Name}' debe implementar '{nameof(ISectionNamedOptions)}'");

			var section = config.GetSection(options.SectionName);

			var genericConfigure = ConfigureMethod.MakeGenericMethod(type);
			genericConfigure.Invoke(null, [me, null, section]);
		}
	}
}