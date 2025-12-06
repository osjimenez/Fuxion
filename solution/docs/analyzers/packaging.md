# Packaging Fuxion with Analyzers

This document explains how Fuxion packages its Roslyn analyzers into the main NuGet package.

## ?? Package Structure

When you run `dotnet pack` on the Fuxion project, the resulting NuGet package includes:

```
Fuxion.1.0.0.nupkg
??? lib/
?   ??? netstandard2.0/
?   ?   ??? Fuxion.dll                    ? Runtime library
?   ??? net472/
?   ?   ??? Fuxion.dll
?   ??? net8.0/
?   ?   ??? Fuxion.dll
?   ??? net9.0/
?   ?   ??? Fuxion.dll
?   ??? net10.0/
?       ??? Fuxion.dll
??? analyzers/
    ??? dotnet/
        ??? cs/
            ??? Fuxion.Analyzers.dll       ? Roslyn analyzer
            ??? Fuxion.Analyzers.CodeFixes.dll ? Code fix provider
```

## ?? How It Works

### 1. Centralized Configuration

All analyzer paths are defined in `src/Directory.Build.props` for easy maintenance:

```xml
<PropertyGroup>
  <!-- Base directories -->
  <FuxionAnalyzersDir>$(MSBuildThisFileDirectory)Analyzers\</FuxionAnalyzersDir>
  <FuxionAnalyzersCodeFixesDir>$(MSBuildThisFileDirectory)Analyzers.CodeFixes\</FuxionAnalyzersCodeFixesDir>
  
  <!-- Project files -->
  <FuxionAnalyzersProject>$(FuxionAnalyzersDir)Fuxion.Analyzers.csproj</FuxionAnalyzersProject>
  <FuxionAnalyzersCodeFixesProject>$(FuxionAnalyzersCodeFixesDir)Fuxion.Analyzers.CodeFixes.csproj</FuxionAnalyzersCodeFixesProject>
  
  <!-- Output DLLs -->
  <FuxionAnalyzersDll>$(FuxionAnalyzersDir)bin\$(Configuration)\netstandard2.0\Fuxion.Analyzers.dll</FuxionAnalyzersDll>
  <FuxionAnalyzersCodeFixesDll>$(FuxionAnalyzersCodeFixesDir)bin\$(Configuration)\netstandard2.0\Fuxion.Analyzers.CodeFixes.dll</FuxionAnalyzersCodeFixesDll>
  
  <!-- NuGet package path -->
  <AnalyzersPackagePath>analyzers/dotnet/cs</AnalyzersPackagePath>
</PropertyGroup>
```

**Benefits:**
- ? Single source of truth
- ? Easy to update paths
- ? Used across all projects
- ? No hardcoded relative paths

### 2. Build Order

The `Fuxion.csproj` uses these variables in a custom MSBuild target:

```xml
<Target Name="BuildAnalyzersBeforePack" BeforeTargets="GenerateNuspec">
  <Message Text="Building Fuxion.Analyzers from: $(FuxionAnalyzersProject)" Importance="high" />
  <MSBuild Projects="$(FuxionAnalyzersProject)" 
           Targets="Build" 
           Properties="Configuration=$(Configuration)" />
  
  <Message Text="Building Fuxion.Analyzers.CodeFixes from: $(FuxionAnalyzersCodeFixesProject)" Importance="high" />
  <MSBuild Projects="$(FuxionAnalyzersCodeFixesProject)" 
           Targets="Build" 
           Properties="Configuration=$(Configuration)" />
</Target>
```

### 3. Including Analyzer DLLs

Using the centralized variables:

```xml
<ItemGroup>
  <!-- Pack Analyzer DLL -->
  <None Include="$(FuxionAnalyzersDll)" 
        Pack="true" 
        PackagePath="$(AnalyzersPackagePath)" 
        Visible="false" 
        Condition="Exists('$(FuxionAnalyzersDll)')" />
  
  <!-- Pack CodeFix DLL -->
  <None Include="$(FuxionAnalyzersCodeFixesDll)" 
        Pack="true" 
        PackagePath="$(AnalyzersPackagePath)" 
        Visible="false" 
        Condition="Exists('$(FuxionAnalyzersCodeFixesDll)')" />
</ItemGroup>
```

### 4. Automatic Loading

When a project references the Fuxion NuGet package:

