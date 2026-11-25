# HostListener — 上位机简易监听程序

上位机侧 **TCP 服务端**：监听新设计 PCB（经 4G / 以太网）拨入的连接，打印原始与成帧数据。本目录已纳入私有仓库 `UpperClass-IoT-Host`。

**语言：** [中文](README.md) · [English](README.en.md) · [Русский](README.ru.md)

## 做什么 / 不做什么

| 做 | 不做 |
|---|---|
| 监听 `0.0.0.0:9800`（可改），多 PCB 并发 | 完整业务库、权限、Web |
| 打印对端 IP:端口、十六进制 | 绑定某一行业产品名 |
| `80` / `86` 成帧切分 + 极简 ACK | 总召 / 遥控 / 落库全流程 |
| 参数改端口 | 依赖数据库 |

角色：上位机 = TCP **服务端**；PCB = TCP **客户端**（4G 上网后连主机公网/内网 IP:9800）。

## 目录

```
host-listener/
  HostListener.csproj
  Program.cs
  README.md / README.en.md / README.ru.md
```

## 运行

```bash
cd host-listener
dotnet run -- --port 9800
```

| 参数 | 默认 | 含义 |
|---|---|---|
| `--port` | `9800` | 监听端口 |
| `--bind` | `0.0.0.0` | 绑定地址 |
| `--quiet-hex` | 关 | 少打原始块 |
| `--max-hex` | `256` | 每块最多打印字节数 |

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
publish\HostListener.exe --port 9800
```

## 日志

- `CONNECT`：PCB 已连上（对端常为 4G NAT IP）
- `RX` / `FRAME`：原始 / 成帧
- `TX ACK`：对链路状态类固定帧的极简回应
- `CLOSE`：断开与统计

## 研发用法

1. 防火墙放行入站 **TCP 9800**  
2. 起本程序  
3. PCB 或模拟器连 `服务器IP:9800`  
4. 见 `CONNECT` + `RX`/`FRAME` → 路径通  

## 安全

研发预览：**无 TLS、无鉴权**。公网暴露需防火墙 / VPN / 白名单。