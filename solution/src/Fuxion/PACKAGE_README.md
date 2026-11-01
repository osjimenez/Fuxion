<br/><br/>
<p align="center">
  <image src="https://raw.githubusercontent.com/osjimenez/Fuxion/refs/heads/main/solution/.config/full_logo.svg" alt="Fuxion logo" width="300px">
</p>
<br/><br/>

Install command:
```bash
dotnet add package Fuxion
```

## Native AOT

From .NET 11 on, Fuxion is compatible with trimming and native AOT. The responses (`Response<T>`, `ResponseMaybe<T>`,
`Error`, `None`, `Unit`) read and write JSON without reflection: chain `FuxionJsonContext` after a context of your own,
which declares your types and your responses.

```csharp
[JsonSerializable(typeof(Response<Order>))]
[JsonSerializable(typeof(Order))]
partial class AppJsonContext : JsonSerializerContext;

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.TypeInfoResolverChain.Add(AppJsonContext.Default);
options.TypeInfoResolverChain.Add(FuxionJsonContext.Default);
```

What still uses reflection is marked, and warns where you use it.

## Types by their key

A `UriKey` names a type by an `https` URI that ends with its major version (`https://acme.example/erp/invoice/v1`), and a
generic type with its arguments by its key followed by theirs between brackets. It has one canonical form: anything else
is rejected. Declare the key and the exact revision of a type with `[UriKey]`, and mark a class with
`[TypeDefinitions]`: the generator of Fuxion writes there the definition of each type (its key, its revision, its arity,
its base and its interfaces) and the code that registers them, without reflection.

```csharp
[UriKey("https://acme.example/erp/invoice/v1", "1.3.0")]
public record Invoice(string Number);

[TypeDefinitions]
internal static partial class AppTypes;

var registry = new TypeRegistry();
AppTypes.Register(registry);
// https://meta.fuxion.nexus/system/list/v1[https://acme.example/erp/invoice/v1]
UriKey key = registry.GetKey<List<Invoice>>();
```

**🔨 Work in progress..**