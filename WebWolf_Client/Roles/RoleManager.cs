using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles.RoleClasses;

namespace WebWolf_Client.Roles;

public class RoleManager
{
    public static List<Role> Roles { get; set; } = new List<Role>()
    {
        new Seherin(),
        new Werwolf(),
        new Hexe()
    };

    public static Role GetRole(RoleType roleType)
    {
        return Roles.First(x => x.RoleType == roleType);
    }
    
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

    public static void AssignRoles()
    {
        Random random = new Random();
        var availablePlayers = new List<PlayerData>(PlayerData.Players);
        var roleSettings = RoleSetting.RoleSettings;

        foreach (var roleSetting in roleSettings)
        {
            for (int i = 0; i < roleSetting.MaxAmount; i++)
            {
                if (availablePlayers.Count == 0)
                    continue;

                int randomIndex = random.Next(availablePlayers.Count);
                availablePlayers[randomIndex].RpcSetRole(roleSetting.Role);
                availablePlayers.RemoveAt(randomIndex);
            }
            
        }

        foreach (var player in availablePlayers)
        {
            player.RpcSetRole(RoleType.Dorfbewohner);
        }
        
        foreach (var player in PlayerData.Players)
        {
            Program.DebugLog($"{player.Name}: {player.Role}");
        }
    }
    
    public static Dictionary<string, int> WaitingForRole { get; } = new Dictionary<string, int>();
    
    // Ruft die Spieler mit der Rolle auf und verwaltet den Ablauf der jeweiligen Aktion
    public static void RpcCallRole(RoleType role)
    {
        WaitingForRole.Clear();
        foreach (var playerData in PlayerData.Players)
        {
            if (playerData.Role == role && playerData.IsAlive)
                WaitingForRole.Add(playerData.Id, 0);
        }

        if (WaitingForRole.Count == 0)
            return;

        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.CallRole, JsonConvert.SerializeObject(new Packets.SimpleRole(role)))));

        var timer = Task.Delay(14000);
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
                            new SendToPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleCanceled,
                                JsonConvert.SerializeObject(new Packets.SimpleRole(role)), pair.Key)));
                        WaitingForRole[pair.Key] = 1;
                    }
                }
                
                var roleObj = RoleManager.GetRole(role);
                roleObj.PrepareCancelAction();
                roleObj.AfterCancel();
                break;
            }
        }
        
        while (WaitingForRole.ContainsValue(1)){}
        Console.WriteLine("Bazinga!");
        Program.DebugLog("Bazinga!");
    }
}
