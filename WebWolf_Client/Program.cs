using Newtonsoft.Json;
using WebSocketSharp;

namespace WebWolf_Client;

class Program
{
    private static string ID;
    static void Main(string[] args)
    {
        using (var ws = new WebSocket("ws://192.168.93.160:8443/json"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    Console.WriteLine("Laputa says: " + e.Data);
                    if (e.IsText)
                    {
                        var packet = JsonConvert.DeserializeObject<Packet>(e.Data);
                        if (packet.Type == 0)
                        {
                            var handshake = JsonConvert.DeserializeObject<HandshakePacket>(e.Data);
                            ID = handshake.Name;
                            Console.WriteLine("ID: " + ID);
                            ws.Send(JsonConvert.SerializeObject(new HandshakePacket(ID, "Laputa")));
                        }
                        else
                        {
                            var normalPacket = JsonConvert.DeserializeObject<NormalPacket>(e.Data);
                            if (normalPacket == null)
                            {
                                return;
                            }
                            Console.WriteLine("Data: " + normalPacket.Data);
                        }
                    }
                };
                ws.Connect();
               
                Console.ReadKey();
                
            }
    }
}
