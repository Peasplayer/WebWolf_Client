namespace WebWolf_Client.Networking;

// Paket das nur an bestimmte Empfänger gesendet wird
public class SendToPaket : NormalPaket
{
    public string Receiver;
    public SendToPaket(string sender, PaketDataType dataType, string data, string receiver) : base(sender, dataType, data)
    {
        Type = PaketType.SendTo;
        Receiver = receiver;
    }
}