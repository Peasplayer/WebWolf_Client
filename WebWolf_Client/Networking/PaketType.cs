namespace WebWolf_Client.Networking;

// Art des Pakets
public enum PaketType : uint
{
    Handshake = 0,
    Broadcast = 1,
    SendTo = 2,
    Server = 3
}