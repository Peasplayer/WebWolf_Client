namespace WebWolf_Client.Ui;

// Klasse um Daten für die Darstellung eines Kreises von Spielern zu senden
public class SpecialPlayerCircle
{
    public List<string> PlayerNames { get; }
    public string CenterText { get; }
        
    public SpecialPlayerCircle(List<string> playerNames, string centerText)
    {
        PlayerNames = playerNames;
        CenterText = centerText;
    }
}