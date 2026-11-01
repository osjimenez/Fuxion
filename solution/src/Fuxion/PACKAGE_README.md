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

**🔨 Work in progress..**