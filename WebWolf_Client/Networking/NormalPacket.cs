namespace WebWolf_Client.Networking;

public class NormalPacket : Packet
{
    public PacketDataType DataType;
    public string Data;
    
    public NormalPacket(string sender, PacketDataType dataType, string data) : base(sender)
    {
        DataType = dataType;
        Data = data;
    }
}