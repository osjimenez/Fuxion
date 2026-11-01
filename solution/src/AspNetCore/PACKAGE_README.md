<br/><br/>
<p align="center">
  <image src="https://raw.githubusercontent.com/osjimenez/Fuxion/refs/heads/main/solution/.config/full_logo.svg" alt="Fuxion logo" width="300px">
</p>
<br/><br/>

Install command:
```bash
dotnet add package Fuxion.AspNetCore
```

## Native AOT

From .NET 11 on, compatible with trimming and native AOT with minimal APIs (ASP.NET Core does not support MVC there).
Register your context before `AddResponses()`, which chains `FuxionJsonContext` after it:

```csharp
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));
builder.Services.AddResponses();
```

Map each endpoint by its type, `app.MapEndpoint<GetOrderEndpoint>()`: `MapEndpointsForAssembly` finds them by reflection.

**🔨 Work in progress..**