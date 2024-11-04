using System.Security.Cryptography;

namespace WebWolf_Client;

public class HandshakePacket : Packet
{
    public string Name;
    
    public HandshakePacket(string sender,string name): base(sender)
    {
        Name = name;
        Type = 0;
        
    }
}