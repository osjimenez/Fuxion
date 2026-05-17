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
/// while still allowing callers to override that section name when needed.
/// </remarks>
public static class ConfigurationExtensions
{
	extension(IConfiguration me)
	{
		/// <summary>
		/// Creates and binds an options instance using either the provided section name or the one exposed by the target type.
		/// </summary>
		/// <typeparam name="T">The options type to instantiate and bind.</typeparam>
		/// <param name="sectionName">
		/// An optional configuration section name to use instead of <see cref="ISectionNamedOptions.SectionName"/>.
		/// </param>
		/// <returns>A new instance of <typeparamref name="T"/> bound to the selected configuration section.</returns>
		/// <remarks>
		/// The target type must expose a public parameterless constructor because the method creates the instance first,
		/// reads its <see cref="ISectionNamedOptions.SectionName"/>, and then binds the corresponding configuration section
		/// unless <paramref name="sectionName"/> provides an explicit override.
		/// </remarks>
		public T GetSectionNamedOptions<T>(string? sectionName = null) where T : ISectionNamedOptions, new()
		{
			var t = new T();
			me.GetSection(sectionName ?? t.SectionName).Bind(t);
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
		/// Each matching type is registered through <c>AddSectionNamedOptions(Type, IConfiguration, string?)</c>
		/// using the section name declared by the options type.
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
		/// Registers an options type in the service collection using either the provided section name or the one declared by the type itself.
		/// </summary>
		/// <typeparam name="T">The options type to register.</typeparam>
		/// <param name="config">The configuration source used to obtain the target section.</param>
		/// <param name="sectionName">
		/// An optional configuration section name to use instead of <see cref="ISectionNamedOptions.SectionName"/>.
		/// </param>
		public void AddSectionNamedOptions<T>(IConfiguration config, string? sectionName = null)
			where T : ISectionNamedOptions
			=> me.AddSectionNamedOptions(typeof(T), config, sectionName);

		/// <summary>
		/// Registers an options type in the service collection using either the provided section name or the one declared by the type itself.
		/// </summary>
		/// <param name="type">The concrete options type to register.</param>
		/// <param name="config">The configuration source used to obtain the target section.</param>
		/// <param name="sectionName">
		/// An optional configuration section name to use instead of the value exposed by <see cref="ISectionNamedOptions.SectionName"/>.
		/// </param>
		/// <exception cref="InvalidProgramException">
		/// Thrown when <paramref name="type"/> cannot be instantiated as an <see cref="ISectionNamedOptions"/>.
		/// </exception>
		/// <remarks>
		/// This method creates an instance of the supplied type to read <see cref="ISectionNamedOptions.SectionName"/>,
		/// resolves the section indicated by <paramref name="sectionName"/> or, when it is <see langword="null"/>,
		/// the section declared by the type itself, and then invokes the corresponding generic options registration API.
		/// </remarks>
		public void AddSectionNamedOptions(Type type, IConfiguration config, string? sectionName = null)
		{
			if (Activator.CreateInstance(type) is not ISectionNamedOptions options)
				throw new InvalidProgramException($"El tipo '{type.Name}' debe implementar '{nameof(ISectionNamedOptions)}'");

			var section = config.GetSection(sectionName ?? options.SectionName);

			var genericConfigure = ConfigureMethod.MakeGenericMethod(type);
			genericConfigure.Invoke(null, [me, null, section]);
		}
	}
}