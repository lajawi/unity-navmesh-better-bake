# Changelog

## [1.1.0] - 2025.09.17

### Added

- Support for LOD Group Component (only if `Use Geometry` is set to `Render Meshes`)
    - First LOD is used (`LOD 0`)
    - All renderers from `LOD 0` are included when baking

## [1.0.0] - 2025.09.13

### Added

- Buttons to the NavMesh Surface component that...
    - realise all tree instances of terrains in the scene, with all necessary components
    - cleans up realised instances after the bake

[Unreleased]: https://github.com/lajawi/unity-navmesh-better-bake/blob/main
[1.0.0]: https://github.com/lajawi/unity-navmesh-better-bake/releases/tag/v1.0.0
[1.1.0]: https://github.com/lajawi/unity-navmesh-better-bake/compare/v1.0.0...v1.1.0
