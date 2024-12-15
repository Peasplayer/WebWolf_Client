using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles.RoleClasses;
using WebWolf_Client.Settings;

namespace WebWolf_Client.Roles;

public class RoleManager
{
    // Liste aller Rollen
    public static List<Role> Roles { get; set; } = new List<Role>()
    {
        new Dieb(),
        new Amor(),
        new Seherin(),
        new Werwolf(),
        new Hexe(),
        new Jäger()
    };

    // Sucht das erste objekt mir derselben Rolle
    public static Role GetRole(RoleType roleType)
    {
        return Roles.First(x => x.RoleType == roleType);
    }
    
    // Erstellt eine Liste mir den Spieler und ihren Rollen
    public static List<PlayerData> GetPlayersWithRole(RoleType role)
    {
        var list = new List<PlayerData>();
        foreach (var player in PlayerData.Players)
        {
            if (player.Role == role)
                list.Add(player);
        }

        return list;
    }

    // Gibt jeder Person eine zufällige Rolle
    public static void AssignRoles()
    {
        Random random = new Random();
        var availablePlayers = new List<PlayerData>(PlayerData.Players);

        // Als Erstes werden die Werwölfe zugewiesen
        var werwolf = (Werwolf) GetRole(RoleType.Werwolf);
        // Es wird maximal 1 Werwolf auf 4 Spieler zugewiesen, es sei denn das eingestellte Limit ist kleiner
        for (int i = 0; i < Math.Min(werwolf.MaxAmount, PlayerData.Players.Count / 4); i++)
        {
            if (availablePlayers.Count == 0)
                continue;

            int randomIndex = random.Next(availablePlayers.Count);
            availablePlayers[randomIndex].RpcSetRole(RoleType.Werwolf);
            availablePlayers.RemoveAt(randomIndex);
        }

        // Danach werden alle Rollen * Limit in eine Liste geschrieben...
        var availableRoles = new List<RoleType>();
        foreach (var role in Roles)
        {
            if (role.RoleType == RoleType.Werwolf)
                continue;

            for (int i = 0; i < role.MaxAmount; i++)
            {
                availableRoles.Add(role.RoleType);
            }
        }

        // ... und anschließend den Spielern zufällig zugewiesen
        foreach (var player in availablePlayers.ToList())
        {
            if (availableRoles.Count == 0)
                continue;

            int randomIndex = random.Next(availableRoles.Count);
            player.RpcSetRole(availableRoles[randomIndex]);
            availableRoles.RemoveAt(randomIndex);
            availablePlayers.Remove(player);
        }

        // Alle restlichen Spieler werden Dorfbewohner
        foreach (var player in availablePlayers)
        {
            player.RpcSetRole(RoleType.Dorfbewohner);
        }
    }
    
    public static Dictionary<string, int> WaitingForRole { get; } = new Dictionary<string, int>();
    
    // Ruft die Spieler mit der Rolle auf und verwaltet den Ablauf der jeweiligen Aktion
    public static void RpcCallRole(RoleType role, PlayerData? target = null)
    {
        var roleObj = RoleManager.GetRole(role);
        WaitingForRole.Clear();
        
        // Je nachdem wer alles aufgerufen werden soll, wird zur Warteliste hinzugefügt
        if (target != null)
            WaitingForRole.Add(target.Id, 0);
        else
        {
            foreach (var playerData in PlayerData.Players)
            {
                if (playerData.Role == role && (playerData.IsAlive || !roleObj.IsAliveRole))
                    WaitingForRole.Add(playerData.Id, 0);
            }
        }

        // Falls niemand aufgerufen werden soll, wird der Vorgang abgebrochen
        if (WaitingForRole.Count == 0)
            return;

        // Die, die aufgrufen werden sollen, werden aufgerufen
        if (target != null)
        {
            NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                new SendToPaket(NetworkingManager.Instance.CurrentId, PaketDataType.CallRole,
                    JsonConvert.SerializeObject(new Pakets.SimpleRole(role)), target.Id)));
        }
        else
        {
            NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.CallRole,
                    JsonConvert.SerializeObject(new Pakets.SimpleRole(role)))));
        }

        // Setzt die Zeit, die jede Rolle für seine Aktionen, hat auf die eingestellt Zeit 
        float actionDuration = SettingsManager.GetRoleActionDuration(role);
        var timer = Task.Delay((int) actionDuration * 1000);
        while (WaitingForRole.Count > 0)
        {
            if (timer.IsCompleted)
            {
                for (var i = 0; i < WaitingForRole.Count; i++)
                {
                    var pair = WaitingForRole.ElementAt(i);
                    if (pair.Value == 0)
                    {
                        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                            new SendToPaket(NetworkingManager.Instance.CurrentId, PaketDataType.RoleCanceled,
                                JsonConvert.SerializeObject(new Pakets.SimpleRole(role)), pair.Key)));
                        WaitingForRole[pair.Key] = 1;
                    }
                }
                
                // Falls irgendwelche Sachen noch vom Host erledigt werden müssen, nachdem die Aktion abgebrochen wurde
                if (PlayerData.LocalPlayer.Role != role)
                {
                    roleObj.PrepareCancelAction();
                    roleObj.AfterCancel();
                }
                break;
            }
        }
        
        while (WaitingForRole.ContainsValue(1)){}
        
        Program.DebugLog("Role is done");
    }
}
