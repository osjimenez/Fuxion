# Debugging Fuxion Analyzers

Este documento explica cómo debuguear los analyzers de Fuxion, específicamente el `RequiresNotNullAnalyzer` (FX001).

## ? Respuesta rápida a tu pregunta

**SÍ, los analyzers SE PUEDEN DEBUGUEAR** de dos formas:

1. **Con Debug.WriteLine()** - Método más simple, ya implementado ?
2. **Con Visual Studio Debugger** - Debugging paso a paso

## ?? Logging con Debug.WriteLine (Ya implementado)

He añadido logging exhaustivo al `RequiresNotNullAnalyzer.cs` con `Debug.WriteLine()`. 

### Cómo ver los logs:

1. **Compila el proyecto del analyzer**:
   ```powershell
   dotnet build src\Analyzers\Fuxion.Analyzers.csproj
   ```

2. **Abre Visual Studio**

3. **Abre la ventana de Output**:
   - Menú: `View` ? `Output` (o `Ctrl+Alt+O`)
   - En el dropdown "Show output from:", selecciona **`Analyzer Compiler`** o **`Debug`**

4. **Compila el proyecto de test**:
   ```powershell
   dotnet build test\Pods\Fuxion.Pods.Test.csproj
   ```

5. **Mira el Output** - Verás mensajes como:
   ```
   [FX001 Analyzer] ========== ANALYZER INITIALIZED ==========
   [FX001 Analyzer] ===== Analyzing member access: Value =====
   [FX001 Analyzer] Member symbol: TestClass.Value
   [FX001 Analyzer] ? Symbol is property or method
   [FX001 Analyzer] Has [RequiresNotNull] on member: True
   [FX001 Analyzer] ? Member REQUIRES non-null
   [FX001 Analyzer] Expression type: TestClass?
   [FX001 Analyzer] Is nullable type: True
   [FX001 Analyzer] ?? REPORTING DIAGNOSTIC FX001 for 'Value' on 'TestClass?'
   ```

### Lo que el logging te dirá:

El analyzer ahora imprime:
- ? Cuándo se inicializa
- ? Qué miembros está analizando
- ? Si encuentra el atributo `[RequiresNotNull]`
- ? Si el tipo es nullable
- ? Si reporta o no el diagnostic FX001
- ? Todos los atributos en los miembros y tipos contenedores
- ? Por qué NO reporta un diagnóstico (si no lo hace)

## ?? Debugging con Visual Studio Debugger

### Opción 1: Attach to Process

1. **Abre DOS instancias de Visual Studio**:
   - Instancia 1: Tu solución de Fuxion (con el analyzer)
   - Instancia 2: Un proyecto de prueba o tu solución de Fuxion

2. **En la Instancia 1** (donde está el analyzer):
   - Pon breakpoints en `RequiresNotNullAnalyzer.cs`
   - Ve a `Debug` ? `Attach to Process...`
   - Selecciona el proceso `devenv.exe` de la Instancia 2
   - Haz check en "Show processes from all users"
   - Attach

3. **En la Instancia 2**:
   - Edita un archivo `.cs` que use `[RequiresNotNull]`
   - Los breakpoints en la Instancia 1 se activarán

### Opción 2: VSIX Project (Recomendado para desarrollo continuo)

1. **Crea un proyecto VSIX** para el analyzer (si no existe):
   ```
   File ? New ? Project ? "VSIX Project"
   ```

2. **Configura el proyecto VSIX** para incluir tu analyzer

3. **Run/Debug el proyecto VSIX**:
   - Se abrirá una instancia experimental de Visual Studio
   - Tus breakpoints funcionarán automáticamente

### Opción 3: Debugging Unit Tests

Puedes crear unit tests para los analyzers usando `Microsoft.CodeAnalysis.Testing`:

