namespace WebWolf_Client.Networking;

public class Packet
{
    public string Sender;
    public PacketType Type;
    
    public Packet(string sender)
    {
        Sender = sender;
    }
}