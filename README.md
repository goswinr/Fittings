<!-- in VS Code press Ctrl + Shift + V to see a preview-->
![Logo](https://raw.githubusercontent.com/goswinr/Fittings/main/Doc/logo128.png)

# Fittings
[![Fittings on nuget.org](https://img.shields.io/nuget/v/Fittings.svg)](https://www.nuget.org/packages/Fittings/)
![code size](https://img.shields.io/github/languages/code-size/goswinr/Fittings.svg)
[![license](https://img.shields.io/github/license/goswinr/Fittings)](LICENSE)


Fittings is a collection of utilities for working with WPF in F#. It has
* A persistent Window class that will remember its size and position on screen after each change.
* Utilities for synchronization, global error handling, Dependency Properties, Commands, and ViewModels
* A class for loading and saving simple app settings async called `PersistentSettings`


It has zero dependencies. Apart form FSharp.Core (6.0.7) that every F# library depends upon.

### License

[MIT](https://raw.githubusercontent.com/goswinr/Fittings/main/LICENSE.txt)

### Changelog

`0.8.8`
- upgrade to FSharp.Core 8.0.400 to make it work for Fesh.Revit

`0.8.0`
- Add TryGet.. methods on  `PersistentSettings`

`0.7.0`
- always save default value in `PersistentSettings` on Get(), if key missing
- Setters in `PersistentSettings` always save the value (with the usual delay of 400 ms)

`0.6.0`
- Rename this library to Fittings from FsEx.Wpf

`0.5.0`
- trim whitespace on keys and values in  PersistentSettings
- ignore empty lines in PersistentSettings

`0.4.0`
- rename Settings to PersistentSettings
- add warning to SaveWriter if file is used twice

`0.3.1`
- rename ErrorHandeling to ErrorHandling
- better documentation
- fix float precision in Settings class

`0.2.0`
- more functionality on Settings serialization API

`0.1.0`
- first public release

