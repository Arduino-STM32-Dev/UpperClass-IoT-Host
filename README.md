# Upper-Class Host Machine for Industrial IoT

Private design note for a **上位机** (upper-class / supervisory host) that sits above field devices and turns raw OT signals into operable plant data.

This repository introduces the host-layer product direction only. It does not document, fork, or expose any existing field project.

**Languages:** [English](README.md) · [中文](README.zh-CN.md) · [Русский](README.ru.md)

## Open device prospect (beyond one vertical)

The host is not locked to a single product family. Any board that can keep a session and speak a declared dialect can sit under the same upper-class layer. Early R&D candidates include:

- **Smart meters / energy monitors** — voltage, current, power, demand, breaker state
- **Environmental nodes** — temperature, humidity, gas, vibration, door/window contacts
- **Industrial cameras / vision triggers** — event flags + sparse telemetry (not raw video on the OT path)
- **Actuator / relay modules** — pump, valve, contactor with select-execute-safe command path
- **Battery / BMS packs** — SOC, SOH, cell alarms, charge inhibit
- **HVAC / building controllers** — setpoints, mode, fault bits, schedule overrides
- **4G / Ethernet edge gateways** — concentrate several sensors into one host identity
- **Lab bring-up PCBs** — new MCU + modem spins used to prove the wire before product tooling

Common rule: **identity in the frame** (or device certificate), not in a DHCP lease. The host owns archive, roles, and UX; the PCB stays thin.


## R&D core: why host ↔ new PCB communication matters

From a research-and-development view, the first hard gate is not the HMI look. It is a **closed loop between the upper-class host and a newly designed PCB**. Until that loop is green, firmware, protocol, and product claims stay unproven.

**Meaning of establishing communication**

1. **Proof of the protocol contract** — The board is not "online" because TCP connected. It is online when frames with agreed start bytes, address (`A` + `CA`), checksum, and TI map are accepted by the host decoder.
2. **Decouple hardware bring-up from cloud demos** — Early PCB spin must talk to *our* host, not a vendor MQTT toy. That keeps dialect, point table, and command semantics under R&D control.
3. **Make identity durable** — 4G IP changes; link/common address does not. Host registration is how a new board becomes a named asset in the lab and later in the field.
4. **Create a regression surface** — Once the host accepts the board, every firmware change can be scored: handshake time, GI complete, YX/YC non-zero, SOE timestamp, heartbeat hold.
5. **Unlock the rest of the stack** — Archive, alarm, web API, mobile view, and remote control are worthless until the wire path is real.

**What becomes possible after communication succeeds**

| Capability | R&D outcome |
|---|---|
| Link init + dialect detect | Confirm balanced / standard 101 (or custom) path on this PCB revision |
| General interrogation (GI) | Full YX/YC snapshot validates point map and ADC/IO wiring |
| COS / SOE | Prove event path and clock quality for protection-style signals |
| Clock sync / read | Align board time so SOE is forensic-grade |
| Select → execute → terminate | Safe remote command path for switch objects on the new board |
| Heartbeat / reconnect | Measure hold time, drop rate, and recovery under NAT and 4G |
| Persist + API | Same session feeds archive and ops UI without a second protocol |
| Multi-board soak | Parallel sessions expose race, buffer, and address-collision bugs early |

**R&D stop condition for a PCB spin:** host list shows this board as online with correct dialect; GI fills non-empty YC/YX; at least one COS/SOE round-trip; command path dry-run OK; session holds through heartbeat. Only then does the board leave "lab wire" and enter product integration.


## Why an upper-class machine

Edge boards, meters, PLCs, sensors, gateways, and actuator nodes speak short binary or framed payloads on serial, TCP, or cellular. Operators do not. The upper-class host is the control-room side that:

1. Accepts many concurrent device sessions
2. Decodes protocol dialects into a stable object model (YX / YC / SOE / commands)
3. Stores history, alarms, and identity (link address, common address, asset tag)
4. Serves HMI / API / mobile views without putting databases on every board
5. Enforces auth, audit, and safe command paths (select → execute → terminate)

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

Python/JS stay useful for tools and UIs. The **core host path** (listen → decode → persist → command) stays C# so invariants live in one compiled place.

## Target shape (product, not a clone)

```
[ New IoT PCB / edge node ] --TCP--> [ Upper-Class Host ]
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