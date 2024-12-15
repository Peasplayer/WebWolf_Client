namespace WebWolf_Client.Networking;

// Basispaket-Klasse
public class Paket
{
    public string Sender;
    public PaketType Type;
    
    public Paket(string sender)
    {
        Sender = sender;
    }
}