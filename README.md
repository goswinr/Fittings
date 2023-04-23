<!-- in VS Code press Ctrl + Shift + V to see a preview-->

# FsEx.Wpf

[![FsEx.Wpf on nuget.org](https://img.shields.io/nuget/v/FsEx.Wpf.svg)](https://www.nuget.org/packages/FsEx.Wpf/)
[![FsEx.Wpf on fuget.org](https://www.fuget.org/packages/FsEx.Wpf/badge.svg)](https://www.fuget.org/packages/FsEx.Wpf)
![code size](https://img.shields.io/github/languages/code-size/goswinr/FsEx.Wpf.svg) 
[![license](https://img.shields.io/github/license/goswinr/FsEx.Wpf)](LICENSE)

![Logo](https://raw.githubusercontent.com/goswinr/FsEx.Wpf/main/Doc/logo128.png)

FsEx.Wpf is a collection of utilities for working with WPF in F#. It has
* A persistent Window class that will remember its size and position on screen after each change.
* Utilities for synchronization, global error handling, Dependency Properties, Commands, and ViewModels
* A class for loading and saving simple app settings async called `PersistentSettings`


This Library carries the `FsEx` prefix, but has no dependency on https://github.com/goswinr/FsEx. 
It only has the same author.

It actually has zero dependencies. Apart form FSharp.Core (4.5+) that every F# library depends upon.

### License

[MIT](https://raw.githubusercontent.com/goswinr/FsEx.Wpf/main/LICENSE.txt)

### Changelog
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

