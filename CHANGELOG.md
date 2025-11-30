# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.10.0] - 2025-02-14
### Changed
- Add argument to ErrorHandling to skip writing to a file on the desktop.
- use 'dotnet outdated tool' instead of dependabot for nuget package updates
- removed explicit FSharp.Core 6.0.7 reference

## [0.9.0] - 2024-11-19
### Added
- Documentation via [FSharp.Formatting](https://fsprojects.github.io/FSharp.Formatting/)
- Github Actions for CI/CD
### Changed
- pin FSharp.Core to latest version (for Fesh.Revit)

## [0.8.8] - 2024-11-04
### Changed
- Upgrade to FSharp.Core 8.0.400 to make it work for Fesh.Revit

## [0.8.0] - 2024-11-03
### Added
- Add TryGet.. methods on `PersistentSettings`

## [0.7.0] - 2024-11-02
### Changed
- Always save default value in `PersistentSettings` on Get(), if key missing
- Setters in `PersistentSettings` always save the value (with the usual delay of 400 ms)

## [0.6.0] - 2023-10-29
### Changed
- Rename this library to Fittings from FsEx.Wpf

## [0.5.0] - 2023-04-23
### Changed
- Trim whitespace on keys and values in `PersistentSettings`
- Ignore empty lines in `PersistentSettings`

## [0.4.0] - 2023-02-04
### Changed
- Rename Settings to `PersistentSettings`
- Add warning to SaveWriter if file is used twice

## [0.3.1] - 2022-08-06
### Changed
- Rename ErrorHandling to ErrorHandling
- Better documentation
- Fix float precision in Settings class

## [0.2.0] - 2022-03-07
### Added
- More functionality on Settings serialization API

## [0.1.0] - 2022-03-06
### Added
- First public release


[Unreleased]: https://github.com/goswinr/Fittings/compare/0.10.0...HEAD
[0.10.0]: https://github.com/goswinr/Fittings/compare/0.9.0...0.10.0
[0.9.0]: https://github.com/goswinr/Fittings/compare/0.8.8...0.9.0
[0.8.8]: https://github.com/goswinr/Fittings/compare/0.8.0...0.8.8
[0.8.0]: https://github.com/goswinr/Fittings/compare/0.7.0...0.8.0
[0.7.0]: https://github.com/goswinr/Fittings/compare/0.6.0...0.7.0
[0.6.0]: https://github.com/goswinr/Fittings/compare/0.5.0...0.6.0
[0.5.0]: https://github.com/goswinr/Fittings/compare/0.4.0...0.5.0
[0.4.0]: https://github.com/goswinr/Fittings/compare/0.3.1...0.4.0
[0.3.1]: https://github.com/goswinr/Fittings/compare/0.2.0...0.3.1
[0.2.0]: https://github.com/goswinr/Fittings/compare/0.1.0...0.2.0
[0.1.0]: https://github.com/goswinr/Fittings/releases/tag/0.1.0


