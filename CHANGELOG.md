# Changelog

## [1.26.2](https://github.com/mggarofalo/gas/compare/v1.26.1...v1.26.2) (2026-04-05)


### Bug Fixes

* handle unconfigured YNAB in defaults effect to avoid stalled state ([#113](https://github.com/mggarofalo/gas/issues/113)) ([152e7ab](https://github.com/mggarofalo/gas/commit/152e7ab31cfb8809aa95d42251de3ffb93d0fee9))
* UI bugs — vehicle deactivation, mobile layout, YNAB defaults, contrast ([#112](https://github.com/mggarofalo/gas/issues/112)) ([7d04a31](https://github.com/mggarofalo/gas/commit/7d04a318ff65f8b1d3c22381125f8106b2a4edc9))

## [1.26.1](https://github.com/mggarofalo/gas/compare/v1.26.0...v1.26.1) (2026-04-04)


### Bug Fixes

* use PAT for release-please so tag pushes trigger docker publish ([#110](https://github.com/mggarofalo/gas/issues/110)) ([fba4358](https://github.com/mggarofalo/gas/commit/fba4358d04399ff8d9692fbe6191a9eb1c9c68ca))

## [1.26.0](https://github.com/mggarofalo/gas/compare/v1.25.0...v1.26.0) (2026-04-04)


### Features

* add dark/light/system theme switcher ([#105](https://github.com/mggarofalo/gas/issues/105)) ([a72d0d0](https://github.com/mggarofalo/gas/commit/a72d0d0d1c4b73355884be0d7761ace111ac0400))
* add health check endpoint with DB and MinIO validation ([#41](https://github.com/mggarofalo/gas/issues/41)) ([427024e](https://github.com/mggarofalo/gas/commit/427024eb64507086e67fdc222a9837e40b795453))
* add JWT authentication with admin seed and login UI ([3ad3641](https://github.com/mggarofalo/gas/commit/3ad36410d27a260e0f1ded2a8003aed787425c3f))
* add JWT authentication with admin seed and login UI ([4a60d20](https://github.com/mggarofalo/gas/commit/4a60d20b133a7a21f2d2a8101dc0ba4429041f15))
* add nearby station lookup from local fill-up history ([ce9345a](https://github.com/mggarofalo/gas/commit/ce9345aa41bb0c13693e6c161ca15b85233bebbf))
* add octane rating to vehicles and fill-ups ([#46](https://github.com/mggarofalo/gas/issues/46)) ([e92e36b](https://github.com/mggarofalo/gas/commit/e92e36b86e6d1073c54e8383ed7cf370237be4ae))
* add Paperless sync status UI to fill-up pages ([#63](https://github.com/mggarofalo/gas/issues/63)) ([e7144f0](https://github.com/mggarofalo/gas/commit/e7144f0805787643f3ef570ab691dc8ea6997af7))
* add Paperless-ngx background sync service ([#61](https://github.com/mggarofalo/gas/issues/61)) ([ba7cf75](https://github.com/mggarofalo/gas/commit/ba7cf75d90c9a972bce1561e54bf70eebf83fedf))
* add PostgreSQL backup script with 7-day rotation ([#44](https://github.com/mggarofalo/gas/issues/44)) ([8509367](https://github.com/mggarofalo/gas/commit/850936759d7c4c5b223a83a7eb99caab68dd73ac))
* add pull sync reset to clear server_knowledge and stale imports ([#78](https://github.com/mggarofalo/gas/issues/78)) ([739ff5f](https://github.com/mggarofalo/gas/commit/739ff5ff5fec2931a09a6db24cbbe55bb11f792b))
* add secrets volume, entrypoint, and internal Docker networking ([f2abae1](https://github.com/mggarofalo/gas/commit/f2abae103bb2f6cd5a7d70f58d2ed68e81958b8f))
* add vehicle filter dropdown to dashboard ([#104](https://github.com/mggarofalo/gas/issues/104)) ([ff1c4f6](https://github.com/mggarofalo/gas/commit/ff1c4f6a036eccbddb94d94c893a80e5df1ace51))
* add vehicle memo mappings UI to import queue page ([#82](https://github.com/mggarofalo/gas/issues/82)) ([c7788cd](https://github.com/mggarofalo/gas/commit/c7788cdad0fc6efb86884c5090737da14a388a61))
* add YNAB CSV ingest endpoint for historical fill-ups ([#48](https://github.com/mggarofalo/gas/issues/48)) ([991cdf5](https://github.com/mggarofalo/gas/commit/991cdf5b11eea96acc0194bcd06c2031521e8881))
* add YNAB settings page with cascading dropdowns ([#55](https://github.com/mggarofalo/gas/issues/55)) ([dd24a18](https://github.com/mggarofalo/gas/commit/dd24a18c382eaaf4029f53b3e4c811ae787edb34))
* add YNAB settings table and configuration API ([#50](https://github.com/mggarofalo/gas/issues/50)) ([5f012dc](https://github.com/mggarofalo/gas/commit/5f012dcc25b551bf1b160fd494001d4b86bede0d))
* add YNAB two-way sync with import review queue ([#72](https://github.com/mggarofalo/gas/issues/72)) ([3a87f77](https://github.com/mggarofalo/gas/commit/3a87f77918f2d27662aee380327d5fe31cc8b14e))
* flexible memo parser, auto-apply mappings on list, validation icons ([#86](https://github.com/mggarofalo/gas/issues/86)) ([c0f22ff](https://github.com/mggarofalo/gas/commit/c0f22ff0b356ecd3e44ab8aba3fcc2538595566b))
* implement Paperless-ngx API client for document upload ([#59](https://github.com/mggarofalo/gas/issues/59)) ([e0e2dc7](https://github.com/mggarofalo/gas/commit/e0e2dc7e2c942d3f8cf2aa0480bf022ff98337f5))
* implement vehicle CRUD, fill-up CRUD, receipts, and dashboard ([4793bc6](https://github.com/mggarofalo/gas/commit/4793bc67eb3b19c2a09eb08bd4389acd270dc238))
* implement YNAB API client and proxy endpoints ([#52](https://github.com/mggarofalo/gas/issues/52)) ([981c6b0](https://github.com/mggarofalo/gas/commit/981c6b0d5ecc50ff6ebc5ba614a107054494bb62))
* mobile-first responsive design and form polish ([5c44943](https://github.com/mggarofalo/gas/commit/5c44943635194e799d003dba5c272dc70d997e24))
* mobile-first responsive design and form polish ([350dfdd](https://github.com/mggarofalo/gas/commit/350dfdd301626cda459e5405c8ebafc2298aadcc))
* per-fill-up YNAB account and category selection ([#76](https://github.com/mggarofalo/gas/issues/76)) ([2fa7797](https://github.com/mggarofalo/gas/commit/2fa7797d745a6ae7e45a362526495178c3b31dea))
* push fill-up transactions to YNAB on creation ([#53](https://github.com/mggarofalo/gas/issues/53)) ([cbd6238](https://github.com/mggarofalo/gas/commit/cbd6238ca02bd021e8ddabb7319b04c16946eead))
* receipt upload triggers iOS document scanner ([7d44872](https://github.com/mggarofalo/gas/commit/7d448721563b79cb5e8d1ef0a7b1075cc0c3b29a))
* receipt upload triggers iOS document scanner ([9024201](https://github.com/mggarofalo/gas/commit/9024201ce8aa2f6a05cef12163e2178795d50f11))
* reorder fill-up form and add station autocomplete ([a07cded](https://github.com/mggarofalo/gas/commit/a07cdedb426dcc9c950f4b1dc85ac1499d5b508b))
* reorder fill-up form, add station autocomplete from history ([83c4957](https://github.com/mggarofalo/gas/commit/83c49573c84f6147ce1e3622f4e8d1f6e63d1bbb))
* scaffold .NET API, React frontend, Docker, and CI/CD ([2a2429d](https://github.com/mggarofalo/gas/commit/2a2429da967f0a317f79d6537184a17df96db47d))
* show app version in sidebar footer ([7cac3e7](https://github.com/mggarofalo/gas/commit/7cac3e71cedae3b9cfdd3978b9d583421d809899))
* show app version in sidebar footer ([a82f3eb](https://github.com/mggarofalo/gas/commit/a82f3eb5148d806e5237b7538c29720ced51e2ee))
* UI polish — animations, toasts, spinners, empty states ([12cf3a8](https://github.com/mggarofalo/gas/commit/12cf3a8a1d14cc9a6f3a9e68fc46aa8e4a4a4d4e))
* UI polish and backend normalization ([6626917](https://github.com/mggarofalo/gas/commit/6626917bc52e1da2a236f940477ad0c4c9d1be16))
* update fill-up form with total price, calculated gallons, and YNAB picker ([#103](https://github.com/mggarofalo/gas/issues/103)) ([d87ded6](https://github.com/mggarofalo/gas/commit/d87ded6420f792c7675433b9f410a74e2e4b5ec2))
* YNAB API-driven backfill and per-fill-up account selection ([#70](https://github.com/mggarofalo/gas/issues/70)) ([f65c357](https://github.com/mggarofalo/gas/commit/f65c357b28327cc62ed2cd67a6329b3b7ac85fc9))


### Bug Fixes

* add EF Core migration and handle MinIO bucket check gracefully ([2fd3dab](https://github.com/mggarofalo/gas/commit/2fd3dab01ce5fd73728a75e21921b4296ccf100d))
* add writable dp-keys volume for Data Protection key persistence ([#66](https://github.com/mggarofalo/gas/issues/66)) ([4b54841](https://github.com/mggarofalo/gas/commit/4b54841aa482944863d8bd51aba70cba92cfe735))
* align frontend API calls with backend endpoint paths ([#102](https://github.com/mggarofalo/gas/issues/102)) ([eaff408](https://github.com/mggarofalo/gas/commit/eaff40869a3ee739523db3219de07f2898a7a4b4))
* calculate gallons in import list projection when DB value is null ([#90](https://github.com/mggarofalo/gas/issues/90)) ([490d097](https://github.com/mggarofalo/gas/commit/490d0976c28adc77cb79aa571f1e4af402aa046f))
* dark mode theme system, login password sync, code splitting ([9d8ef7e](https://github.com/mggarofalo/gas/commit/9d8ef7e27918996abb3908b211e1280cded66a47))
* dark mode theme, login password sync, code splitting ([f1b8fa4](https://github.com/mggarofalo/gas/commit/f1b8fa4a368cb000f0c53053e4ad5ba26d21912a))
* frontend gallons calculation + parser ordering tests ([#92](https://github.com/mggarofalo/gas/issues/92)) ([818eb88](https://github.com/mggarofalo/gas/commit/818eb888df8ba114476b6e516794f62b86036745))
* GPS button shows state inline, remove lat/lng display, clamp decimals ([c1fa165](https://github.com/mggarofalo/gas/commit/c1fa16591ac5106ec20638ab59e9698f65ca5e06))
* GPS button state indicator, remove lat/lng display, clamp decimals ([993f937](https://github.com/mggarofalo/gas/commit/993f937e09847c04a5c01250f4a14bc6d3d4933e))
* GPS requires user gesture, receipt shows iOS scanner, suppress password managers ([7e66546](https://github.com/mggarofalo/gas/commit/7e66546ebd3834d50082898afb1072656e03b4a3))
* GPS user gesture, iOS scanner, password manager suppression ([c48ece4](https://github.com/mggarofalo/gas/commit/c48ece4a20f5bd512c3d42b14315889a69bd793a))
* increase YNAB HttpClient timeout for large plan fetches ([#80](https://github.com/mggarofalo/gas/issues/80)) ([4dfbaa0](https://github.com/mggarofalo/gas/commit/4dfbaa001917756267a1ed13be2842b8046b9935))
* iOS Safari geolocation, document scanner, and Bitwarden suppression ([acb222a](https://github.com/mggarofalo/gas/commit/acb222a25da532e73eb3620111da121ce69ac034))
* iOS Safari geolocation, document scanner, and Bitwarden suppression ([b218cf0](https://github.com/mggarofalo/gas/commit/b218cf09dc8c0bde87c4586f12bde53e20fcdf0c))
* limit gallons and price inputs to 999.999 ([6e81b98](https://github.com/mggarofalo/gas/commit/6e81b98be4afd408be50cf77a96c52c88a6dfd16))
* limit gallons and price to 999.999 ([5788ff7](https://github.com/mggarofalo/gas/commit/5788ff72dea5ab2d161575554043245f8e3fce92))
* mount dp-keys volume outside read-only /secrets ([#69](https://github.com/mggarofalo/gas/issues/69)) ([7b6226b](https://github.com/mggarofalo/gas/commit/7b6226bc440e84cfc35b9b6903ab0c6795a4eaf2))
* prevent shell mangling of generated admin password ([4cb7768](https://github.com/mggarofalo/gas/commit/4cb77680e2f4308314f3e9832baced73659d7ea4))
* prevent shell mangling of generated admin password ([c4779c1](https://github.com/mggarofalo/gas/commit/c4779c1172fd5f0f0b98f4ed36d0a6a55f226795))
* re-parse memos on import list load for stale imports ([#95](https://github.com/mggarofalo/gas/issues/95)) ([f5533a2](https://github.com/mggarofalo/gas/commit/f5533a2fac9db1f58c85979ba53c911ba7559ad7))
* remove GetRolesAsync call from token service ([6454297](https://github.com/mggarofalo/gas/commit/6454297abf7257feb4228f675d41404aa9b77ab0))
* remove GetRolesAsync call from token service ([2267ec1](https://github.com/mggarofalo/gas/commit/2267ec1e8b838aea4c55fc62b431129478771891))
* replace numeric spinner inputs with formatted text inputs ([d8a25f2](https://github.com/mggarofalo/gas/commit/d8a25f220f080f75d09e54f9f33c10931e2b441f))
* replace numeric spinner inputs with formatted text inputs ([9109c43](https://github.com/mggarofalo/gas/commit/9109c43bf34f2fbdf6a3244b3bdcc7b66396a0e7))
* resolve all build warnings and enable TreatWarningsAsErrors ([#57](https://github.com/mggarofalo/gas/issues/57)) ([b0f42e0](https://github.com/mggarofalo/gas/commit/b0f42e04c3dbe3f8c5cdea9492af23bb9c02d41b))
* resolve Zod v4 + hookform resolver type errors for tsc -b ([d2208bf](https://github.com/mggarofalo/gas/commit/d2208bfaf4e33b23424b96be457483096bbfc6ac))
* restore API project directly in Dockerfile instead of solution ([92a6916](https://github.com/mggarofalo/gas/commit/92a69164cb8c65b535b641b90077280b9434fab5))
* retroactively apply vehicle memo mappings to pending imports ([#84](https://github.com/mggarofalo/gas/issues/84)) ([90ff435](https://github.com/mggarofalo/gas/commit/90ff4351631ef8c0af7163b54a47439ec131ee40))
* separate token endpoint, suppress log noise, stop 401 retry loop (GAS-71, GAS-72) ([#74](https://github.com/mggarofalo/gas/issues/74)) ([e6c8606](https://github.com/mggarofalo/gas/commit/e6c8606db8e66e391518fc82d962f45b2d303127))
* serve React SPA from wwwroot with fallback routing ([2c4087f](https://github.com/mggarofalo/gas/commit/2c4087ff877288729804e12e3e75657a209036ea))
* serve React SPA from wwwroot with fallback routing ([b8ccfa2](https://github.com/mggarofalo/gas/commit/b8ccfa217da77f80165c775a392ef1bbe10df39b))
* stop admin seeder from resetting password on every restart ([961bf2f](https://github.com/mggarofalo/gas/commit/961bf2f6be544c0dee8de2816195303df99c7280))
* stop admin seeder from resetting password on every restart ([8459952](https://github.com/mggarofalo/gas/commit/8459952a0db8164c869dc233a63d5ea910b2dbb7))
* strip parens from memos, auto-calculate gallons from amount/price ([#88](https://github.com/mggarofalo/gas/issues/88)) ([59018a4](https://github.com/mggarofalo/gas/commit/59018a4c2fdcd92839b32c344b2ec8cafac0e84c))
* update Dockerfile to copy Directory.*.props from repo root ([0cc4284](https://github.com/mggarofalo/gas/commit/0cc42845b771afe628897e5cdf3ad39383af3fc9))
* use correct release-type in release-please workflow ([542fef1](https://github.com/mggarofalo/gas/commit/542fef198762bf26717445b0f8cde62f75f2e554))
* YNAB quality, dashboard charts, fill-up editing ([#97](https://github.com/mggarofalo/gas/issues/97)) ([496d6cd](https://github.com/mggarofalo/gas/commit/496d6cd04c969e49707a6fc20bc898deff846422))


### Performance Improvements

* lazy-load all route components ([a106aec](https://github.com/mggarofalo/gas/commit/a106aec4e373af8c7a59f9c815ce7b27ed140872))
* lazy-load all route components ([62bbd1b](https://github.com/mggarofalo/gas/commit/62bbd1b0f45e706e4781cc879e73b4a61885aab2))

## [1.25.0](https://github.com/mggarofalo/gas/compare/v1.24.3...v1.25.0) (2026-04-04)


### Features

* add dark/light/system theme switcher ([#105](https://github.com/mggarofalo/gas/issues/105)) ([a72d0d0](https://github.com/mggarofalo/gas/commit/a72d0d0d1c4b73355884be0d7761ace111ac0400))
* add vehicle filter dropdown to dashboard ([#104](https://github.com/mggarofalo/gas/issues/104)) ([ff1c4f6](https://github.com/mggarofalo/gas/commit/ff1c4f6a036eccbddb94d94c893a80e5df1ace51))
* update fill-up form with total price, calculated gallons, and YNAB picker ([#103](https://github.com/mggarofalo/gas/issues/103)) ([d87ded6](https://github.com/mggarofalo/gas/commit/d87ded6420f792c7675433b9f410a74e2e4b5ec2))


### Bug Fixes

* align frontend API calls with backend endpoint paths ([#102](https://github.com/mggarofalo/gas/issues/102)) ([eaff408](https://github.com/mggarofalo/gas/commit/eaff40869a3ee739523db3219de07f2898a7a4b4))

## [1.24.2](https://github.com/mggarofalo/gas/compare/v1.24.1...v1.24.2) (2026-04-04)


### Bug Fixes

* use correct release-type in release-please workflow ([542fef1](https://github.com/mggarofalo/gas/commit/542fef198762bf26717445b0f8cde62f75f2e554))
* YNAB quality, dashboard charts, fill-up editing ([#97](https://github.com/mggarofalo/gas/issues/97)) ([496d6cd](https://github.com/mggarofalo/gas/commit/496d6cd04c969e49707a6fc20bc898deff846422))

## [1.24.1](https://github.com/mggarofalo/gas/compare/v1.24.0...v1.24.1) (2026-04-03)


### Bug Fixes

* re-parse memos on import list load for stale imports ([#95](https://github.com/mggarofalo/gas/issues/95)) ([f5533a2](https://github.com/mggarofalo/gas/commit/f5533a2fac9db1f58c85979ba53c911ba7559ad7))

## [1.24.0](https://github.com/mggarofalo/gas/compare/v1.23.3...v1.24.0) (2026-04-03)


### Features

* add health check endpoint with DB and MinIO validation ([#41](https://github.com/mggarofalo/gas/issues/41)) ([427024e](https://github.com/mggarofalo/gas/commit/427024eb64507086e67fdc222a9837e40b795453))
* add JWT authentication with admin seed and login UI ([3ad3641](https://github.com/mggarofalo/gas/commit/3ad36410d27a260e0f1ded2a8003aed787425c3f))
* add JWT authentication with admin seed and login UI ([4a60d20](https://github.com/mggarofalo/gas/commit/4a60d20b133a7a21f2d2a8101dc0ba4429041f15))
* add nearby station lookup from local fill-up history ([ce9345a](https://github.com/mggarofalo/gas/commit/ce9345aa41bb0c13693e6c161ca15b85233bebbf))
* add octane rating to vehicles and fill-ups ([#46](https://github.com/mggarofalo/gas/issues/46)) ([e92e36b](https://github.com/mggarofalo/gas/commit/e92e36b86e6d1073c54e8383ed7cf370237be4ae))
* add Paperless sync status UI to fill-up pages ([#63](https://github.com/mggarofalo/gas/issues/63)) ([e7144f0](https://github.com/mggarofalo/gas/commit/e7144f0805787643f3ef570ab691dc8ea6997af7))
* add Paperless-ngx background sync service ([#61](https://github.com/mggarofalo/gas/issues/61)) ([ba7cf75](https://github.com/mggarofalo/gas/commit/ba7cf75d90c9a972bce1561e54bf70eebf83fedf))
* add PostgreSQL backup script with 7-day rotation ([#44](https://github.com/mggarofalo/gas/issues/44)) ([8509367](https://github.com/mggarofalo/gas/commit/850936759d7c4c5b223a83a7eb99caab68dd73ac))
* add pull sync reset to clear server_knowledge and stale imports ([#78](https://github.com/mggarofalo/gas/issues/78)) ([739ff5f](https://github.com/mggarofalo/gas/commit/739ff5ff5fec2931a09a6db24cbbe55bb11f792b))
* add secrets volume, entrypoint, and internal Docker networking ([f2abae1](https://github.com/mggarofalo/gas/commit/f2abae103bb2f6cd5a7d70f58d2ed68e81958b8f))
* add vehicle memo mappings UI to import queue page ([#82](https://github.com/mggarofalo/gas/issues/82)) ([c7788cd](https://github.com/mggarofalo/gas/commit/c7788cdad0fc6efb86884c5090737da14a388a61))
* add YNAB CSV ingest endpoint for historical fill-ups ([#48](https://github.com/mggarofalo/gas/issues/48)) ([991cdf5](https://github.com/mggarofalo/gas/commit/991cdf5b11eea96acc0194bcd06c2031521e8881))
* add YNAB settings page with cascading dropdowns ([#55](https://github.com/mggarofalo/gas/issues/55)) ([dd24a18](https://github.com/mggarofalo/gas/commit/dd24a18c382eaaf4029f53b3e4c811ae787edb34))
* add YNAB settings table and configuration API ([#50](https://github.com/mggarofalo/gas/issues/50)) ([5f012dc](https://github.com/mggarofalo/gas/commit/5f012dcc25b551bf1b160fd494001d4b86bede0d))
* add YNAB two-way sync with import review queue ([#72](https://github.com/mggarofalo/gas/issues/72)) ([3a87f77](https://github.com/mggarofalo/gas/commit/3a87f77918f2d27662aee380327d5fe31cc8b14e))
* flexible memo parser, auto-apply mappings on list, validation icons ([#86](https://github.com/mggarofalo/gas/issues/86)) ([c0f22ff](https://github.com/mggarofalo/gas/commit/c0f22ff0b356ecd3e44ab8aba3fcc2538595566b))
* implement Paperless-ngx API client for document upload ([#59](https://github.com/mggarofalo/gas/issues/59)) ([e0e2dc7](https://github.com/mggarofalo/gas/commit/e0e2dc7e2c942d3f8cf2aa0480bf022ff98337f5))
* implement vehicle CRUD, fill-up CRUD, receipts, and dashboard ([4793bc6](https://github.com/mggarofalo/gas/commit/4793bc67eb3b19c2a09eb08bd4389acd270dc238))
* implement YNAB API client and proxy endpoints ([#52](https://github.com/mggarofalo/gas/issues/52)) ([981c6b0](https://github.com/mggarofalo/gas/commit/981c6b0d5ecc50ff6ebc5ba614a107054494bb62))
* mobile-first responsive design and form polish ([5c44943](https://github.com/mggarofalo/gas/commit/5c44943635194e799d003dba5c272dc70d997e24))
* mobile-first responsive design and form polish ([350dfdd](https://github.com/mggarofalo/gas/commit/350dfdd301626cda459e5405c8ebafc2298aadcc))
* per-fill-up YNAB account and category selection ([#76](https://github.com/mggarofalo/gas/issues/76)) ([2fa7797](https://github.com/mggarofalo/gas/commit/2fa7797d745a6ae7e45a362526495178c3b31dea))
* push fill-up transactions to YNAB on creation ([#53](https://github.com/mggarofalo/gas/issues/53)) ([cbd6238](https://github.com/mggarofalo/gas/commit/cbd6238ca02bd021e8ddabb7319b04c16946eead))
* receipt upload triggers iOS document scanner ([7d44872](https://github.com/mggarofalo/gas/commit/7d448721563b79cb5e8d1ef0a7b1075cc0c3b29a))
* receipt upload triggers iOS document scanner ([9024201](https://github.com/mggarofalo/gas/commit/9024201ce8aa2f6a05cef12163e2178795d50f11))
* reorder fill-up form and add station autocomplete ([a07cded](https://github.com/mggarofalo/gas/commit/a07cdedb426dcc9c950f4b1dc85ac1499d5b508b))
* reorder fill-up form, add station autocomplete from history ([83c4957](https://github.com/mggarofalo/gas/commit/83c49573c84f6147ce1e3622f4e8d1f6e63d1bbb))
* scaffold .NET API, React frontend, Docker, and CI/CD ([2a2429d](https://github.com/mggarofalo/gas/commit/2a2429da967f0a317f79d6537184a17df96db47d))
* show app version in sidebar footer ([7cac3e7](https://github.com/mggarofalo/gas/commit/7cac3e71cedae3b9cfdd3978b9d583421d809899))
* show app version in sidebar footer ([a82f3eb](https://github.com/mggarofalo/gas/commit/a82f3eb5148d806e5237b7538c29720ced51e2ee))
* UI polish — animations, toasts, spinners, empty states ([12cf3a8](https://github.com/mggarofalo/gas/commit/12cf3a8a1d14cc9a6f3a9e68fc46aa8e4a4a4d4e))
* UI polish and backend normalization ([6626917](https://github.com/mggarofalo/gas/commit/6626917bc52e1da2a236f940477ad0c4c9d1be16))
* YNAB API-driven backfill and per-fill-up account selection ([#70](https://github.com/mggarofalo/gas/issues/70)) ([f65c357](https://github.com/mggarofalo/gas/commit/f65c357b28327cc62ed2cd67a6329b3b7ac85fc9))


### Bug Fixes

* add EF Core migration and handle MinIO bucket check gracefully ([2fd3dab](https://github.com/mggarofalo/gas/commit/2fd3dab01ce5fd73728a75e21921b4296ccf100d))
* add writable dp-keys volume for Data Protection key persistence ([#66](https://github.com/mggarofalo/gas/issues/66)) ([4b54841](https://github.com/mggarofalo/gas/commit/4b54841aa482944863d8bd51aba70cba92cfe735))
* calculate gallons in import list projection when DB value is null ([#90](https://github.com/mggarofalo/gas/issues/90)) ([490d097](https://github.com/mggarofalo/gas/commit/490d0976c28adc77cb79aa571f1e4af402aa046f))
* dark mode theme system, login password sync, code splitting ([9d8ef7e](https://github.com/mggarofalo/gas/commit/9d8ef7e27918996abb3908b211e1280cded66a47))
* dark mode theme, login password sync, code splitting ([f1b8fa4](https://github.com/mggarofalo/gas/commit/f1b8fa4a368cb000f0c53053e4ad5ba26d21912a))
* frontend gallons calculation + parser ordering tests ([#92](https://github.com/mggarofalo/gas/issues/92)) ([818eb88](https://github.com/mggarofalo/gas/commit/818eb888df8ba114476b6e516794f62b86036745))
* GPS button shows state inline, remove lat/lng display, clamp decimals ([c1fa165](https://github.com/mggarofalo/gas/commit/c1fa16591ac5106ec20638ab59e9698f65ca5e06))
* GPS button state indicator, remove lat/lng display, clamp decimals ([993f937](https://github.com/mggarofalo/gas/commit/993f937e09847c04a5c01250f4a14bc6d3d4933e))
* GPS requires user gesture, receipt shows iOS scanner, suppress password managers ([7e66546](https://github.com/mggarofalo/gas/commit/7e66546ebd3834d50082898afb1072656e03b4a3))
* GPS user gesture, iOS scanner, password manager suppression ([c48ece4](https://github.com/mggarofalo/gas/commit/c48ece4a20f5bd512c3d42b14315889a69bd793a))
* increase YNAB HttpClient timeout for large plan fetches ([#80](https://github.com/mggarofalo/gas/issues/80)) ([4dfbaa0](https://github.com/mggarofalo/gas/commit/4dfbaa001917756267a1ed13be2842b8046b9935))
* iOS Safari geolocation, document scanner, and Bitwarden suppression ([acb222a](https://github.com/mggarofalo/gas/commit/acb222a25da532e73eb3620111da121ce69ac034))
* iOS Safari geolocation, document scanner, and Bitwarden suppression ([b218cf0](https://github.com/mggarofalo/gas/commit/b218cf09dc8c0bde87c4586f12bde53e20fcdf0c))
* limit gallons and price inputs to 999.999 ([6e81b98](https://github.com/mggarofalo/gas/commit/6e81b98be4afd408be50cf77a96c52c88a6dfd16))
* limit gallons and price to 999.999 ([5788ff7](https://github.com/mggarofalo/gas/commit/5788ff72dea5ab2d161575554043245f8e3fce92))
* mount dp-keys volume outside read-only /secrets ([#69](https://github.com/mggarofalo/gas/issues/69)) ([7b6226b](https://github.com/mggarofalo/gas/commit/7b6226bc440e84cfc35b9b6903ab0c6795a4eaf2))
* prevent shell mangling of generated admin password ([4cb7768](https://github.com/mggarofalo/gas/commit/4cb77680e2f4308314f3e9832baced73659d7ea4))
* prevent shell mangling of generated admin password ([c4779c1](https://github.com/mggarofalo/gas/commit/c4779c1172fd5f0f0b98f4ed36d0a6a55f226795))
* remove GetRolesAsync call from token service ([6454297](https://github.com/mggarofalo/gas/commit/6454297abf7257feb4228f675d41404aa9b77ab0))
* remove GetRolesAsync call from token service ([2267ec1](https://github.com/mggarofalo/gas/commit/2267ec1e8b838aea4c55fc62b431129478771891))
* replace numeric spinner inputs with formatted text inputs ([d8a25f2](https://github.com/mggarofalo/gas/commit/d8a25f220f080f75d09e54f9f33c10931e2b441f))
* replace numeric spinner inputs with formatted text inputs ([9109c43](https://github.com/mggarofalo/gas/commit/9109c43bf34f2fbdf6a3244b3bdcc7b66396a0e7))
* resolve all build warnings and enable TreatWarningsAsErrors ([#57](https://github.com/mggarofalo/gas/issues/57)) ([b0f42e0](https://github.com/mggarofalo/gas/commit/b0f42e04c3dbe3f8c5cdea9492af23bb9c02d41b))
* resolve Zod v4 + hookform resolver type errors for tsc -b ([d2208bf](https://github.com/mggarofalo/gas/commit/d2208bfaf4e33b23424b96be457483096bbfc6ac))
* restore API project directly in Dockerfile instead of solution ([92a6916](https://github.com/mggarofalo/gas/commit/92a69164cb8c65b535b641b90077280b9434fab5))
* retroactively apply vehicle memo mappings to pending imports ([#84](https://github.com/mggarofalo/gas/issues/84)) ([90ff435](https://github.com/mggarofalo/gas/commit/90ff4351631ef8c0af7163b54a47439ec131ee40))
* separate token endpoint, suppress log noise, stop 401 retry loop (GAS-71, GAS-72) ([#74](https://github.com/mggarofalo/gas/issues/74)) ([e6c8606](https://github.com/mggarofalo/gas/commit/e6c8606db8e66e391518fc82d962f45b2d303127))
* serve React SPA from wwwroot with fallback routing ([2c4087f](https://github.com/mggarofalo/gas/commit/2c4087ff877288729804e12e3e75657a209036ea))
* serve React SPA from wwwroot with fallback routing ([b8ccfa2](https://github.com/mggarofalo/gas/commit/b8ccfa217da77f80165c775a392ef1bbe10df39b))
* stop admin seeder from resetting password on every restart ([961bf2f](https://github.com/mggarofalo/gas/commit/961bf2f6be544c0dee8de2816195303df99c7280))
* stop admin seeder from resetting password on every restart ([8459952](https://github.com/mggarofalo/gas/commit/8459952a0db8164c869dc233a63d5ea910b2dbb7))
* strip parens from memos, auto-calculate gallons from amount/price ([#88](https://github.com/mggarofalo/gas/issues/88)) ([59018a4](https://github.com/mggarofalo/gas/commit/59018a4c2fdcd92839b32c344b2ec8cafac0e84c))


### Performance Improvements

* lazy-load all route components ([a106aec](https://github.com/mggarofalo/gas/commit/a106aec4e373af8c7a59f9c815ce7b27ed140872))
* lazy-load all route components ([62bbd1b](https://github.com/mggarofalo/gas/commit/62bbd1b0f45e706e4781cc879e73b4a61885aab2))

## [1.23.3](https://github.com/mggarofalo/gas/compare/v1.23.2...v1.23.3) (2026-04-03)


### Bug Fixes

* frontend gallons calculation + parser ordering tests ([#92](https://github.com/mggarofalo/gas/issues/92)) ([818eb88](https://github.com/mggarofalo/gas/commit/818eb888df8ba114476b6e516794f62b86036745))

## [1.23.2](https://github.com/mggarofalo/gas/compare/v1.23.1...v1.23.2) (2026-04-03)


### Bug Fixes

* calculate gallons in import list projection when DB value is null ([#90](https://github.com/mggarofalo/gas/issues/90)) ([490d097](https://github.com/mggarofalo/gas/commit/490d0976c28adc77cb79aa571f1e4af402aa046f))

## [1.23.1](https://github.com/mggarofalo/gas/compare/v1.23.0...v1.23.1) (2026-04-03)


### Bug Fixes

* strip parens from memos, auto-calculate gallons from amount/price ([#88](https://github.com/mggarofalo/gas/issues/88)) ([59018a4](https://github.com/mggarofalo/gas/commit/59018a4c2fdcd92839b32c344b2ec8cafac0e84c))

## [1.23.0](https://github.com/mggarofalo/gas/compare/v1.22.1...v1.23.0) (2026-04-03)


### Features

* flexible memo parser, auto-apply mappings on list, validation icons ([#86](https://github.com/mggarofalo/gas/issues/86)) ([c0f22ff](https://github.com/mggarofalo/gas/commit/c0f22ff0b356ecd3e44ab8aba3fcc2538595566b))

## [1.22.1](https://github.com/mggarofalo/gas/compare/v1.22.0...v1.22.1) (2026-04-03)


### Bug Fixes

* retroactively apply vehicle memo mappings to pending imports ([#84](https://github.com/mggarofalo/gas/issues/84)) ([90ff435](https://github.com/mggarofalo/gas/commit/90ff4351631ef8c0af7163b54a47439ec131ee40))

## [1.22.0](https://github.com/mggarofalo/gas/compare/v1.21.1...v1.22.0) (2026-04-03)


### Features

* add vehicle memo mappings UI to import queue page ([#82](https://github.com/mggarofalo/gas/issues/82)) ([c7788cd](https://github.com/mggarofalo/gas/commit/c7788cdad0fc6efb86884c5090737da14a388a61))

## [1.21.1](https://github.com/mggarofalo/gas/compare/v1.21.0...v1.21.1) (2026-04-03)


### Bug Fixes

* increase YNAB HttpClient timeout for large plan fetches ([#80](https://github.com/mggarofalo/gas/issues/80)) ([4dfbaa0](https://github.com/mggarofalo/gas/commit/4dfbaa001917756267a1ed13be2842b8046b9935))

## [1.21.0](https://github.com/mggarofalo/gas/compare/v1.20.0...v1.21.0) (2026-04-03)


### Features

* add pull sync reset to clear server_knowledge and stale imports ([#78](https://github.com/mggarofalo/gas/issues/78)) ([739ff5f](https://github.com/mggarofalo/gas/commit/739ff5ff5fec2931a09a6db24cbbe55bb11f792b))

## [1.20.0](https://github.com/mggarofalo/gas/compare/v1.19.1...v1.20.0) (2026-04-03)


### Features

* per-fill-up YNAB account and category selection ([#76](https://github.com/mggarofalo/gas/issues/76)) ([2fa7797](https://github.com/mggarofalo/gas/commit/2fa7797d745a6ae7e45a362526495178c3b31dea))

## [1.19.1](https://github.com/mggarofalo/gas/compare/v1.19.0...v1.19.1) (2026-04-03)


### Bug Fixes

* separate token endpoint, suppress log noise, stop 401 retry loop (GAS-71, GAS-72) ([#74](https://github.com/mggarofalo/gas/issues/74)) ([e6c8606](https://github.com/mggarofalo/gas/commit/e6c8606db8e66e391518fc82d962f45b2d303127))

## [1.19.0](https://github.com/mggarofalo/gas/compare/v1.18.1...v1.19.0) (2026-04-03)


### Features

* add YNAB two-way sync with import review queue ([#72](https://github.com/mggarofalo/gas/issues/72)) ([3a87f77](https://github.com/mggarofalo/gas/commit/3a87f77918f2d27662aee380327d5fe31cc8b14e))
* YNAB API-driven backfill and per-fill-up account selection ([#70](https://github.com/mggarofalo/gas/issues/70)) ([f65c357](https://github.com/mggarofalo/gas/commit/f65c357b28327cc62ed2cd67a6329b3b7ac85fc9))

## [1.18.1](https://github.com/mggarofalo/gas/compare/v1.18.0...v1.18.1) (2026-04-03)


### Bug Fixes

* add writable dp-keys volume for Data Protection key persistence ([#66](https://github.com/mggarofalo/gas/issues/66)) ([4b54841](https://github.com/mggarofalo/gas/commit/4b54841aa482944863d8bd51aba70cba92cfe735))
* mount dp-keys volume outside read-only /secrets ([#69](https://github.com/mggarofalo/gas/issues/69)) ([7b6226b](https://github.com/mggarofalo/gas/commit/7b6226bc440e84cfc35b9b6903ab0c6795a4eaf2))

## [1.18.0](https://github.com/mggarofalo/gas/compare/v1.17.0...v1.18.0) (2026-04-02)


### Features

* add Paperless sync status UI to fill-up pages ([#63](https://github.com/mggarofalo/gas/issues/63)) ([e7144f0](https://github.com/mggarofalo/gas/commit/e7144f0805787643f3ef570ab691dc8ea6997af7))

## [1.17.0](https://github.com/mggarofalo/gas/compare/v1.16.0...v1.17.0) (2026-04-02)


### Features

* add Paperless-ngx background sync service ([#61](https://github.com/mggarofalo/gas/issues/61)) ([ba7cf75](https://github.com/mggarofalo/gas/commit/ba7cf75d90c9a972bce1561e54bf70eebf83fedf))

## [1.16.0](https://github.com/mggarofalo/gas/compare/v1.15.1...v1.16.0) (2026-04-02)


### Features

* implement Paperless-ngx API client for document upload ([#59](https://github.com/mggarofalo/gas/issues/59)) ([e0e2dc7](https://github.com/mggarofalo/gas/commit/e0e2dc7e2c942d3f8cf2aa0480bf022ff98337f5))

## [1.15.1](https://github.com/mggarofalo/gas/compare/v1.15.0...v1.15.1) (2026-04-02)


### Bug Fixes

* resolve all build warnings and enable TreatWarningsAsErrors ([#57](https://github.com/mggarofalo/gas/issues/57)) ([b0f42e0](https://github.com/mggarofalo/gas/commit/b0f42e04c3dbe3f8c5cdea9492af23bb9c02d41b))

## [1.15.0](https://github.com/mggarofalo/gas/compare/v1.14.0...v1.15.0) (2026-04-02)


### Features

* add YNAB settings page with cascading dropdowns ([#55](https://github.com/mggarofalo/gas/issues/55)) ([dd24a18](https://github.com/mggarofalo/gas/commit/dd24a18c382eaaf4029f53b3e4c811ae787edb34))

## [1.14.0](https://github.com/mggarofalo/gas/compare/v1.13.0...v1.14.0) (2026-04-02)


### Features

* implement YNAB API client and proxy endpoints ([#52](https://github.com/mggarofalo/gas/issues/52)) ([981c6b0](https://github.com/mggarofalo/gas/commit/981c6b0d5ecc50ff6ebc5ba614a107054494bb62))
* push fill-up transactions to YNAB on creation ([#53](https://github.com/mggarofalo/gas/issues/53)) ([cbd6238](https://github.com/mggarofalo/gas/commit/cbd6238ca02bd021e8ddabb7319b04c16946eead))

## [1.13.0](https://github.com/mggarofalo/gas/compare/v1.12.0...v1.13.0) (2026-04-01)


### Features

* add YNAB settings table and configuration API ([#50](https://github.com/mggarofalo/gas/issues/50)) ([5f012dc](https://github.com/mggarofalo/gas/commit/5f012dcc25b551bf1b160fd494001d4b86bede0d))

## [1.12.0](https://github.com/mggarofalo/gas/compare/v1.11.0...v1.12.0) (2026-04-01)


### Features

* add YNAB CSV ingest endpoint for historical fill-ups ([#48](https://github.com/mggarofalo/gas/issues/48)) ([991cdf5](https://github.com/mggarofalo/gas/commit/991cdf5b11eea96acc0194bcd06c2031521e8881))

## [1.11.0](https://github.com/mggarofalo/gas/compare/v1.10.0...v1.11.0) (2026-04-01)


### Features

* add octane rating to vehicles and fill-ups ([#46](https://github.com/mggarofalo/gas/issues/46)) ([e92e36b](https://github.com/mggarofalo/gas/commit/e92e36b86e6d1073c54e8383ed7cf370237be4ae))

## [1.10.0](https://github.com/mggarofalo/gas/compare/v1.9.0...v1.10.0) (2026-04-01)


### Features

* add PostgreSQL backup script with 7-day rotation ([#44](https://github.com/mggarofalo/gas/issues/44)) ([8509367](https://github.com/mggarofalo/gas/commit/850936759d7c4c5b223a83a7eb99caab68dd73ac))

## [1.9.0](https://github.com/mggarofalo/gas/compare/v1.8.4...v1.9.0) (2026-04-01)


### Features

* add health check endpoint with DB and MinIO validation ([#41](https://github.com/mggarofalo/gas/issues/41)) ([427024e](https://github.com/mggarofalo/gas/commit/427024eb64507086e67fdc222a9837e40b795453))

## [1.8.4](https://github.com/mggarofalo/gas/compare/v1.8.3...v1.8.4) (2026-03-31)


### Bug Fixes

* limit gallons and price inputs to 999.999 ([6e81b98](https://github.com/mggarofalo/gas/commit/6e81b98be4afd408be50cf77a96c52c88a6dfd16))
* limit gallons and price to 999.999 ([5788ff7](https://github.com/mggarofalo/gas/commit/5788ff72dea5ab2d161575554043245f8e3fce92))

## [1.8.3](https://github.com/mggarofalo/gas/compare/v1.8.2...v1.8.3) (2026-03-31)


### Bug Fixes

* iOS Safari geolocation, document scanner, and Bitwarden suppression ([acb222a](https://github.com/mggarofalo/gas/commit/acb222a25da532e73eb3620111da121ce69ac034))
* iOS Safari geolocation, document scanner, and Bitwarden suppression ([b218cf0](https://github.com/mggarofalo/gas/commit/b218cf09dc8c0bde87c4586f12bde53e20fcdf0c))

## [1.8.2](https://github.com/mggarofalo/gas/compare/v1.8.1...v1.8.2) (2026-03-31)


### Bug Fixes

* GPS requires user gesture, receipt shows iOS scanner, suppress password managers ([7e66546](https://github.com/mggarofalo/gas/commit/7e66546ebd3834d50082898afb1072656e03b4a3))
* GPS user gesture, iOS scanner, password manager suppression ([c48ece4](https://github.com/mggarofalo/gas/commit/c48ece4a20f5bd512c3d42b14315889a69bd793a))

## [1.8.1](https://github.com/mggarofalo/gas/compare/v1.8.0...v1.8.1) (2026-03-31)


### Bug Fixes

* replace numeric spinner inputs with formatted text inputs ([d8a25f2](https://github.com/mggarofalo/gas/commit/d8a25f220f080f75d09e54f9f33c10931e2b441f))
* replace numeric spinner inputs with formatted text inputs ([9109c43](https://github.com/mggarofalo/gas/commit/9109c43bf34f2fbdf6a3244b3bdcc7b66396a0e7))

## [1.8.0](https://github.com/mggarofalo/gas/compare/v1.7.1...v1.8.0) (2026-03-31)


### Features

* show app version in sidebar footer ([7cac3e7](https://github.com/mggarofalo/gas/commit/7cac3e71cedae3b9cfdd3978b9d583421d809899))
* show app version in sidebar footer ([a82f3eb](https://github.com/mggarofalo/gas/commit/a82f3eb5148d806e5237b7538c29720ced51e2ee))

## [1.7.1](https://github.com/mggarofalo/gas/compare/v1.7.0...v1.7.1) (2026-03-31)


### Bug Fixes

* GPS button shows state inline, remove lat/lng display, clamp decimals ([c1fa165](https://github.com/mggarofalo/gas/commit/c1fa16591ac5106ec20638ab59e9698f65ca5e06))
* GPS button state indicator, remove lat/lng display, clamp decimals ([993f937](https://github.com/mggarofalo/gas/commit/993f937e09847c04a5c01250f4a14bc6d3d4933e))

## [1.7.0](https://github.com/mggarofalo/gas/compare/v1.6.0...v1.7.0) (2026-03-31)


### Features

* receipt upload triggers iOS document scanner ([7d44872](https://github.com/mggarofalo/gas/commit/7d448721563b79cb5e8d1ef0a7b1075cc0c3b29a))
* receipt upload triggers iOS document scanner ([9024201](https://github.com/mggarofalo/gas/commit/9024201ce8aa2f6a05cef12163e2178795d50f11))

## [1.6.0](https://github.com/mggarofalo/gas/compare/v1.5.0...v1.6.0) (2026-03-31)


### Features

* reorder fill-up form and add station autocomplete ([a07cded](https://github.com/mggarofalo/gas/commit/a07cdedb426dcc9c950f4b1dc85ac1499d5b508b))
* reorder fill-up form, add station autocomplete from history ([83c4957](https://github.com/mggarofalo/gas/commit/83c49573c84f6147ce1e3622f4e8d1f6e63d1bbb))

## [1.5.0](https://github.com/mggarofalo/gas/compare/v1.4.1...v1.5.0) (2026-03-31)


### Features

* mobile-first responsive design and form polish ([5c44943](https://github.com/mggarofalo/gas/commit/5c44943635194e799d003dba5c272dc70d997e24))
* mobile-first responsive design and form polish ([350dfdd](https://github.com/mggarofalo/gas/commit/350dfdd301626cda459e5405c8ebafc2298aadcc))

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
