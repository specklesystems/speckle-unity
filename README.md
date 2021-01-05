

# Connector Unity WIP

[![Twitter Follow](https://img.shields.io/twitter/follow/SpeckleSystems?style=social)](https://twitter.com/SpeckleSystems) [![Discourse users](https://img.shields.io/discourse/users?server=https%3A%2F%2Fdiscourse.speckle.works&style=flat-square)](https://discourse.speckle.works) [![website](https://img.shields.io/badge/www-speckle.systems-royalblue?style=flat-square)](https://speckle.systems)



## Introduction

This repo holds Speckle's Unity Connector, it currently is ⚠ **WORK IN PROGRESS** ⚠, please use at your own risk!

This connector is meant to be used by developers, it doesn't have a UI but it offers convenience methods to send and receive data. The connector currently only supports a subset of all the features and methods in the [Speckle .NET SDK](https://github.com/specklesystems/speckle-sharp).



![unity](https://user-images.githubusercontent.com/2679513/103669743-a3ebc080-4f70-11eb-8248-dfee18395679.gif)





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



## How to use

The project showcases how to send and receive data with Speckle in Unity. 

In order to run it make sure to first **add an account via the Speckle manager.**

### Accounts

#### Default account

If you only add one account in the Manager, **that will also be your default account**. If you have multiple accounts, you can **switch default account using the Manager**.

![image](https://user-images.githubusercontent.com/2679513/97778543-c4159280-1b6f-11eb-924e-04b3fb1ed3e0.png)



Some nodes accept an optional "account" input, **if not provided the default account will be used.**

![image-20201031111912748](https://user-images.githubusercontent.com/2679513/97778555-da235300-1b6f-11eb-9c24-aa50908fcacf.png)



### Sending and Receiving

This connector is meant to be used by developers, it doesn't have a UI but it offers convenience methods to send and receive data. The connector currently only supports a subset of all the features in the [Speckle .NET SDK](https://github.com/specklesystems/speckle-sharp).

#### Sending

To Send data simply call the `Sender.Send(string streamId, List<GameObject> gameObjects, Account account = null)` method, it's on a static class so you don't need to initialize it.

**Currently only GameObjects with a `MeshFilter` on them can be sent.**

#### Receiving

To Receive data instantiate a `Receiver.cs` like so:

```c#
var receiver = ScriptableObject.CreateInstance<Receiver>();
receiver.Init(ReceiveText.text);
```

To receive the data on its last commit and convert it automatically call `await receiver.Receive();` .

To automatically receive all new data being sent to the stream, subscribe to new data events like so:

```c#
receiver.OnNewData += ReceiverOnNewData;
...
...
private void ReceiverOnNewData(GameObject go)
{
   ...
}
```

The received GameObject will contain as children a flattened list of all the converted objects.

**Currently only Mesh, Lines and points can be received.**



### Questions and Feedback 💬

Hey, this is work in progress, I'm sure you'll have plenty of feedback, and we want to hear all about it! Get in touch with us on [the forum](https://discourse.speckle.works)! 



## Contributing

Please make sure you read the [contribution guidelines](.github/CONTRIBUTING.md) for an overview of the best practices we try to follow.



## Community

The Speckle Community hangs out on [the forum](https://discourse.speckle.works), do join and introduce yourself!



## License

Unless otherwise described, the code in this repository is licensed under the Apache-2.0 License. Please note that some modules, extensions or code herein might be otherwise licensed. This is indicated either in the root of the containing folder under a different license file, or in the respective file's header. If you have any questions, don't hesitate to get in touch with us via [email](mailto:hello@speckle.systems).

