namespace WebWolf_Client;

public class NormalPacket : Packet
{
    public int DataType;
    public string Data;
    
    public NormalPacket(string sender,int dataType, string data) : base(sender)
    {
        DataType = dataType;
        Data = data;
    }
}