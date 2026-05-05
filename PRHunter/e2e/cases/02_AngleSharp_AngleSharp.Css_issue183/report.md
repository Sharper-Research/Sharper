# AngleSharp/AngleSharp.Css#183 - InvalidOperationException

- **Issue**: [StyleDeclarationExtensions.GetColor throws when there is no color assigned](https://github.com/AngleSharp/AngleSharp.Css/issues/183)
- **Failed PR** (rejected): https://github.com/AngleSharp/AngleSharp.Css/pull/197
- **Base SHA** (buggy state): `366abd16ee7cdb027af7cb3a720a6515d12fd2f8`

## Files touched by the failed fix

- `src/AngleSharp.Css.Tests/Library/ComputedStyle.cs`
- `src/AngleSharp.Css/BrowsingContextExtensions.cs`
- `src/AngleSharp.Css/Dom/Internal/CssStyleDeclaration.cs`

## AutoRepair findings

`violations.json`: **934 violations** captured from the live test trace.

- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.OrValueConverter, IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.StructValueConverter`1[[AngleSharp.Css.Values.CssIntegerValue, AngleSharp.Css, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e83494dcdc6d31ea]], IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.DictionaryValueConverter`1[[AngleSharp.Css.Dom.BookmarkState, AngleSharp.Css, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e83494dcdc6d31ea]], IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.DictionaryValueConverter`1[[AngleSharp.Css.Dom.FootnoteDisplay, AngleSharp.Css, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e83494dcdc6d31ea]], IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.DictionaryValueConverter`1[[AngleSharp.Css.Dom.FootnotePolicy, AngleSharp.Css, Version=1.0.0.0, Culture=neutral, PublicKeyToken=e83494dcdc6d31ea]], IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.IdentifierValueConverter, IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.OrValueConverter, IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.OrValueConverter, IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Declarations.CursorDeclaration+CursorValueConverter, IValueAggregator)  ->  False
- `DeclarationInfo.cs:30` - ValidCast(AngleSharp.Css.Converters.OrValueConverter, IValueAggregator)  ->  False
- ... and 924 more

## Why SonarQube misses this

SonarQube is a static analyzer. It inspects source text for syntactic
patterns and shallow data-flow facts. The bug above only manifests on
a specific runtime path: the variable bindings, buffer sizes, or cast
targets that produce the violation are not recoverable from source
alone. AutoRepair instruments the code, runs the existing test suite,
and observes the actual failing trace - the violation is reported
with concrete evidence (file, line, predicate, observed values).
