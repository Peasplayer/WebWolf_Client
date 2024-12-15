namespace WebWolf_Client.Networking;

// Begrüßungspaket mit dem Namen des Spielers
public class HandshakePaket : Paket
{
    public string Name;
    
    public HandshakePaket(string sender, string name): base(sender)
    {
        Name = name;
        Type = PaketType.Handshake;
    }
}