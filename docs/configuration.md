# AC Timing – Configuration & Deployment Guide

## Building the executable

Run one of the following from the **repository root**. Requires .NET 10 SDK on your build machine.

```bash
# Linux (x64)
dotnet publish src/Devlabs.AcTiming.Web -p:PublishProfile=linux-x64

# Windows (x64)
dotnet publish src/Devlabs.AcTiming.Web -p:PublishProfile=win-x64
```

Output is placed in:

| Platform | Output path             |
|----------|-------------------------|
| Linux    | `publish/linux-x64/`    |
| Windows  | `publish\win-x64\`      |

---

## What to distribute

Copy the **entire output folder** to the target machine. No .NET installation is required.

```
publish/linux-x64/
├── Devlabs.AcTiming.Web          ← main executable
├── libe_sqlite3.so               ← SQLite native library (must stay next to the exe)
├── Devlabs.AcTiming.Web.staticwebassets.endpoints.json
├── appsettings.json              ← edit this before running (see below)
└── wwwroot/                      ← static web assets (CSS, JS, map files)
```

On Linux, make it executable first:
```bash
chmod +x Devlabs.AcTiming.Web
```

---

## Running

```bash
# Linux
./Devlabs.AcTiming.Web

# Windows
Devlabs.AcTiming.Web.exe
```

Open a browser and navigate to `http://<server-ip>:<port>` (default port **5000**).

The SQLite database file (`actiming.db`) is created automatically next to the executable on first start.

---

## Assetto Corsa server requirements

The AC dedicated server must have the UDP plugin enabled. Add/update these settings in `server_cfg.ini`:

```ini
; Port the timing app sends commands TO (must match AcServer:ServerPort below, or leave 0 to auto-detect)
UDP_PLUGIN_LOCAL_PORT=9999

; Address and port the AC server sends data TO (must match AcServer:UdpPort below)
UDP_PLUGIN_ADDRESS=127.0.0.1:9996
```

> Use `127.0.0.1` if the timing app runs **on the same machine** as AC.
> Use the timing machine's IP address if it runs on a **different machine**.

---

## appsettings.json reference

Edit `appsettings.json` before the first run. All settings have sensible defaults.

```json
{
  "Urls": "http://0.0.0.0:5000",
  "ConnectionStrings": {
    "Default": "Data Source=actiming.db"
  },
  "AcServer": {
    "UdpPort": 9996,
    "ServerHost": "",
    "ServerPort": 0,
    "RealtimePosIntervalMs": 100
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### `Urls`
The HTTP address the web server listens on.

| Value | Effect |
|-------|--------|
| `http://localhost:5000` | Only accessible from the same machine (default when not set) |
| `http://0.0.0.0:5000` | Accessible from any machine on the network |
| `http://0.0.0.0:8080` | Same, on a different port |

Set this to `http://0.0.0.0:5000` so spectators can open the timing page from their own computers.

You can also set this via the environment variable `ASPNETCORE_URLS` instead of the file.

---

### `ConnectionStrings.Default`
Path to the SQLite database file. Created automatically on first start.

- Relative paths resolve from the working directory where you run the exe.
- Absolute path example: `"Data Source=/opt/actiming/actiming.db"`

---

### `AcServer.UdpPort`
**Default: `9996`**

The local UDP port this app **listens on** for packets from the AC server.
Must match the port part of `UDP_PLUGIN_ADDRESS` in `server_cfg.ini`.

---

### `AcServer.ServerHost`
**Default: `""` (auto-detected)**

The hostname or IP address of the AC server, used to send commands back to AC.
Leave empty to auto-detect from the first incoming packet — this works in most setups.

---

### `AcServer.ServerPort`
**Default: `0` (auto-detected)**

The port AC listens on for plugin commands.
Corresponds to `UDP_PLUGIN_LOCAL_PORT` in `server_cfg.ini`.
Leave at `0` to auto-detect from the first incoming packet.

---

### `AcServer.RealtimePosIntervalMs`
**Default: `100`**

How often (in milliseconds) the AC server sends car position updates.
Lower values = smoother live positions, more UDP traffic.
Set to `0` to disable position updates entirely.

---

### `Logging.LogLevel`
Controls console log verbosity.

| Level | Use case |
|-------|----------|
| `"Warning"` | Quiet — only problems printed |
| `"Information"` | Normal operation (default) |
| `"Debug"` | Verbose — useful for diagnosing connectivity issues |

To debug UDP packet reception specifically, add this key under `LogLevel`:
```json
"Devlabs.AcTiming.Infrastructure.AcServer.AcUdpClient": "Debug"
```

---

## Minimal working example

AC server and timing app on the **same Windows machine**:

**`server_cfg.ini`** (AC server config):
```ini
UDP_PLUGIN_LOCAL_PORT=9999
UDP_PLUGIN_ADDRESS=127.0.0.1:9996
```

**`appsettings.json`** (timing app, placed next to the exe):
```json
{
  "Urls": "http://0.0.0.0:5000",
  "AcServer": {
    "UdpPort": 9996
  }
}
```

Run `Devlabs.AcTiming.Web.exe`, then open `http://<server-ip>:5000` in any browser.

---

## Firewall

Make sure the following ports are open on the machine running the timing app:

| Port | Protocol | Direction | Purpose |
|------|----------|-----------|---------|
| `9996` (or your `UdpPort`) | UDP | Inbound | Receive data from AC server |
| `5000` (or your `Urls` port) | TCP | Inbound | Browser access to the web UI |
