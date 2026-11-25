using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Upper-class host: simple TCP listener for new PCB boards dialing in over 4G/Ethernet.
// No product-specific dialect decoder � accept, log, optional framed ACK.
//
//   dotnet run -- --port 9800
//   HostListener.exe --port 9800 --bind 0.0.0.0

int port = int.Parse(GetArg(args, "--port", "9800"), CultureInfo.InvariantCulture);
string bind = GetArg(args, "--bind", "0.0.0.0");
bool quietHex = HasFlag(args, "--quiet-hex");
int maxHex = int.Parse(GetArg(args, "--max-hex", "256"), CultureInfo.InvariantCulture);

var ip = bind is "0.0.0.0" or "*" ? IPAddress.Any : IPAddress.Parse(bind);
var listener = new TcpListener(ip, port);
listener.Start();
Console.WriteLine($"[{Ts()}] HostListener v0.1 listening on {bind}:{port}");
Console.WriteLine("Waiting for PCB clients (4G/Ethernet TCP). Ctrl+C to stop.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var sessions = 0;
try
{
    while (!cts.IsCancellationRequested)
    {
        var acceptTask = listener.AcceptTcpClientAsync(cts.Token).AsTask();
        var client = await acceptTask;
        sessions++;
        var id = sessions;
        _ = Task.Run(() => HandleClientAsync(client, id, quietHex, maxHex, cts.Token), cts.Token);
    }
}
catch (OperationCanceledException) { }
finally
{
    listener.Stop();
    Console.WriteLine($"[{Ts()}] stopped.");
}

static async Task HandleClientAsync(TcpClient client, int id, bool quietHex, int maxHex, CancellationToken ct)
{
    var remote = client.Client.RemoteEndPoint?.ToString() ?? "?";
    Console.WriteLine($"[{Ts()}] #{id} CONNECT {remote}");
    client.NoDelay = true;
    client.ReceiveTimeout = 0;

    var buf = new byte[4096];
    var pending = new List<byte>();
    long rxBytes = 0;
    long rxFrames = 0;

    try
    {
        using (client)
        using (var stream = client.GetStream())
        {
            while (!ct.IsCancellationRequested && client.Connected)
            {
                int n;
                try
                {
                    n = await stream.ReadAsync(buf.AsMemory(0, buf.Length), ct);
                }
                catch (OperationCanceledException) { break; }
                catch (IOException) { break; }

                if (n <= 0) break;
                rxBytes += n;
                for (int i = 0; i < n; i++) pending.Add(buf[i]);

                if (!quietHex)
                {
                    var show = Math.Min(n, maxHex);
                    Console.WriteLine($"[{Ts()}] #{id} RX {n}B from {remote}  {Hex(buf.AsSpan(0, show))}{(n > show ? " ..." : "")}");
                }

                foreach (var frame in TakeFramed(pending))
                {
                    rxFrames++;
                    Console.WriteLine($"[{Ts()}] #{id} FRAME {frame.Length}B  {Hex(frame)}");
                    // Minimal link-style ACK for fixed 6-byte frames starting 0x80 (optional hello path)
                    if (frame.Length == 6 && frame[0] == 0x80 && frame[5] == 0x16)
                    {
                        byte c = frame[1];
                        byte fc = (byte)(c & 0x0F);
                        bool prm = (c & 0x40) != 0;
                        if (prm && fc == 9)
                        {
                            // request link status ? status OK (DIR=1 PRM=0 FC=11)
                            var ack = FixedAck(0x8B, frame[2], frame[3]);
                            await stream.WriteAsync(ack, ct);
                            Console.WriteLine($"[{Ts()}] #{id} TX ACK link-status  {Hex(ack)}");
                        }
                        else if (prm && (fc == 0 || fc == 2))
                        {
                            var ack = FixedAck(0x80, frame[2], frame[3]);
                            await stream.WriteAsync(ack, ct);
                            Console.WriteLine($"[{Ts()}] #{id} TX ACK confirm  {Hex(ack)}");
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{Ts()}] #{id} ERR {ex.Message}");
    }
    finally
    {
        Console.WriteLine($"[{Ts()}] #{id} CLOSE {remote}  rxBytes={rxBytes} frames={rxFrames}");
    }
}

static byte[] FixedAck(byte control, byte aLo, byte aHi)
{
    byte sum = (byte)((0x80 + control + aLo + aHi) & 0xFF);
    return new byte[] { 0x80, control, aLo, aHi, sum, 0x16 };
}

static List<byte[]> TakeFramed(List<byte> b)
{
    var list = new List<byte[]>();
    while (b.Count > 0)
    {
        byte s0 = b[0];
        if (s0 == 0x80)
        {
            if (b.Count < 6) break;
            if (b[5] != 0x16) { b.RemoveAt(0); continue; }
            list.Add(b.GetRange(0, 6).ToArray());
            b.RemoveRange(0, 6);
            continue;
        }
        if (s0 == 0x86)
        {
            if (b.Count < 4) break;
            int len = b[1];
            if (b[2] != b[1] || b[3] != 0x86) { b.RemoveAt(0); continue; }
            int total = len + 6;
            if (b.Count < total) break;
            list.Add(b.GetRange(0, total).ToArray());
            b.RemoveRange(0, total);
            continue;
        }
        // Unknown leading byte: keep raw until a known start appears, or drop one
        b.RemoveAt(0);
    }
    return list;
}

static string GetArg(string[] args, string name, string def)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return def;
}

static bool HasFlag(string[] args, string name) =>
    args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

static string Hex(ReadOnlySpan<byte> b)
{
    var sb = new StringBuilder(b.Length * 3);
    for (int i = 0; i < b.Length; i++)
    {
        if (i > 0) sb.Append(' ');
        sb.Append(b[i].ToString("X2"));
    }
    return sb.ToString();
}

static string Ts() => DateTime.Now.ToString("HH:mm:ss");
