using System.Configuration;
using WebWolf_Client.Roles;

namespace WebWolf_Client.Settings;

public class SettingsManager
{
    public static List<Setting> AllSettings = new List<Setting>();
    
    public static readonly StringSetting ServerIp = new StringSetting("Server IP", "ServerIp", "77.90.17.73");
    
    public static readonly NumberSetting WerwolfMaxAmount = new NumberSetting("Maximale Anzahl Werwölfe", "RoleWerwolfAmount", 3, 1, 7);
    public static readonly NumberSetting SeherinMaxAmount = new NumberSetting("Maximale Anzahl Seherinnen", "RoleSeherinAmount", 1, 1, 20);
    public static readonly NumberSetting HexeMaxAmount = new NumberSetting("Maximale Anzahl Hexen", "RoleHexeAmount", 1, 1, 20);
    public static readonly NumberSetting JägerMaxAmount = new NumberSetting("Maximale Anzahl Jäger", "RoleJägerAmount", 1, 1, 20);

    public static readonly FloatSetting HexeActionDuration = new FloatSetting("Dauer Hexe Aktionen (in Sekunden)", "HexeActionDuration", 18,10,30);
    public static readonly FloatSetting JägerActionDuration = new FloatSetting("Dauer Jäger Aktion (in Sekunden)", "JägerActionDuration", 8,8,30);
    public static readonly FloatSetting WerwolfActionDuration = new FloatSetting("Dauer Werwolf Aktion (in Sekunden)", "WerwolfActionDuration", 20,20,50);
    public static readonly FloatSetting SeherinActionDuration = new FloatSetting("Dauer Seherin Aktion (in Sekunden)", "SeherinActionDuration", 8, 8, 30);



        
    public static readonly BooleanSetting RevealRoleOnDeath = new BooleanSetting("Rolle bei Tod anzeigen", "RevealRoleOnDeath", true);

    // Bekommt die Dauer, die eine Rolle für seine Aktionen hat 
    public static float GetRoleActionDuration(string role)
    {
        switch (role)
        {
            case "Hexe":
            return HexeActionDuration.Value;
            case "Jäger":
            return JägerActionDuration.Value;
            case "Werwolf":
            return WerwolfActionDuration.Value;
            case "Seherin":
            return SeherinActionDuration.Value;
            default:
            return 10;
        }
    }
    
    // Bekommt die maximale Anzahl, die es von jeder Rolle geben darf
    public static int GetMaxAmount(RoleType role)
    {
        switch (role)
        {
            case RoleType.Werwolf:
                return WerwolfMaxAmount.Value;
            case RoleType.Seherin:
                return SeherinMaxAmount.Value;
            case RoleType.Hexe:
                return HexeMaxAmount.Value;
            case RoleType.Jäger:
                return JägerMaxAmount.Value;
            default:
                return 0;
        }
    }
    
    public static int? GetIntValue(string id)
    {
        var settings = ConfigurationManager.AppSettings;
        var raw = settings[id];
        if (raw == null)
            return null;

        return Int32.Parse(raw);
    }

    public static float? GetFloatValue(string id)
    {
        var settings = ConfigurationManager.AppSettings;
        var raw = settings[id];
        if (raw == null)
        {
            return null;
        }
        return float.Parse(raw);
    }

    public static bool? GetBooleanValue(string id)
    {
        var settings = ConfigurationManager.AppSettings;
        var raw = settings[id];
        if (raw == null)
        {
            return null;
        }
        return bool.Parse(raw);
    }
    
    public static string? GetStringValue(string id)
    {
        var settings = ConfigurationManager.AppSettings;
        var raw = settings[id];
        return raw;
    }

    public static void SetValue(string id, string value)
    {
        var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var settings = configFile.AppSettings.Settings;
        if (settings[id] == null)
            settings.Add(id, value);
        else
            settings[id].Value = value;
        
        configFile.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
    }
}