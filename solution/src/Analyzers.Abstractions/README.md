# Fuxion.Analyzers.Abstractions

Attribute definitions for Fuxion Roslyn analyzers.

## ?? Installation

```bash
dotnet add package Fuxion.Analyzers.Abstractions
```

## ?? What's Included

This package contains only the **attribute definitions** used by Fuxion analyzers:

- `[RequiresNotNull]` - Marks properties/methods that throw when accessed with null
- `[HasMembersRequiringNotNull]` - Marks types containing members that require non-null (workaround for extension members)

## ?? Usage Scenarios

### **Scenario 1: Using with Fuxion (Automatic)**

If you install `Fuxion`, you automatically get this package transitively:

```xml
<PackageReference Include="Fuxion" Version="1.0.0" />
<!-- Fuxion.Analyzers.Abstractions is included automatically -->
```

### **Scenario 2: Using Analyzers Only**

If you want to use Fuxion analyzers **without** the full Fuxion runtime:

```xml
<PackageReference Include="Fuxion.Analyzers" Version="1.0.0" />
<!-- Fuxion.Analyzers.Abstractions is included transitively -->
```

You get:
- ? Analyzer attributes (this package)
- ? Compile-time checks (Fuxion.Analyzers)
- ? NO Fuxion runtime (~500KB saved)

### **Scenario 3: Your Own Library**

Mark your own members with null-safety attributes:

```csharp
using Fuxion.Analyzers;

public class MyClass
{
    private string? _value;
    
    [RequiresNotNull]
    public string Value 
    { 
        get => _value ?? throw new ArgumentNullException();
        set => _value = value;
    }
}
```

Users of your library will get FX001 warnings if they have `Fuxion.Analyzers` installed.

## ?? Attributes

### `[RequiresNotNull]`

Indicates a member throws `ArgumentNullException` when accessed with null.

**Targets:** Properties, Methods

**Example:**
```csharp
[RequiresNotNull]
public string Name { get; set; }
```

### `[HasMembersRequiringNotNull]`

Indicates a type contains members that require non-null (workaround for C# 14 extension members).

**Targets:** Classes, Structs

**Example:**
```csharp
[HasMembersRequiringNotNull("Pod", "Json")]
public class FuxionExtensions<T> where T : notnull
{
    public T Value { get; }
}
```

## ?? Related Packages

- [`Fuxion`](https://www.nuget.org/packages/Fuxion) - Full runtime library (includes this package)
- [`Fuxion.Analyzers`](https://www.nuget.org/packages/Fuxion.Analyzers) - Roslyn analyzers (references this package)

## ?? Documentation

- [Analyzer Documentation](../../docs/analyzers/README.md)
- [FX001 Rule](../../docs/analyzers/FX001.md)

## ?? Feedback

[Open an issue](https://github.com/osjimenez/Fuxion/issues/new?labels=analyzer) for bugs or suggestions.
