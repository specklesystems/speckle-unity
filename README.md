
## Installation


# Connector Unity

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/user/unity.html)


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
https://github.com/constructobot/speckle-unity.git?path=/Packages/systems.speckle.speckle-unity
```