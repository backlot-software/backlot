# Backlot.Demo.Studio

A host that runs **nothing but Backlot Studio**, pointed at a Backlot API in another process.
The Studio is an HTTP client of the API, so a host that
only runs the Studio needs none of the framework.

## The endpoint

The API address is `BacklotStudio:BaseUrl`. It has no default here, and there for is required.

Any of the usual configuration sources works:

```bash
# appsettings.json / appsettings.<Environment>.json
{ "BacklotStudio": { "BaseUrl": "https://api.example.com/" } }

# environment
BacklotStudio__BaseUrl=https://api.example.com/ dotnet run

# user-secrets, so it stays out of source control
dotnet user-secrets set "BacklotStudio:BaseUrl" "https://api.example.com/"

# command line
dotnet run -- --BacklotStudio:BaseUrl=https://api.example.com/
```

## Running it against the demo API

This standalone studio works best for the Backlot.Demo.Azure or any other loadbalanced installation for enterprise
purposes not providing a studio endpoint itself.