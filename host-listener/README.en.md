# HostListener — Upper-Class Host TCP Listener

TCP **server** on the upper-class host: accepts dial-ins from new PCB boards over 4G / Ethernet, logs raw and framed bytes. Lives in the private repo `UpperClass-IoT-Host`.

**Languages:** [中文](README.md) · [English](README.en.md) · [Русский](README.ru.md)

## Does / does not

| Does | Does not |
|---|---|
| Listen `0.0.0.0:9800` (configurable), many concurrent PCBs | Full archive, auth, Web UI |
| Print peer IP:port and hex | Bind to one industry product name |
| Split frames starting `80` / `86` + minimal ACK | Full GI / command / DB pipeline |
| Port via args | Require a database |

Role: host = TCP **server**; PCB = TCP **client** (after 4G attach, connect to host IP:9800).

## Layout

```
host-listener/
  HostListener.csproj
  Program.cs
  README.md / README.en.md / README.ru.md
```

## Run

```bash
cd host-listener
dotnet run -- --port 9800
```

| Arg | Default | Meaning |
|---|---|---|
| `--port` | `9800` | Listen port |
| `--bind` | `0.0.0.0` | Bind address |
| `--quiet-hex` | off | Less raw dumps |
| `--max-hex` | `256` | Max bytes printed per chunk |

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
publish\HostListener.exe --port 9800
```

## Log lines

- `CONNECT` — PCB connected (peer often a 4G NAT IP)
- `RX` / `FRAME` — raw / framed
- `TX ACK` — minimal reply to link-status style fixed frames
- `CLOSE` — disconnect + counters

## Lab use

1. Allow inbound **TCP 9800**  
2. Start this process  
3. PCB or simulator connects to `host-ip:9800`  
4. `CONNECT` + `RX`/`FRAME` means the path works  

## Security

Preview build: **no TLS, no auth**. Do not expose to the open Internet without firewall / VPN / allow-list.