namespace WebWolf_Client.Settings;

public abstract class Setting
{
    // Bekommt die ID und den Namen von der Einstellung
    public string Name { get; }
    public string Id { get; }
    
    public Setting(string name, string id)
    {
        Name = name;
        Id = id;
        
        // Fügt die Einstellung zur Liste hinzu
        SettingsManager.AllSettings.Add(this);
    }
}