# Backlot.Studio.Desktop

A standalone, cross-platform desktop application for **Backlot Studio** built with Electron and an ASP.NET Core (.NET 10) sidecar host.

It packages the complete Backlot Studio management interface into a dedicated desktop window without requiring a separate browser tab or embedding within your main API host. It includes an interactive connection profile manager to seamlessly test and switch between remote Backlot API environments (e.g. Local Dev, Staging, Production).

---

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/)
- [Node.js](https://nodejs.org/) (v18+ recommended) and `npm`

---

## Quick Start (Development)

### 1. Build the .NET Host

From the repository root or the project folder, build the project once so that the .NET assembly is available:

```bash
dotnet build Backlot.Studio.Desktop/Backlot.Studio.Desktop.csproj
```

### 2. Install Electron Dependencies

Navigate into the `electron` directory and install dependencies:

```bash
cd Backlot.Studio.Desktop/electron
npm install
```

### 3. Start the Desktop App

Launch the application using Electron:

```bash
npm start
```

When launched:
1. The **Connection Manager** screen will appear.
2. Enter your Backlot API URL (e.g. `https://localhost:7221/` or your remote environment) and click **Test Connection** to verify reachability.
3. Click **Connect** to launch the headless .NET Kestrel sidecar on an ephemeral loopback port (`127.0.0.1:0`) and navigate directly to `/studio`.

---

## In-App Navigation & Shortcuts

- **Switch Connection Profile:** Press <kbd>Ctrl+Shift+C</kbd> (or <kbd>Cmd+Shift+C</kbd> on macOS) or select **Studio > Switch Connection Profile** from the application menu to disconnect from the active session and return to the profile manager.
- **Reload Studio:** Press <kbd>Ctrl+R</kbd> (or <kbd>Cmd+R</kbd> on macOS) or select **Studio > Reload Studio**.
- **Developer Tools:** Press <kbd>Ctrl+Shift+I</kbd> or toggle via **View > Toggle Developer Tools**.

---

## Running Headless (.NET Host Only)

You can also run the headless .NET Kestrel host directly from the terminal without Electron.

Because it runs headless without a co-hosted API, `BacklotStudio:BaseUrl` is required:

```bash
dotnet run --project Backlot.Studio.Desktop/Backlot.Studio.Desktop.csproj -- --BacklotStudio:BaseUrl=https://localhost:7221/
```

To specify an explicit port rather than dynamic port binding (`0`):

```bash
dotnet run --project Backlot.Studio.Desktop/Backlot.Studio.Desktop.csproj -- --urls=http://127.0.0.1:5000 --BacklotStudio:BaseUrl=https://localhost:7221/
```

Once running, navigate to `http://127.0.0.1:5000/studio` in any web browser.

---

## Packaging & Distribution

The packaging pipeline uses `dotnet publish` to build a single-file, self-contained .NET binary and `electron-builder` to bundle the application.

Inside `Backlot.Studio.Desktop/electron`:

```bash
# Publish self-contained .NET binary to ./dotnet-bin
npm run build:dotnet

# Package an unpacked executable directory into ./dist
npm run pack

# Build platform installers and distributables (AppImage, deb, dmg, zip, nsis)
npm run dist
```

Platform-specific .NET publish profiles are located in `Backlot.Studio.Desktop/Properties/PublishProfiles/`:
- `npm run build:dotnet:linux` (linux-x64)
- `npm run build:dotnet:win` (win-x64)
- `npm run build:dotnet:osx` (osx-arm64)

---

## Running Tests

Automated integration tests verify process lifecycle management, readiness token detection, ephemeral port binding, and API reachability:

```bash
cd Backlot.Studio.Desktop/electron
npm test
```
