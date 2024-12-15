namespace WebWolf_Client.Networking;

// Datenpaket das an alle Clients gesendet werden sollen
public class BroadcastPaket : NormalPaket
{
    public BroadcastPaket(string sender, PaketDataType dataType, string data) : base(sender, dataType, data)
    {
        Type = PaketType.Broadcast;
    }
}