

# Connector Unity

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Community forum users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square&logo=discourse&logoColor=white)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/https://-speckle.systems-royalblue?style=flat-square)](https://speckle.systems) [![docs](https://img.shields.io/badge/docs-speckle.guide-orange?style=flat-square&logo=read-the-docs&logoColor=white)](https://speckle.guide/dev/)



## Introduction

This repo holds Speckle's Unity Connector, it's currently released as early alpha.

This connector is meant to be used by developers, it doesn't have an elaborated UI but it offers convenience methods to send and receive data. The connector uses our [Speckle .NET SDK](https://github.com/specklesystems/speckle-sharp).



![unity](https://user-images.githubusercontent.com/2679513/108543628-3a83ff00-72dd-11eb-8792-3d43ce54e6af.gif)



## Documentation

More comprehensive developer documentation can be found in the [Speckle Docs website](https://speckle.guide/dev/).



## Developing & Debugging

We encourage everyone interested to debug / hack /contribute / give feedback to this project.

### Requirements

- Unity (we're currently testing with 2020+)
- A Speckle Server running (more on this below)
- Speckle Manager (more on this below)



### Dependencies

All dependencies to Speckle Core have been included compiled in the Asset folder until we figure out how to best reference Core.

The GraphQL library has been recompiled with a fix for Unity, see https://github.com/graphql-dotnet/graphql-client/issues/318 for more info.



### Getting Started 🏁

Following instructions on how to get started debugging and contributing to this connector.


#### Server

In order to test Speckle in all its glory you'll need a server running, you can run a local one by simply following these instructions:

- https://github.com/specklesystems/Server

If you're facing any errors make sure Postgress and Redis are up and running. 

#### Accounts

The connector itself doesn't have features to manage your Speckle accounts, this functionality has been delegated to the Speckle Manager desktop app.

You can install an alpha version of it from: [https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe](https://speckle-releases.ams3.digitaloceanspaces.com/manager/SpeckleManager%20Setup.exe)

After installing it, you can use it to add/create an account on the Server.



### Debugging

Open your IDE and click "Attach to Unity and Debug".



### Questions and Feedback 💬

Hey, this is work in progress, I'm sure you'll have plenty of feedback, and we want to hear all about it! Get in touch with us on [the forum](https://discourse.speckle.works)! 



## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.



## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself!



## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).

