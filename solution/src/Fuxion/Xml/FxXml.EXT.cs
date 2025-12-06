using Fuxion.Text.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Fuxion.Xml;

/// <summary>
/// Provides extension methods for XML serialization and deserialization operations.
/// /// </summary>
public static class XElementExtensions
{
	extension<T>(FuxionExtensions<T?> me)
	{
		/// <summary>
		/// Provides XML serialization and deserialization extension operations for this value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property enables fluent XML operations through the <c>.Fx.Xml</c> syntax,
		/// following the Fuxion extension pattern for organized API surface.
		/// </para>
		/// <para>
		/// Use this to access XML-specific operations for serialization and deserialization.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// var person = new Person { Name = "John", Age = 30 };
		/// 
		/// // Serialize to XElement
		/// XElement xml = person.Fx.Xml.ToXElement();
		/// 
		/// // Deserialize from XElement
		/// Person restored = xml.Fx.Xml.FromXElement&lt;Person&gt;();
		/// </code>
		/// </example>
		public XmlExtensions<T?> Xml
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(me.Value);
		}
	}

	extension<T>(XmlExtensions<T> me)
	{
		/// <summary>
		/// Converts the object to an <see cref="XElement"/> using XML serialization.
		/// </summary>
		/// <returns>
		/// An <see cref="XElement"/> representing the XML-serialized object.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method uses <see cref="XmlSerializer"/> to serialize the object into XML format.
		/// The type <typeparamref name="T"/> must be XML-serializable (have a parameterless constructor
		/// and public properties/fields).
		/// </para>
		/// <para>
		/// The resulting <see cref="XElement"/> can be manipulated using LINQ to XML, saved to files,
		/// or integrated into larger XML documents.
		/// </para>
		/// <para>
		/// <b>Performance Note:</b> XML serialization creates a new <see cref="XmlSerializer"/> instance
		/// for each call. For high-performance scenarios with repeated serialization of the same type,
		/// consider caching the serializer.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the object cannot be serialized. Common causes include:
		/// <list type="bullet">
		/// <item>The type lacks a parameterless constructor</item>
		/// <item>The type contains non-serializable members without [XmlIgnore]</item>
		/// <item>Circular references exist in the object graph</item>
		/// </list>
		/// </exception>
		/// <example>
		/// <code>
		/// // Simple object serialization
		/// var person = new Person { Name = "John", Age = 30 };
		/// XElement xml = person.Fx.Xml.ToXElement();
		/// // Result: &lt;Person&gt;&lt;Name&gt;John&lt;/Name&gt;&lt;Age&gt;30&lt;/Age&gt;&lt;/Person&gt;
		/// 
		/// // Save to file
		/// xml.Save("person.xml");
		/// 
		/// // Integrate into larger document
		/// var document = new XElement("Root",
		///     new XElement("People",
		///         person.Fx.Xml.ToXElement()));
		/// </code>
		/// </example>
		public XElement ToXElement()
		{
			using var memoryStream = new MemoryStream();
			using var streamWriter = new StreamWriter(memoryStream);
			var xmlSerializer = new XmlSerializer(typeof(T));
			xmlSerializer.Serialize(streamWriter, me.Value);
			return XElement.Parse(Encoding.UTF8.GetString(memoryStream.ToArray(), 0, (int)memoryStream.Length));
		}

		/// <summary>
		/// Converts the object to an <see cref="XElement"/> using XML serialization with an explicit type.
		/// </summary>
		/// <param name="type">
		/// The type to use for serialization. This can differ from <typeparamref name="T"/> when dealing
		/// with polymorphic scenarios or when you want to serialize as a base type.
		/// </param>
		/// <returns>
		/// An <see cref="XElement"/> representing the XML-serialized object.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This overload is useful when:
		/// <list type="bullet">
		/// <item>You need to serialize an object as its base type</item>
		/// <item>You're working with runtime-determined types</item>
		/// <item>You want to control the XML structure by specifying the serialization type</item>
		/// </list>
		/// </para>
		/// <para>
		/// The specified <paramref name="type"/> must be compatible with the actual object being serialized
		/// (i.e., the object must be assignable to the specified type).
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the object cannot be serialized with the specified type.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when the object is not compatible with the specified type.
		/// </exception>
		/// <example>
		/// <code>
		/// // Serialize derived type as base type
		/// Animal dog = new Dog { Name = "Buddy", Breed = "Labrador" };
		/// XElement xml = dog.Fx.Xml.ToXElement(typeof(Animal));
		/// // Result only includes Animal properties
		/// 
		/// // Runtime type determination
		/// Type serializationType = GetSerializationType();
		/// XElement dynamicXml = obj.Fx.Xml.ToXElement(serializationType);
		/// </code>
		/// </example>
		public XElement ToXElement(Type type)
		{
			using var memoryStream = new MemoryStream();
			using TextWriter streamWriter = new StreamWriter(memoryStream);
			var xmlSerializer = new XmlSerializer(type);
			xmlSerializer.Serialize(streamWriter, me.Value);
			return XElement.Parse(Encoding.UTF8.GetString(memoryStream.ToArray(), 0, (int)memoryStream.Length));
		}
	}

	extension<T>(XmlExtensions<XElement> me)
	{
		/// <summary>
		/// Deserializes an <see cref="XElement"/> into an object of the specified type.
		/// </summary>
		/// <returns>
		/// A new instance of the target type populated with data from the XML element.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method uses <see cref="XmlSerializer"/> to deserialize the XML element into a strongly-typed object.
		/// The XML structure must match the expected structure for the target type.
		/// </para>
		/// <para>
		/// <b>Requirements for the target type:</b>
		/// <list type="bullet">
		/// <item>Must have a public parameterless constructor</item>
		/// <item>Properties/fields to be deserialized must be public</item>
		/// <item>Complex types must also be XML-serializable</item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Thrown when the XML structure doesn't match the expected structure for the target type.
		/// </exception>
		/// <exception cref="InvalidCastException">
		/// Thrown when deserialization returns null, indicating a critical failure in the deserialization process.
		/// </exception>
		/// <example>
		/// <code>
		/// // Deserialize from XML string
		/// string xmlString = "&lt;Person&gt;&lt;Name&gt;John&lt;/Name&gt;&lt;Age&gt;30&lt;/Age&gt;&lt;/Person&gt;";
		/// XElement xml = XElement.Parse(xmlString);
		/// Person person = xml.Fx.Xml.FromXElement&lt;Person&gt;();
		/// // person.Name == "John", person.Age == 30
		/// 
		/// // Load from file and deserialize
		/// XElement fileXml = XElement.Load("person.xml");
		/// Person filePerson = fileXml.Fx.Xml.FromXElement&lt;Person&gt;();
		/// 
		/// // Round-trip serialization/deserialization
		/// var original = new Person { Name = "Alice", Age = 25 };
		/// XElement serialized = original.Fx.Xml.ToXElement();
		/// Person restored = serialized.Fx.Xml.FromXElement&lt;Person&gt;();
		/// // restored is equivalent to original
		/// </code>
		/// </example>
		public T FromXElement()
		{
			using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(me.Value.ToString().ToCharArray()));
			var xmlSerializer = new XmlSerializer(typeof(T));
			return (T)(xmlSerializer.Deserialize(memoryStream) ?? throw new InvalidCastException($"Deserialization from type '{typeof(T).Name}' failed"));
		}
	}
}

/// <summary>
/// Extension class that provides XML operations for values.
/// This class is part of the Fuxion extension pattern accessed via <c>.Fx.Xml</c>.
/// </summary>
/// <typeparam name="T">The type of the value being extended.</typeparam>
/// <remarks>
/// This class should not be instantiated directly. Use the <c>.Fx.Xml</c> syntax to access XML operations.
/// </remarks>
public class XmlExtensions<T>(T me) : Extensions<T>(me);