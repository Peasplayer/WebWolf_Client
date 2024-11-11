using WebWolf_Client.Networking;

namespace WebWolf_Client;

class Program
{
    private static string ID;
    static void Main(string[] args)
    {
        var net = new NetworkingManager();
        net.StartConnection("ws://localhost:8443/json");
        Console.ReadKey();
    }
}
