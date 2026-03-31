# Changelog

## [1.4.1](https://github.com/mggarofalo/gas/compare/v1.4.0...v1.4.1) (2026-03-31)


### Bug Fixes

* stop admin seeder from resetting password on every restart ([961bf2f](https://github.com/mggarofalo/gas/commit/961bf2f6be544c0dee8de2816195303df99c7280))
* stop admin seeder from resetting password on every restart ([8459952](https://github.com/mggarofalo/gas/commit/8459952a0db8164c869dc233a63d5ea910b2dbb7))

## [1.4.0](https://github.com/mggarofalo/gas/compare/v1.3.0...v1.4.0) (2026-03-31)


### Features

* UI polish — animations, toasts, spinners, empty states ([12cf3a8](https://github.com/mggarofalo/gas/commit/12cf3a8a1d14cc9a6f3a9e68fc46aa8e4a4a4d4e))
* UI polish and backend normalization ([6626917](https://github.com/mggarofalo/gas/commit/6626917bc52e1da2a236f940477ad0c4c9d1be16))

## [1.3.0](https://github.com/mggarofalo/gas/compare/v1.2.5...v1.3.0) (2026-03-31)


### Features

* add JWT authentication with admin seed and login UI ([3ad3641](https://github.com/mggarofalo/gas/commit/3ad36410d27a260e0f1ded2a8003aed787425c3f))
* add JWT authentication with admin seed and login UI ([4a60d20](https://github.com/mggarofalo/gas/commit/4a60d20b133a7a21f2d2a8101dc0ba4429041f15))
* add nearby station lookup from local fill-up history ([ce9345a](https://github.com/mggarofalo/gas/commit/ce9345aa41bb0c13693e6c161ca15b85233bebbf))
* add secrets volume, entrypoint, and internal Docker networking ([f2abae1](https://github.com/mggarofalo/gas/commit/f2abae103bb2f6cd5a7d70f58d2ed68e81958b8f))
* implement vehicle CRUD, fill-up CRUD, receipts, and dashboard ([4793bc6](https://github.com/mggarofalo/gas/commit/4793bc67eb3b19c2a09eb08bd4389acd270dc238))
* scaffold .NET API, React frontend, Docker, and CI/CD ([2a2429d](https://github.com/mggarofalo/gas/commit/2a2429da967f0a317f79d6537184a17df96db47d))


### Bug Fixes

* add EF Core migration and handle MinIO bucket check gracefully ([2fd3dab](https://github.com/mggarofalo/gas/commit/2fd3dab01ce5fd73728a75e21921b4296ccf100d))
* dark mode theme system, login password sync, code splitting ([9d8ef7e](https://github.com/mggarofalo/gas/commit/9d8ef7e27918996abb3908b211e1280cded66a47))
* dark mode theme, login password sync, code splitting ([f1b8fa4](https://github.com/mggarofalo/gas/commit/f1b8fa4a368cb000f0c53053e4ad5ba26d21912a))
* prevent shell mangling of generated admin password ([4cb7768](https://github.com/mggarofalo/gas/commit/4cb77680e2f4308314f3e9832baced73659d7ea4))
* prevent shell mangling of generated admin password ([c4779c1](https://github.com/mggarofalo/gas/commit/c4779c1172fd5f0f0b98f4ed36d0a6a55f226795))
* remove GetRolesAsync call from token service ([6454297](https://github.com/mggarofalo/gas/commit/6454297abf7257feb4228f675d41404aa9b77ab0))
* remove GetRolesAsync call from token service ([2267ec1](https://github.com/mggarofalo/gas/commit/2267ec1e8b838aea4c55fc62b431129478771891))
* resolve Zod v4 + hookform resolver type errors for tsc -b ([d2208bf](https://github.com/mggarofalo/gas/commit/d2208bfaf4e33b23424b96be457483096bbfc6ac))
* restore API project directly in Dockerfile instead of solution ([92a6916](https://github.com/mggarofalo/gas/commit/92a69164cb8c65b535b641b90077280b9434fab5))
* serve React SPA from wwwroot with fallback routing ([2c4087f](https://github.com/mggarofalo/gas/commit/2c4087ff877288729804e12e3e75657a209036ea))
* serve React SPA from wwwroot with fallback routing ([b8ccfa2](https://github.com/mggarofalo/gas/commit/b8ccfa217da77f80165c775a392ef1bbe10df39b))


### Performance Improvements

* lazy-load all route components ([a106aec](https://github.com/mggarofalo/gas/commit/a106aec4e373af8c7a59f9c815ce7b27ed140872))
* lazy-load all route components ([62bbd1b](https://github.com/mggarofalo/gas/commit/62bbd1b0f45e706e4781cc879e73b4a61885aab2))

## [1.2.5](https://github.com/mggarofalo/gas/compare/v1.2.4...v1.2.5) (2026-03-31)


### Bug Fixes

* remove GetRolesAsync call from token service ([6454297](https://github.com/mggarofalo/gas/commit/6454297abf7257feb4228f675d41404aa9b77ab0))
* remove GetRolesAsync call from token service ([2267ec1](https://github.com/mggarofalo/gas/commit/2267ec1e8b838aea4c55fc62b431129478771891))

## [1.2.4](https://github.com/mggarofalo/gas/compare/v1.2.3...v1.2.4) (2026-03-31)


### Bug Fixes

* prevent shell mangling of generated admin password ([4cb7768](https://github.com/mggarofalo/gas/commit/4cb77680e2f4308314f3e9832baced73659d7ea4))
* prevent shell mangling of generated admin password ([c4779c1](https://github.com/mggarofalo/gas/commit/c4779c1172fd5f0f0b98f4ed36d0a6a55f226795))

## [1.2.3](https://github.com/mggarofalo/gas/compare/v1.2.2...v1.2.3) (2026-03-31)


### Performance Improvements

* lazy-load all route components ([a106aec](https://github.com/mggarofalo/gas/commit/a106aec4e373af8c7a59f9c815ce7b27ed140872))
* lazy-load all route components ([62bbd1b](https://github.com/mggarofalo/gas/commit/62bbd1b0f45e706e4781cc879e73b4a61885aab2))

## [1.2.2](https://github.com/mggarofalo/gas/compare/v1.2.1...v1.2.2) (2026-03-31)


### Bug Fixes

* dark mode theme system, login password sync, code splitting ([9d8ef7e](https://github.com/mggarofalo/gas/commit/9d8ef7e27918996abb3908b211e1280cded66a47))
* dark mode theme, login password sync, code splitting ([f1b8fa4](https://github.com/mggarofalo/gas/commit/f1b8fa4a368cb000f0c53053e4ad5ba26d21912a))

## [1.2.1](https://github.com/mggarofalo/gas/compare/v1.2.0...v1.2.1) (2026-03-31)


### Bug Fixes

* serve React SPA from wwwroot with fallback routing ([2c4087f](https://github.com/mggarofalo/gas/commit/2c4087ff877288729804e12e3e75657a209036ea))
* serve React SPA from wwwroot with fallback routing ([b8ccfa2](https://github.com/mggarofalo/gas/commit/b8ccfa217da77f80165c775a392ef1bbe10df39b))

## [1.2.0](https://github.com/mggarofalo/gas/compare/v1.1.1...v1.2.0) (2026-03-31)


### Features

* add JWT authentication with admin seed and login UI ([3ad3641](https://github.com/mggarofalo/gas/commit/3ad36410d27a260e0f1ded2a8003aed787425c3f))
* add JWT authentication with admin seed and login UI ([4a60d20](https://github.com/mggarofalo/gas/commit/4a60d20b133a7a21f2d2a8101dc0ba4429041f15))

## [1.1.1](https://github.com/mggarofalo/gas/compare/v1.1.0...v1.1.1) (2026-03-31)


### Bug Fixes

* add EF Core migration and handle MinIO bucket check gracefully ([2fd3dab](https://github.com/mggarofalo/gas/commit/2fd3dab01ce5fd73728a75e21921b4296ccf100d))

## [1.1.0](https://github.com/mggarofalo/gas/compare/v1.0.1...v1.1.0) (2026-03-31)


### Features

* add secrets volume, entrypoint, and internal Docker networking ([f2abae1](https://github.com/mggarofalo/gas/commit/f2abae103bb2f6cd5a7d70f58d2ed68e81958b8f))

## [1.0.1](https://github.com/mggarofalo/gas/compare/v1.0.0...v1.0.1) (2026-03-31)


### Bug Fixes

* restore API project directly in Dockerfile instead of solution ([92a6916](https://github.com/mggarofalo/gas/commit/92a69164cb8c65b535b641b90077280b9434fab5))

## 1.0.0 (2026-03-31)


### Features

* add nearby station lookup from local fill-up history ([ce9345a](https://github.com/mggarofalo/gas/commit/ce9345aa41bb0c13693e6c161ca15b85233bebbf))
* implement vehicle CRUD, fill-up CRUD, receipts, and dashboard ([4793bc6](https://github.com/mggarofalo/gas/commit/4793bc67eb3b19c2a09eb08bd4389acd270dc238))
* scaffold .NET API, React frontend, Docker, and CI/CD ([2a2429d](https://github.com/mggarofalo/gas/commit/2a2429da967f0a317f79d6537184a17df96db47d))


### Bug Fixes

* resolve Zod v4 + hookform resolver type errors for tsc -b ([d2208bf](https://github.com/mggarofalo/gas/commit/d2208bfaf4e33b23424b96be457483096bbfc6ac))
