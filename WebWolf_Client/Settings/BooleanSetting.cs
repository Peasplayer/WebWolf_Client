namespace WebWolf_Client.Settings;

public class BooleanSetting : Setting
{
    public bool Value { get; private set; }

    public BooleanSetting(string name, string id, bool defaultValue) : base(name, id)
    {
        Value = SettingsManager.GetBooleanValue(id) ?? defaultValue;
    }

    // Setzt den Wert, der vom Benutzer eingegeben wurde
    public void SetValue(bool value)
    {
        Value = value;
        SettingsManager.SetValue(Id, value.ToString());
    }
}
