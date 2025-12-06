# Fuxion Analyzers

Fuxion includes built-in Roslyn analyzers that provide compile-time safety checks for your code.

## ?? Installation

Analyzers are **automatically included** when you install the Fuxion package:

```xml
<PackageReference Include="Fuxion" Version="1.0.0" />
```

That's it! No additional packages needed. The analyzers are loaded automatically by Visual Studio.

## ?? Available Analyzers

### FX001: RequiresNotNull Violation

**Rule ID:** `FX001`  
**Category:** Safety  
**Severity:** Error

Detects when you try to access a member marked with `[RequiresNotNull]` on a nullable type without using the null-conditional operator (`?.`).

#### ? **Incorrect Usage:**

```csharp
string? nullableStr = GetNullableString();
nullableStr.Fx.Pod.BuildUriKeyPod(resolver); // ? ERROR FX001
```

#### ? **Correct Usage:**

```csharp
string? nullableStr = GetNullableString();
nullableStr?.Fx.Pod.BuildUriKeyPod(resolver); // ? OK - uses ?.
```

#### ?? **Quick Fix Available:**

When you see FX001, press `Ctrl+.` (or click the lightbulb ??) to automatically add the null-conditional operator:

```
? nullableStr.Fx.Pod
   ? (press Ctrl+.)
? nullableStr?.Fx.Pod
```

## ?? How It Works

The analyzer detects members marked with `[RequiresNotNull]`:

```csharp
extension<T>(FuxionExtensions<T?> me) where T : notnull
{
    [RequiresNotNull]  // ? This attribute triggers FX001
    public PodExtensions<T> Pod
    {
        get
        {
            ArgumentNullException.ThrowIfNull(me.Value);
            return new(me.Value);
        }
    }
}
```

When you access `.Pod` on a nullable type, the analyzer reports FX001 to prevent runtime `ArgumentNullException`.

## ?? Configuration

### Disable Specific Warnings

If you need to suppress FX001 in specific cases:

```csharp
#pragma warning disable FX001
var result = nullable.Fx.Pod; // Warning suppressed
#pragma warning restore FX001
```

### Disable All Analyzer Warnings

In your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);FX001</NoWarn>
</PropertyGroup>
```

### Completely Disable Analyzers

Not recommended, but if you need to:

```xml
<ItemGroup>
  <Analyzer Remove="Fuxion.Analyzers.dll" />
  <Analyzer Remove="Fuxion.Analyzers.CodeFixes.dll" />
</ItemGroup>
```

## ?? See Also

- [FX001 Documentation](FX001.md)
- [Debugging Analyzers](debugging-analyzers.md)
- [Contributing Analyzers](../../CONTRIBUTING.md#analyzers)

## ?? Feedback

If you find false positives or have suggestions for new analyzers, please [open an issue](https://github.com/osjimenez/Fuxion/issues/new?labels=analyzer).
