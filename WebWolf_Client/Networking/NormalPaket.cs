namespace WebWolf_Client.Networking;

// Normales Datenpaket
public class NormalPaket : Paket
{
    public PaketDataType DataType;
    public string Data;
    
    public NormalPaket(string sender, PaketDataType dataType, string data) : base(sender)
    {
        DataType = dataType;
        Data = data;
    }
}