namespace WebWolf_Client.Settings;

public abstract class Setting
{
    public string Name { get; }
    public string Id { get; }
    
    public Setting(string name, string id)
    {
        Name = name;
        Id = id;
        
        SettingsManager.AllSettings.Add(this);
    }
}