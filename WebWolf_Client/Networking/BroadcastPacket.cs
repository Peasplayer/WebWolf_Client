namespace WebWolf_Client.Networking;

public class BroadcastPacket : NormalPacket
{
    public BroadcastPacket(string sender, PacketDataType dataType, string data) : base(sender, dataType, data)
    {
        Type = PacketType.Broadcast;
    }
}