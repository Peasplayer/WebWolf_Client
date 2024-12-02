namespace WebWolf_Client.Roles;

public class RoleManager
{
    public static List<Role> Roles { get; set; } = new List<Role>()
    {
        new Seherin()
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
}
