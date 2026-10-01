
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerApp
{
    public struct Command
    {
        public int SequenceNumber;
        public float MoveX;
        public float MoveY;
    }

    internal class Program
    {
        private static ConcurrentQueue<Command> commandQueue = new ConcurrentQueue<Command>();
        private static float serverX = 0.0f;
        private static float serverY = 0.0f;
        private static int lastProcessedSequence = 0;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("=== Authoritative Server Starting ===");
            using UdpClient server = new UdpClient(9050);
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);

            // Task 1: Receiver Thread (Fills the Command Queue)
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var result = await server.ReceiveAsync();
                    clientEP = result.RemoteEndPoint;
                    string rawData = Encoding.UTF8.GetString(result.Buffer);

                    string[] parts = rawData.Split(',');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int seq) &&
                        float.TryParse(parts[1], out float dx) &&
                        float.TryParse(parts[2], out float dy))
                    {
                        commandQueue.Enqueue(new Command { SequenceNumber = seq, MoveX = dx, MoveY = dy });
                    }
                }
            });

            // Task 2: Server Simulation Loop (Authoritative Tick at 20 Hz)
            while (true)
            {
                while (commandQueue.TryDequeue(out Command cmd))
                {
                    serverX += cmd.MoveX;
                    serverY += cmd.MoveY;
                    lastProcessedSequence = cmd.SequenceNumber;
                }

                if (clientEP.Port != 0)
                {
                    // Format: AckSequence, ServerPositionX, ServerPositionY
                    string statePayload = $"{lastProcessedSequence},{serverX:F2},{serverY:F2}";
                    byte[] payloadBytes = Encoding.UTF8.GetBytes(statePayload);
                    await server.SendAsync(payloadBytes, payloadBytes.Length, clientEP);
                }

                Console.WriteLine($"[Tick] Auth Pos: ({serverX:F2}, {serverY:F2}) | Last Seq Ack: {lastProcessedSequence}");
                await Task.Delay(50); // 20 Ticks / sec
            }
        }
    }
}
