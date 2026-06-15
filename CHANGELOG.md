# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="1.2.0"></a>
## [1.2.0](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.2.0) (2026-06-15)

### Features

* **AI:** Add agents files ([ec0feb9](https://www.github.com/jeffu231/AmateurRadioServices/commit/ec0feb91b2289c6f670c04ac420a4d138c07a553))
* **Versioning:** Add Configuration endpoint to expose the service version ([6144037](https://www.github.com/jeffu231/AmateurRadioServices/commit/614403775098c2cdb2eb4461e1e91ea3587a88d7))

<a name="1.1.2"></a>
## [1.1.2](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.1.2) (2026-06-15)

### Bug Fixes

* Resolve aprs.fi from post to get ([ad6ac05](https://www.github.com/jeffu231/AmateurRadioServices/commit/ad6ac053a9d129b900b62b05c84f0a201c8c6390))
* **qrz:** support suffix callsigns with query lookup ([25c2984](https://www.github.com/jeffu231/AmateurRadioServices/commit/25c2984d2e6e600b89eb335605d39ad8b4bdd3d4))

<a name="1.1.1"></a>
## [1.1.1](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.1.1) (2026-06-05)

### Bug Fixes

* Add logic to strip /R and /P from qrz lookups ([fc8bb48](https://www.github.com/jeffu231/AmateurRadioServices/commit/fc8bb484eac1630b560c42981f4208e0573ed18d))

<a name="1.1.0"></a>
## [1.1.0](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.1.0) (2026-05-20)

### Features

* Add ability to get the sub expire date time from api call ([8d4919c](https://www.github.com/jeffu231/AmateurRadioServices/commit/8d4919cca72419d48c13fcb9240fb98e2d4b05f1))

### Bug Fixes

* Improve error handling parsing call info ([a801af0](https://www.github.com/jeffu231/AmateurRadioServices/commit/a801af043b630862ebad51689a23ddfa445fc352))

### Continuous Integration

* Update action versions and add global.json for dotnet version ([b193357](https://www.github.com/jeffu231/AmateurRadioServices/commit/b193357be7d79728670b483d81a2b4ba57597a89))

<a name="1.0.5"></a>
## [1.0.5](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.0.5) (2026-02-14)

### Bug Fixes

* add problem details service for exception handling ([e7e4b2a](https://www.github.com/jeffu231/AmateurRadioServices/commit/e7e4b2a12722b0bc6f95561223eb3bb0e504d2d9))
* bump maindenheadlib to latest ([21a53c3](https://www.github.com/jeffu231/AmateurRadioServices/commit/21a53c38d43b4a32039c4bde404333974e31f298))
* bump to net 10 ([d608f5d](https://www.github.com/jeffu231/AmateurRadioServices/commit/d608f5de6d7246307efdc7948e60ca9c389658df))
* ensure proper Produces for swagger doc ([4421942](https://www.github.com/jeffu231/AmateurRadioServices/commit/44219427d4e1657d46308605819c12290dd8797c))
* fix expose port in docker file ([24eade5](https://www.github.com/jeffu231/AmateurRadioServices/commit/24eade573630f8be4d3dd9503afb8a0d2e387a11))
* initialize ContactInfo fields with empty ([4fea8ea](https://www.github.com/jeffu231/AmateurRadioServices/commit/4fea8ea622d477110bb6431e6c0f51615126e9d6))
* replace newtonsoft json with system.text.json ([baf368c](https://www.github.com/jeffu231/AmateurRadioServices/commit/baf368c977ddc7fee91a98524c3325df011f5fd7))
* update json converter for number types in strings ([ee9f159](https://www.github.com/jeffu231/AmateurRadioServices/commit/ee9f1598f29b939a33e02d84b35a199d0d60ba6a))
* update swagger libs and refactor config ([0d7ac1d](https://www.github.com/jeffu231/AmateurRadioServices/commit/0d7ac1deb749804a7071efb3750a8cc3c45871e3))
* upgrade api versioning to new library ([c5fccc5](https://www.github.com/jeffu231/AmateurRadioServices/commit/c5fccc58f682c6e8790f6435e0089e6f972a08e3))

### Continuous Integration

* bump publish and runner net to 10.0 ([cd8779f](https://www.github.com/jeffu231/AmateurRadioServices/commit/cd8779f161a0de968439936e7456c7c7eede5409))
* fix dockerfile path ([98267c3](https://www.github.com/jeffu231/AmateurRadioServices/commit/98267c36f11a77a43ee7e0480fc43c50f4b42564))
* ignore more user files ([b400808](https://www.github.com/jeffu231/AmateurRadioServices/commit/b400808ce6383629943fa873e5d29edc37119ab9))

<a name="1.0.4"></a>
## [1.0.4](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.0.4) (2026-01-19)

### Bug Fixes

* update logging format to json ([8eb6a9d](https://www.github.com/jeffu231/AmateurRadioServices/commit/8eb6a9d850864edbf25f69878711c300e0f94ad5))

### Continuous Integration

* Add log and build keywords to commit linter ([ae1ace0](https://www.github.com/jeffu231/AmateurRadioServices/commit/ae1ace0469e86529f887c901c1636306203fdcae))
* update versionize config to not include all commits ([d1dd441](https://www.github.com/jeffu231/AmateurRadioServices/commit/d1dd44110f1573a7008702f841cdb7129356adeb))

<a name="1.0.3"></a>
## [1.0.3](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.0.3) (2025-06-18)

### Documentation

* Update the QRZ service method doc ([bca6ce6](https://www.github.com/jeffu231/AmateurRadioServices/commit/bca6ce612ec3c455f2e7278b560e1fab17e62b37))

### Continuous Integration

* Add versionize config file ([a46a9f3](https://www.github.com/jeffu231/AmateurRadioServices/commit/a46a9f3a614543f804b95eeb15f9f404dcbfbf27))

<a name="1.0.2"></a>
## [1.0.2](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.0.2) (2025-06-18)

<a name="1.0.1"></a>
## [1.0.1](https://www.github.com/jeffu231/AmateurRadioServices/releases/tag/v1.0.1) (2025-06-18)

