namespace WebWolf_Client.Networking;

public class SendToPacket : NormalPacket
{
    public string Receiver;
    public SendToPacket(string sender, PacketDataType dataType, string data, string receiver) : base(sender, dataType, data)
    {
        Type = PacketType.SendTo;
        Receiver = receiver;
    }
}