```csharp
[Fact]
public async Task TestRequiresNotNull_ReportsError()
{
	var test = @"
		public class Test {
			[RequiresNotNull]
			public string Prop { get; set; }
		}
		
		public class Usage {
			void M() {
				Test? t = null;
				_ = t.Prop; // Should report FX001
			}
		}";

	await VerifyCS.VerifyAnalyzerAsync(test, 
		VerifyCS.Diagnostic(RequiresNotNullAnalyzer.Rule)
			.WithLocation(9, 9));
}
```

## ?? Diagnóstico de problemas comunes

### Problema: El analyzer no se ejecuta

**Síntomas**: No ves logs ni errores FX001

**Solución**:
1. Verifica que el analyzer esté referenciado correctamente:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\src\Analyzers\Fuxion.Analyzers.csproj">
       <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
       <OutputItemType>Analyzer</OutputItemType>
     </ProjectReference>
   </ItemGroup>
   ```

2. Limpia y reconstruye:
   ```powershell
   dotnet clean
   dotnet build -c Debug
   ```

3. Reinicia Visual Studio

4. Elimina la carpeta `.vs`:
   ```powershell
   rm -r -force .\.vs\
   ```

### Problema: No veo los logs de Debug.WriteLine

**Síntomas**: El analyzer compila pero no ves output

**Solución**:
1. Asegúrate de estar en modo Debug (no Release)
2. Ve a `Tools` ? `Options` ? `Debugging` ? `Output Window`
3. Marca "All Output Window Text" to "On"
4. Reinicia Visual Studio

### Problema: VS ignora el analyzer actualizado

**Síntomas**: Modificas el analyzer pero sigue comportándose como antes

**Solución**:
```powershell
# Mata todos los procesos de Visual Studio
taskkill /F /IM devenv.exe
taskkill /F /IM MSBuild.exe
taskkill /F /IM VBCSCompiler.exe

# Limpia
dotnet clean
rm -r -force .\.vs\
rm -r -force .\src\Analyzers\bin\
rm -r -force .\src\Analyzers\obj\

# Recompila
dotnet build src\Analyzers\Fuxion.Analyzers.csproj -c Debug

# Abre VS de nuevo
```

## ?? Interpretando el Output del Analyzer

Con el logging añadido, podrás ver exactamente:

### ? Caso exitoso (debería reportar FX001):
```
[FX001 Analyzer] ===== Analyzing member access: Value =====
[FX001 Analyzer] ? Symbol is property or method
[FX001 Analyzer] Has [RequiresNotNull] on member: True        ? Encontró el atributo
[FX001 Analyzer] ? Member REQUIRES non-null                    ? Requiere no-null
[FX001 Analyzer] Expression type: TestClass?                   ? Tipo es nullable
[FX001 Analyzer] Is nullable type: True                        ? Detectó nullable
[FX001 Analyzer] ?? REPORTING DIAGNOSTIC FX001                 ? ¡Reportó el error!
```

### ? Caso donde NO reporta (y verás por qué):
```
[FX001 Analyzer] ===== Analyzing member access: Value =====
[FX001 Analyzer] ? Symbol is property or method
[FX001 Analyzer] Has [RequiresNotNull] on member: False       ? NO encontró el atributo
[FX001 Analyzer] Has [HasMembersRequiringNotNull] on type: False
[FX001 Analyzer] All attributes on member 'Value':
[FX001 Analyzer]   - (ninguno)                                 ? No hay atributos
[FX001 Analyzer] ? Member does NOT require non-null, skipping ? Por eso no reporta
```

## ?? Próximos pasos

1. **Compila y mira el Output** - Ya tienes todo el logging
2. **Si no ves logs** - El analyzer no se está cargando (ver soluciones arriba)
3. **Si ves logs pero no reporta FX001** - Los logs te dirán exactamente por qué
4. **Si necesitas más detalle** - Usa el debugger con attach to process

## ?? Referencias

- [Roslyn Analyzers Documentation](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/README.md)
- [How to Debug Analyzers](https://github.com/dotnet/roslyn/blob/main/docs/wiki/How-To-Debug-Analyzers.md)
- [Analyzer Testing](https://github.com/dotnet/roslyn-sdk/tree/main/src/Microsoft.CodeAnalysis.Testing)
