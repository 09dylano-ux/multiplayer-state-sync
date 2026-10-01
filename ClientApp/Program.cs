using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    public struct InputFrame
    {
        public int SequenceNumber;
        public float MoveX;
        public float MoveY;
        public float PredictedX;
        public float PredictedY;
    }

    internal class Program
    {
        private static float clientX = 0.0f;
        private static float clientY = 0.0f;
        private static int sequenceCounter = 0;
        private static List<InputFrame> pendingInputs = new List<InputFrame>();

        private static async Task Main(string[] args)
        {
            Console.WriteLine("=== Game Client Starting (Predictive Simulation) ===");
            using UdpClient client = new UdpClient();
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 9050);

            // Task 1: Server State Listener & Reconciliation Thread
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var result = await client.ReceiveAsync();
                    string rawData = Encoding.UTF8.GetString(result.Buffer);

                    string[] parts = rawData.Split(',');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int ackSeq) &&
                        float.TryParse(parts[1], out float authX) &&
                        float.TryParse(parts[2], out float authY))
                    {
                        Reconcile(ackSeq, authX, authY);
                    }
                }
            });

            // Task 2: Client Simulation Loop
            Random rand = new Random();
            while (true)
            {
                sequenceCounter++;
                float dx = (float)(rand.NextDouble() * 2.0 - 1.0); // Random movement step
                float dy = (float)(rand.NextDouble() * 2.0 - 1.0);

                // 1. Local Prediction (Immediate application without waiting for server)
                clientX += dx;
                clientY += dy;

                pendingInputs.Add(new InputFrame
                {
                    SequenceNumber = sequenceCounter,
                    MoveX = dx,
                    MoveY = dy,
                    PredictedX = clientX,
                    PredictedY = clientY
                });

                // 2. Transmit Input Command to Server
                string payload = $"{sequenceCounter},{dx:F2},{dy:F2}";
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                await client.SendAsync(bytes, bytes.Length, serverEP);

                Console.WriteLine($"[Client Pred] Seq: {sequenceCounter} | Pos: ({clientX:F2}, {clientY:F2}) | Unack Queue Size: {pendingInputs.Count}");
                await Task.Delay(100);
            }
        }

        private static void Reconcile(int ackSeq, float authX, float authY)
        {
            lock (pendingInputs)
            {
                // Remove all commands the server has already acknowledged
                pendingInputs.RemoveAll(i => i.SequenceNumber <= ackSeq);

                // Re-simulate from the authoritative server state
                float reconciledX = authX;
                float reconciledY = authY;

                foreach (var input in pendingInputs)
                {
                    reconciledX += input.MoveX;
                    reconciledY += input.MoveY;
                }

                // Smoothly update client state
                clientX = reconciledX;
                clientY = reconciledY;
            }
        }
    }
}
