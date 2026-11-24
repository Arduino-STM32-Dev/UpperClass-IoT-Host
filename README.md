# Upper-Class Host Machine for Industrial IoT

Private design note for a **???** (upper-class / supervisory host) that sits above field devices and turns raw OT signals into operable plant data.

This repository introduces the host-layer product direction only. It does not document, fork, or expose any existing field project.

## Why an upper-class machine

Field boards, FTUs, PLCs, and sensors speak short binary frames on serial or TCP. Operators do not. The upper-class host is the control-room side that:

1. Accepts many concurrent device sessions
2. Decodes protocol dialects into a stable object model (YX / YC / SOE / commands)
3. Stores history, alarms, and identity (link address, common address, asset tag)
4. Serves HMI / API / mobile views without putting databases on every board
5. Enforces auth, audit, and safe command paths (select ? execute ? terminate)

Without this layer, each PCB ships a one-off cloud demo (MQTT JSON, vendor portal). That does not scale across vendors, NAT, or 4G address churn. Identity must live in the frame (`A` + `CA`), not in a transient IP.

## Why IoT application on top

IoT here means **operational IoT**, not consumer gadgets:

- Always-on telemetry from wide-area assets
- Event time (SOE) that survives reconnects
- Command and parameter loops with human accountability
- Fleet view: online / offline, dialect, last GI, last alarm
- Integration out: REST / WebSocket / export for MES, GIS, work-order systems

The host is the place where OT protocol fidelity and IT delivery meet. Boards stay lean. The cloud or plant server carries policy, storage, and UX.

## Why C# as the main language

| Need | C# fit |
|---|---|
| Long-running Windows / Linux service host | .NET worker + ASP.NET Core, one language |
| Binary protocol work | Structs, `Span<byte>`, endian helpers, no GC thrash on hot paths |
| Concurrent sockets | `async`/`await`, channels, thread-pool I/O |
| Desktop ops tools + web API | WinForms / WPF / Avalonia **and** Minimal APIs in one stack |
| Strong typing for ASDU / TI / COT maps | Enums, records, analyzers catch wire-format mistakes early |
| Deploy to plant PCs | Self-contained publish, Windows Service, IIS / Kestrel |
| Ecosystem | Npgsql / SQL Server, Serilog, OpenTelemetry, good IDE (Visual Studio / Rider) |

**Summary of advantages**

1. **One stack, many surfaces** — collector daemon, HTTP mid-layer, desktop monitor, test simulator share models and libraries.
2. **Performance enough for OT** — modern .NET is competitive with Go/Java for socket servers; GC is tunable; native AOT optional later.
3. **Safety vs scripting-only** — nullability, static analysis, and typed protocol frames reduce silent wire bugs that PowerShell/Python prototypes miss.
4. **Plant-friendly ops** — most industrial Windows boxes already run .NET-capable hosts; installers and services are familiar to electric ops teams.
5. **Hiring and longevity** — C# / .NET remains a mainstream enterprise language with LTS releases and clear upgrade paths.

Python/JS stay useful for tools and UIs. The **core host path** (listen ? decode ? persist ? command) stays C# so invariants live in one compiled place.

## Target shape (product, not a clone)

```
[ Field PCB / FTU ] --TCP--> [ Upper-Class Host ]
                                |-- session + dialect auto-detect
                                |-- archive / realtime store
                                |-- auth + roles
                                +-- Web / API / ops console
```

Non-goals for this intro repo:

- No dump of an existing plant codebase
- No vendor credentials, VPN accounts, or live endpoints
- No claim that any third-party cloud MQTT demo replaces the host

## Status

Private. Design brief only. Implementation follows in separate private packages under the same org.

## License

All rights reserved. Private repository.
