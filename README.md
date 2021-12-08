# Null Return Analyzer

## Summary

Roslyn analyzer aims to reduce hidden null reference exceptions for projects which is not ready for [Nullable Reference Types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) (e.g. some Unity projects).  
Analyzer enforce you to use [CanBeNull] (from [JetBrains](https://www.jetbrains.com/help/resharper/Reference__Code_Annotation_Attributes.html) or any custom with that name) attribute on obviously null-return methods.  
Code fix is also provided.

## Examples

```
object GetAlwaysNullObject() {
    return null;
}
```
Compiler warning:
```
[NR1001] Method returns null value, but it is not marked with [CanBeNull]
```

## Installation

### Unity 2020.2+

Follow instructions - https://docs.unity3d.com/2020.2/Documentation/Manual/roslyn-analyzers.html

### Unity (older)

Add UPM package https://github.com/tertle/com.bovinelabs.analyzers and put analyzer DLL in expected location (RoslynAnalyzers by default)