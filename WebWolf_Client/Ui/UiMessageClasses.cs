namespace WebWolf_Client.Ui;

public class UiMessageClasses
{
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
}