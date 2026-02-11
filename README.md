<!-- in VS Code press Ctrl + Shift + V to see a preview-->
![Logo](https://raw.githubusercontent.com/goswinr/Fittings/main/Docs/img/logo128.png)

# Fittings
[![Fittings on nuget.org](https://img.shields.io/nuget/v/Fittings)](https://www.nuget.org/packages/Fittings/)
[![Build Status](https://github.com/goswinr/Fittings/actions/workflows/build.yml/badge.svg)](https://github.com/goswinr/Fittings/actions/workflows/build.yml)
[![Docs Build Status](https://github.com/goswinr/Fittings/actions/workflows/docs.yml/badge.svg)](https://github.com/goswinr/Fittings/actions/workflows/docs.yml)
[![license](https://img.shields.io/github/license/goswinr/Fittings)](LICENSE.md)
![code size](https://img.shields.io/github/languages/code-size/goswinr/Fittings.svg)


Fittings is a collection of utilities for working with WPF in F#. It has
* A persistent Window class that will remember its size and position on screen after each change.
* Utilities for synchronization, global error handling, Dependency Properties, Commands, and ViewModels
* A class for loading and saving simple app settings async called `PersistentSettings`

It has no dependencies. (Apart from FSharp.Core that every F# library depends upon.)

> [!CAUTION]
> When used from C# add a reference to FSharp.Core 6.0.7 or higher.

## Full API Documentation

[goswinr.github.io/Fittings](https://goswinr.github.io/Fittings/reference/fittings.html)

### Download

Fittings is available as [NuGet package](https://www.nuget.org/packages/Fittings).

### How to build

Just run `dotnet build`

### Changelog
see [CHANGELOG.md](https://github.com/goswinr/Fittings/blob/main/CHANGELOG.md)

### License

[MIT](https://github.com/goswinr/Fittings/blob/main/LICENSE.md)
