<br/><br/>
<p align="center">
  <image src="https://raw.githubusercontent.com/osjimenez/Fuxion/refs/heads/main/res/logo/Assets/full_light.svg" alt="Fuxion logo" width="300px">
</p>
<br/><br/>

# Fuxion Core Library

Fuxion is a comprehensive .NET library providing essential utilities, extensions, and patterns for modern application development.

## 🚀 Installation

```bash
dotnet add package Fuxion
```

## ✨ Features

### 🔍 **Built-in Roslyn Analyzers**

Fuxion includes compile-time safety checks that help catch bugs before runtime:

- **FX001**: Detects unsafe access to members marked with `[RequiresNotNull]`
- Automatic code fixes with `Ctrl+.`
- Zero configuration required - works out of the box

```csharp
string? nullable = GetString();
nullable.Fx.Pod.BuildUriKeyPod(resolver); // ❌ FX001 Error
nullable?.Fx.Pod.BuildUriKeyPod(resolver); // ✅ OK
```

[📚 Learn more about Fuxion Analyzers](../../docs/analyzers/README.md)

### 🧩 **Extension Methods**

Fluent extension API through the `.Fx` property:

```csharp
var result = myObject.Fx.Json.Serialize();
var bytes = myString.Fx.Encoding.ToBase64String();
```

### 📦 **Pods System**

Type-safe serialization and metadata handling.

### 🔄 **Response Pattern**

Elegant error handling with `Response<T>`:

```csharp
Response<User> GetUser(int id)
{
    if (user == null)
        return Response.NotFound("User not found");
    return Response.SuccessPayload(user);
}
```

### ⏱️ **Time Providers**

Flexible time abstractions for testing:
- `LocalMachineTimeProvider`
- `InternetTimeProvider`
- `CachedTimeProvider`
- `AverageTimeProvider`

### 🎯 And Much More...

- Semantic versioning
- Singleton pattern utilities
- Generic type extensions
- Math utilities
- Collection extensions

## 📖 Documentation

- [Analyzers Documentation](../../docs/analyzers/README.md)
- [API Reference](#) _(coming soon)_
- [Examples](#) _(coming soon)_

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](../../CONTRIBUTING.md).

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**🔨 Work in progress..**