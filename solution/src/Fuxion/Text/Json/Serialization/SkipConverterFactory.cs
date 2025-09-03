using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Linq.Expressions;

public class SkipConverterFactory<T>(T converter, int skipCount = 1) : JsonConverterFactory
	where T : JsonConverter
{
	public int Counter { get; private set; } = 0;
	public int SkipCount => skipCount;
	public T Converter => converter;
	public override bool CanConvert(Type typeToConvert)
	{
		if (Counter >= skipCount)
		{
			if (Converter is JsonConverterFactory fac)
			{
				if (fac.CanConvert(typeToConvert))
				{
					return true;
				}
			}
			else
			{
				return true;
			}
		}
		Counter++;
		return false;
	}

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		=> Converter is JsonConverterFactory fac
			? fac.CreateConverter(typeToConvert, options)
			: Converter;
}
public static class SkipConverterFactoryExtensions
{
	public static JsonSerializerOptions Skip<T>(this JsonSerializerOptions options, int skipCount = 1)
		where T : JsonConverter
	{
		var newOptions = new JsonSerializerOptions(options);

		var skipCurrents = newOptions.Converters.OfType<SkipConverterFactory<T>>().ToList();
		foreach (var skipCurrent in skipCurrents)
		{
			if (skipCurrent.Counter < skipCurrent.SkipCount) throw new JsonException($"No se puede saltar el convertidor '{skipCurrent.Converter.GetType().GetSignature()}' porque no se ha alcanzado el contador.");
			newOptions.Converters.Remove(skipCurrent);
			newOptions.Converters.Add(skipCurrent.Converter);
		}

		var currents = newOptions.Converters.OfType<T>().ToList();
		if (currents.Count == 0) throw new JsonException($"No se puede saltar el convertidor '{typeof(T).GetSignature()}' porque no se ha encontrado.");
		if (currents.Count > 1) throw new JsonException($"No se puede saltar el convertidor '{typeof(T).GetSignature()}' porque se han encontrado varios.");
		var converter = currents.First();
		newOptions.Converters.Remove(converter);

		newOptions.Converters.Add(new SkipConverterFactory<T>(converter, skipCount));
		return newOptions;
	}
}