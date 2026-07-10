```
  ____    _    ____ _  ___     ___ _____ 
 | __ )  / \  / ___| |/ / |   / _ \_   _|
 |  _ \ / _ \| |   | ' /| |  | | | || |  
 | |_) / ___ \ |___| . \| |__| |_| || |_ 
 |____/_/   \_\____|_|\_\_____\___/ |_(_)                                                              
```

### Build .NET apps faster, with less code

Backlot is an opinionated framework on top of .NET 10 for building API-centric web and back-end systems. It is powered by the Movie Pattern Practice (MPP), an architectural approach designed to close the gap between describing software and building it. Backlot enables teams to collaborate in a clear, uniform, and agile way by providing a consistent language across every project.

### The Movie Pattern Practice

MPP simplifies development into four distinct concepts, ensuring code that reads like a screenplay:

- **Roles**: What your code can do. Concise and reusable interfaces acting as abstractions for underlying objects.
- **Scenarios**: What your code actually does. Concrete SOLID code pieces that implement business logic and encapsulate behavior.
- **Watchers**: React to what happened. Components that observe scenarios and their events to trigger follow-up actions.
- **Director**: Orchestrate execution. A central component to manage executions across different hosts, from web apps to serverless functions.

### Building Blocks

Backlot comes with default blocks included but follows SOLID principles so you can swap:
- **Core**: The foundational engine.
- **Authentication**: Built-in providers for Auth0, Basic, and custom implementations.
- **Persistence**: Persist entities in a flexible schema, with Support for RavenDB, LiteDB, and SQL.
- **Storage**: Flexible providers for filesystem or cloud storage.
- **Hosts**: Support for ASP.NET, Azure Functions, and Console apps.

### Installation

To install Backlot in your project via NuGet, use the following dotnet CLI command:

```bash
dotnet add package Backlot.Defaults
```

This package includes the opinionated defaults to get you up and running quickly. For further instructions, visit https://docs.backlot.software.

### Running Demos

The repository includes three available demos to help you explore the framework:

- **Backlot.Demo.Console**: Demonstrates core Role and Scenario usage in a terminal environment.
- **Backlot.Demo.Web**: Shows integration with ASP.NET Core, including middleware and API endpoints.
- **Backlot.Demo.Azure**: Illustrates hosting scenarios within Azure Functions (Isolated Worker model).

Both the azure and web demos require a usersandgroups.json file. By default this is gitignored. The structure is like this:

```json
{
  "Users": [
    {
      "Id": "admin@testmail.backlot.software",
      "SystemAdmin": true,
      "Settings" : {
        "pw": "<pwhash>=optional, only needed if you want to use the built in basic auth provider"
      }
    }
  ],
  "Groups": [
    {
      "Id": "Admin",
      "Users": [
      ]
    },
    {
      "Id": "Users",
      "Users": [
        "...@testmail.backlot.software"
      ]
    }
  ]
}
```

**Prerequisites:**
- .NET 10 SDK
- For the Azure demo: Azure Functions Core Tools and Azurite (or an active Azure Storage account).
- A C# IDE such as JetBrains Rider or Visual Studio.

### Contributions

Contributing to Backlot requires signing a Contributor License Agreement (CLA). This ensures that the project remains open and protected for the entire community. We look forward to your pull requests.

### Documentation

Official documentation, including tutorials and API guides, can be found at:
https://docs.backlot.software

# Licensing

The Backlot Framework is available under **two licenses**:
- **AGPL-3.0** (open-source).
- **Commercial License** (proprietary).

## Open source

The AGPL-3.0 is a copyleft license, which means that any derivative work must also be licensed under the same terms. If you cannot comply with the AGPL-3.0 terms a **commercial license** is necessary.
Most frequently asked questions are answered in our [LICENSE-FAQ](LICENSE-FAQ.md).

## Commercial license

Commercial licenses are available via our partner network:
[Growcreate Netherlands for Worldwide and European customers](https://growcreate.nl/contact/)
[Growcreate UK for UK customers](https://growcreate.co.uk/contact/)

## Contributions

We can only accept contributions after our [CLA](CLA.MD) is signed.