1. **NuGet restore** extracts the package
2. **MSBuild** detects DLLs in `analyzers/dotnet/cs/`
3. **Visual Studio** automatically loads them as Roslyn analyzers
4. **IntelliSense** shows diagnostics and code fixes in real-time

## ?? Updating Analyzer Paths

If you need to change analyzer locations, **edit only one file**:

`src/Directory.Build.props`:

```xml
<PropertyGroup>
  <!-- Example: Move analyzers to a different folder -->
  <FuxionAnalyzersDir>$(MSBuildThisFileDirectory)RoslynAnalyzers\</FuxionAnalyzersDir>
  <FuxionAnalyzersCodeFixesDir>$(MSBuildThisFileDirectory)RoslynCodeFixes\</FuxionAnalyzersCodeFixesDir>
  
  <!-- All projects using these variables will automatically update -->
</PropertyGroup>
```

All projects that reference these variables will automatically use the new paths.

## ?? Creating the Package

### Development Build

```bash
cd src/Fuxion
dotnet pack -c Debug
```

Output: `bin/Debug/Fuxion.{version}.nupkg`

### Release Build

```bash
cd src/Fuxion
dotnet pack -c Release
```

Output: `bin/Release/Fuxion.{version}.nupkg`

### Inspect the Package

```bash
# Extract the .nupkg (it's just a ZIP)
unzip Fuxion.1.0.0.nupkg -d temp/

# Verify analyzer DLLs are included
ls temp/analyzers/dotnet/cs/
# Should show:
# - Fuxion.Analyzers.dll
# - Fuxion.Analyzers.CodeFixes.dll
```

## ?? Why This Approach?

### ? Advantages

1. **One package to install** - Users don't need separate analyzer packages
2. **Always in sync** - Analyzers match the runtime library version
3. **Zero configuration** - Works automatically, no setup required
4. **Transparent** - Users don't even know analyzers are there
5. **Centralized paths** - Easy maintenance through `Directory.Build.props`
6. **Reusable** - Other projects can use the same variables

### ?? Considerations

1. **Package size** - Adds ~100KB (analyzers + codefixes)
2. **Can't opt-out easily** - Analyzers are always loaded
3. **Build order dependency** - Must build analyzers before Fuxion

## ?? Troubleshooting

### Analyzers Not Loading in Visual Studio

1. **Clean and rebuild**:
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Restart Visual Studio** (analyzers are cached)

3. **Check package contents**:
   ```bash
   unzip -l Fuxion.*.nupkg | grep analyzers
   ```

4. **Clear NuGet cache**:
   ```bash
   dotnet nuget locals all --clear
   ```

### Build Fails: Analyzer DLLs Not Found

Ensure analyzers are built before packing:

```bash
# Build analyzers first
cd src/Analyzers
dotnet build -c Release

cd ../Analyzers.CodeFixes
dotnet build -c Release

# Then build/pack Fuxion
cd ../Fuxion
dotnet pack -c Release
```

Or use the solution-level build (recommended):

```bash
cd solution
dotnet build -c Release
cd src/Fuxion
dotnet pack -c Release
```

### Variable Not Defined Error

If you get errors about undefined variables like `$(FuxionAnalyzersProject)`, ensure:

1. The project imports `Directory.Build.props`:
   ```xml
   <Project>
     <Import Project="..\Directory.Build.props" />
     <!-- Your project content -->
   </Project>
   ```

2. The project is in the `src/` folder hierarchy

3. Run `dotnet clean` and rebuild

## ?? References

- [Creating NuGet Packages with Analyzers](https://docs.microsoft.com/en-us/nuget/guides/analyzers-conventions)
- [Roslyn Analyzer Deployment](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md)
- [MSBuild Custom Targets](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets)
- [Directory.Build.props](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)

## ?? Alternative: Separate Package

If you want to keep analyzers separate, you can still publish `Fuxion.Analyzers` as a standalone package.

Users would then install:

```xml
<PackageReference Include="Fuxion" Version="1.0.0" />
<PackageReference Include="Fuxion.Analyzers" Version="1.0.0" /> <!-- Optional -->
```

To enable this:

1. Keep `Fuxion.Analyzers.csproj` with `<IsPackable>true</IsPackable>`
2. Remove analyzer packaging from `Fuxion.csproj`
3. Document both installation options

---

**Current Strategy:** Analyzers included in main package (transparent) with centralized configuration  
**Alternative Strategy:** Separate analyzer package (opt-in)
