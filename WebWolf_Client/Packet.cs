namespace WebWolf_Client;

public class Packet
{
    public string Sender;
    public int Type;
    
    public Packet(string sender)
    {
        Sender = sender;
    }
}