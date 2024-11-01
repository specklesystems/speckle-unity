<h1 align="center">
  <img src="https://user-images.githubusercontent.com/2679513/131189167-18ea5fe1-c578-47f6-9785-3748178e4312.png" width="150px"/><br/>
  Speckle | Unity
</h1>

<p align="center"><a href="https://twitter.com/SpeckleSystems"><img src="https://img.shields.io/twitter/follow/SpeckleSystems?style=social" alt="Twitter Follow"></a> <a href="https://speckle.community"><img src="https://img.shields.io/discourse/users?server=https%3A%2F%2Fspeckle.community&amp;style=flat-square&amp;logo=discourse&amp;logoColor=white" alt="Community forum users"></a> <a href="https://speckle.systems"><img src="https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square" alt="website"></a> <a href="https://speckle.guide/dev/"><img src="https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&amp;logo=read-the-docs&amp;logoColor=white" alt="docs"></a></p>

> Speckle is the first AEC data hub that connects with your favorite AEC tools. Speckle exists to overcome the challenges of working in a fragmented industry where communication, creative workflows, and the exchange of data are often hindered by siloed software and processes. It is here to make the industry better.

<h3 align="center">
    Speckle Connector for Unity
</h3>

> [!WARNING]
> This is a legacy repo! A new next generation connector will be coming soon. In the meantime, check out our active next generation repos here 👇<br/>
> [`speckle-sharp-connectors`](https://github.com/specklesystems/speckle-sharp-connectors): our .NET next generation connectors and desktop UI<br/>
> [`speckle-sharp-sdk`](https://github.com/specklesystems/speckle-sharp-sdk): our .NET SDK, Tests, and Objects


## Introduction


This repo holds Speckle's Unity Connector package + a sample project (Speckle playground).

The package offers several Unity Components to send and receive data from Speckle, and allows developers to easily develop their own components and features.
It has a simple UI, and is missing some of the comforts present in other connectors.
The connector uses our [Speckle .NET SDK](https://github.com/specklesystems/speckle-sharp).

![unity](https://user-images.githubusercontent.com/2679513/108543628-3a83ff00-72dd-11eb-8792-3d43ce54e6af.gif)

Checkout our dedicated [Tutorials and Docs](https://speckle.systems/tag/unity/).

If you are enjoying using Speckle, don't forget to ⭐ our [GitHub repositories](https://github.com/specklesystems),
and [join our community forum](https://speckle.community/) where you can post any questions, suggestions, and discuss exciting projects!

## Notice
We officially support Unity 2021.3 or newer.

Features:
 - Receive Speckle Objects at Editor or Runtime
 - Send Speckle Objects at Editor or Runtime
 - Material override/substitution
 - Automatic receiving changes
 
Currently tested on Windows, Linux, and MacOS.

Android will work [with some signficant limitations](https://github.com/specklesystems/speckle-unity/issues/68), and other platforms likly work with similar limitations.

## Sample Project
This repo holds a simple sample project (Speckle Playground), containing an example GUI (UnityUI) for fetching stream/branch data, and sending/receiving geometry to/from Speckle.

Simply [download this repo](https://github.com/specklesystems/speckle-unity/archive/refs/heads/main.zip)
or clone with git, and open in Unity 2021.3 or newer.
```
git clone https://github.com/specklesystems/speckle-unity.git
```

## Installation (Package)

To install the connector into your own Unity project (rather than using the sample project), open the Package Manager (`Windows -> Package Manager`)
and select **Add Package from git URL**. (requires [git](https://git-scm.com/downloads) installed)

<p align="center"><img src="https://github.com/specklesystems/speckle-docs/blob/main/user/img-unity/unity_install_git.png" width="25%" /></p>

Paste in the following URL
```
https://github.com/specklesystems/speckle-unity.git?path=/Packages/systems.speckle.speckle-unity
```

Checkout [our docs for getting started instructions](https://speckle.guide/user/unity.html#getting-started)
---

We encourage everyone interested to hack / contribute / debug / give feedback to this project.


### Requirements

- Unity 2021 or greater
- Have created an account on [app.speckle.systems](https://app.speckle.systems) (or your own server)
- Installed [Speckle Manager](https://speckle.guide/user/manager.html) (recommended, otherwise you'll need to implement your own authentication system in Unity)

### Dependencies

All dependencies to Speckle Core have been included; compiled in `systems.speckle.speckle-unity` package.


## Contributing

Please make sure you read the [contribution guidelines](https://github.com/specklesystems/speckle-sharp/blob/main/.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.


## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).

