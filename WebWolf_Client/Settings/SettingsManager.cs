using System.Configuration;
using WebWolf_Client.Roles;

namespace WebWolf_Client.Settings;

public class SettingsManager
{
    // Alle Einstellungen die der Benutzer verändern kann
    public static List<Setting> AllSettings = new List<Setting>();
    
    public static readonly StringSetting ServerIp = new StringSetting("Server IP", "ServerIp", "77.90.17.73");
    
    public static NumberSetting WerwolfMaxAmount { get; } = new NumberSetting("Maximale Anzahl Werwölfe", "RoleWerwolfAmount", 3, 1, 7);
    public static NumberSetting SeherinMaxAmount { get; } = new NumberSetting("Maximale Anzahl Seherinnen", "RoleSeherinAmount", 1, 0, 20);
    public static NumberSetting HexeMaxAmount { get; } = new NumberSetting("Maximale Anzahl Hexen", "RoleHexeAmount", 1, 0, 20);
    public static NumberSetting JägerMaxAmount { get; } = new NumberSetting("Maximale Anzahl Jäger", "RoleJägerAmount", 1, 0, 20);
    public static BooleanSetting AmorEnabled { get; } = new BooleanSetting("Amor aktivieren", "AmorEnabled", true);
    public static BooleanSetting DiebEnabled { get; } = new BooleanSetting("Dieb aktivieren", "DiebEnabled", false);

    public static FloatSetting HexeActionDuration { get; } = new FloatSetting("Dauer Hexe Aktionen (in Sekunden)", "HexeActionDuration", 18,10,30);
    public static FloatSetting JägerActionDuration { get; } = new FloatSetting("Dauer Jäger Aktion (in Sekunden)", "JägerActionDuration", 8,8,30);
    public static FloatSetting WerwolfActionDuration { get; } = new FloatSetting("Dauer Werwolf Aktion (in Sekunden)", "WerwolfActionDuration", 20,20,50);
    public static FloatSetting SeherinActionDuration { get; } = new FloatSetting("Dauer Seherin Aktion (in Sekunden)", "SeherinActionDuration", 8, 10, 30);
    public static FloatSetting AmorActionDuration { get; } = new FloatSetting("Dauer Amor Aktion (in Sekunden)", "AmorActionDuration", 10, 10, 30);
    public static BooleanSetting AmorMultipleCouples { get; } = new BooleanSetting("Armor kann mehrere Paare nacheinander verlieben", "AmorMultipleCouples", false);
    public static FloatSetting DiebActionDuration { get; } = new FloatSetting("Dauer Dieb Aktion (in Sekunden)", "DiebActionDuration", 8, 10, 30);
    
    public static BooleanSetting RevealRoleOnDeath { get; } = new BooleanSetting("Rolle bei Tod anzeigen", "RevealRoleOnDeath", true);

    // Bekommt die Dauer, die eine Rolle für seine Aktionen hat 
    public static float GetRoleActionDuration(RoleType role)
    {
        switch (role)
        {
            case RoleType.Werwolf:
                return WerwolfActionDuration.Value;
            case RoleType.Hexe:
                return HexeActionDuration.Value;
            case RoleType.Seherin:
                return SeherinActionDuration.Value;
            case RoleType.Jäger:
                return JägerActionDuration.Value;
            case RoleType.Amor:
                return AmorActionDuration.Value;
            case RoleType.Dieb:
                return DiebActionDuration.Value;
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
            case RoleType.Amor:
                return AmorEnabled.Value ? 1 : 0;
            case RoleType.Dieb:
                return DiebEnabled.Value ? 1 : 0;
            default:
                return 0;
        }
    }
    
    // Ruft den Wert der Einstellung als Integer ab
    public static int? GetIntValue(string id)
    {
        var settings = ConfigurationManager.AppSettings;
        var raw = settings[id];
        if (raw == null)
            return null;

        return Int32.Parse(raw);
    }

    // Ruft den Wert der Einstellung als Float ab
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

    // Ruft den Wert der Einstellung als Boolean ab
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
    
    // Ruft den Wert der Einstellung als String ab
    public static string? GetStringValue(string id)
    {
        var settings = ConfigurationManager.AppSettings;
        var raw = settings[id];
        return raw;
    }

    // Speichert die Einstellung
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