﻿namespace WebWolf_Client.Settings;

public class StringSetting : Setting
{
    public string Value { get; private set; }
    
    public StringSetting(string name, string id, string defaultValue) : base(name, id)
    {
        Value = SettingsManager.GetStringValue(id) ?? defaultValue;
    }
    
    // Setzt die von dem Benutzer eingestellte Wert in die passende Einstellung
    public void SetValue(string value)
    {
        Value = value;
        SettingsManager.SetValue(Id, value);
    }
}