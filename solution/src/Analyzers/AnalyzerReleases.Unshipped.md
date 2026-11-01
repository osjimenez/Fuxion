; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|--------------------
FXTY001  | Usage    | Error    | The key of a type is not in its canonical form
FXTY002  | Usage    | Error    | The revision of a type is not valid
FXTY003  | Usage    | Error    | The base or an interface of a type cannot be named
FXTY004  | Usage    | Error    | A key is on more than one type
FXTY005  | Usage    | Error    | A type with a key cannot be registered
FXTY006  | Usage    | Error    | The class of the type definitions is not valid

; Add new rules here during development
; Move to AnalyzerReleases.Shipped.md when released
