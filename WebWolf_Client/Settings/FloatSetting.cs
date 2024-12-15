namespace WebWolf_Client.Settings;

public class FloatSetting : Setting
{
    public float Value { get; private set; }
    public float Min { get; }
    public float Max { get; }

    public FloatSetting(string name, string id, float defaultValue, float min, float max) : base(name, id)
    {
        Value = SettingsManager.GetFloatValue(id) ?? defaultValue;
        Min = min;
        Max = max;
    }

    public void SetValue(float value)
    {
        if (value < Min || value > Max)
            return;
        
        Value = value;
        SettingsManager.SetValue(Id, value.ToString("0.0"));
    }
}