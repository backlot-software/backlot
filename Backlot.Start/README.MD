```
  ____    _    ____ _  ___     ___ _____ 
 | __ )  / \  / ___| |/ / |   / _ \_   _|
 |  _ \ / _ \| |   | ' /| |  | | | || |  
 | |_) / ___ \ |___| . \| |__| |_| || |_ 
 |____/_/   \_\____|_|\_\_____\___/ |_(_)                                                              
```
# Backlot.Start

Backlot.Start is a starter template for building web apis with Backlot.Web.
If you like to use Backlot in other applications like a console application or your existing CMS installation. Start with using Backlot.Core and scaleup with the packages you need.

## WebDirector.cs

The Director coordinates the entire application. This role involves envisioning, instructing, and unifying various parts to create a seamless performance.
A default web director is added to you solution and configured with the defaults. It's important you change the SecretKey f.e. with a guid. For production purposes we advice you to make this SecretKey configurable in an Azure Keyvault or something similair. 

## Configuration

**env.jsonsettings.json**
The prefix of the jsonsettings.json has to match the environment name.
In below example this is 'local' and therefor the local.jsonsettings.json is used.

```csharp
new JsonSettingsManager("local", fs),
```

If you add secrets to it, make sure to .gitignore this file. It is however advisable to use Azure keyvault or something similair for sensitive information.

## Logging

We advice you to use Serilog with Seq. Within Program.cs we added some optional configuration for that.
Since we use default Microsoft.Extensions.Logging you can configure logging the way you want.

## Authentication

The Backlot.Start is using Backlot.Authentication.Basic. We advice you to use a different authentication provider for production environments (like Auth0 or Authentication.BuiltIn).
For Basic Authentication the usersandgroups.json need to contain a hashed version of a password. We also advice you to ignore this file in your .gitignore.

Example:

```json
{
  "Users": [
    {
      "Id": "admin@testmail.backlot.software",
      "SystemAdmin": true,
      "Settings" : {
        "pw" : "..."
      }
    },
    ... optional other users
  ],
  "Groups": [
    {
      "Id": "Admin",
      "Users": [
        .. add the optional other users with admin access.
      ]
    },
    {
      "Id": "Users",
      "Users": [
        .. add all other users to this default group.
      ]
    }
  ]
}
```
Below a [NetPad](https://github.com/tareqimbasher/NetPad) script to calculate the hashed password and an authentication header. 
```csharp
var pw = "changeme01";
var user = "me@jeroenwijdeven.nl";
// ---
var bytes = Encoding.UTF8.GetBytes(pw);
var hash = SHA256.HashData(bytes);
$"\"pw\": \"{Convert.ToBase64String(hash)}\"".Dump();
var bytes2 = Encoding.UTF8.GetBytes($"{user}:{pw}");
$"Authorization: Basic {Convert.ToBase64String(bytes2)}".Dump();
```