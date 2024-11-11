namespace WebWolf_Client.Networking;

public class HandshakePacket : Packet
{
    public string Name;
    
    public HandshakePacket(string sender,string name): base(sender)
    {
        Name = name;
        Type = PacketType.Handshake;
    }
}