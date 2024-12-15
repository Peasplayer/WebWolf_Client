namespace WebWolf_Client.Settings;

public class NumberSetting : Setting
{
    public int Value { get; private set; }
    public int Min { get; }
    public int Max { get; }
    
    public NumberSetting(string name, string id, int defaultValue, int min, int max) : base(name, id)
    {
        Value = SettingsManager.GetIntValue(id) ?? defaultValue;
        Min = min;
        Max = max;
    }
    
    // Setzt den Wert, der vom Benutzer eingegeben wurde
    public void SetValue(int value)
    {
        if (value < Min || value > Max)
            return;
        
        Value = value;
        SettingsManager.SetValue(Id, value.ToString());
    }
}