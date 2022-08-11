

# Connector Unity

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/user/unity.html)



## Introduction

This repo holds Speckle's Unity Connector + a sample project (Speckle playground). This connector is currently in an Alpha stage.

This connector is meant to be used by developers, it doesn't have an elaborated UI but it offers convenience methods to send and receive data. The connector uses our [Speckle .NET SDK](https://github.com/specklesystems/speckle-sharp).

![unity](https://user-images.githubusercontent.com/2679513/108543628-3a83ff00-72dd-11eb-8792-3d43ce54e6af.gif)

Checkout our dedicated [Tutorials and Docs](https://speckle.systems/tag/unreal/).

If you are enjoying using Speckle, don't forget to ⭐ our [GitHub repositories](https://github.com/specklesystems),
and [join our community forum](https://speckle.community/) where you can post any questions, suggestions, and discuss exciting projects!

## Notice
We support Unity 2020.3, 2021.3. (newer versions likley work, but aren't part of our test pipeline)


Features:
 - Recieve Speckle Objects at Editor or Runtime
 - Send Speckle Objects at Runtime (editor support in the works!)
 - Material override/subsitution
 - Automatic receiving changes
 
Currently tested on Windows and MacOS. Experimental support for Android [in the works](https://github.com/specklesystems/speckle-unity/issues/68).


## Installation

To install the connector into your own Unity project (rather than using sample project), open the Package Manager (`Windows -> Package Manager`)
and select **Add Package from git URL**. (requires [git](https://git-scm.com/downloads) installed)

<p align="center"><img src="https://github.com/specklesystems/speckle-docs/blob/main/user/img-unity/unity_install_git.png" width="25%" /></p>

Paste in the following URL
```
https://github.com/specklesystems/speckle-unity.git?path=/Packages/systems.speckle.speckle-unity
```

---

We encourage everyone interested to hack / contribute / debug / give feedback to this project.


### Requirements

- Unity (we're currently testing with 2020+)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)

### Getting Started 🏁

Following instructions on how to get started debugging and contributing to this connector.



### Dependencies

All dependencies to Speckle Core have been included compiled in the Asset folder until we figure out how to best reference Core.


## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.


## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).

