using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;
using WebWolf_Client.Roles.RoleClasses;
using WebWolf_Client.Settings;
using WebWolf_Client.Ui;

namespace WebWolf_Client;

public class PlayerData
{
    // Ermittelt den lokalen Spieler mithilfe der Id des Clients
    public static PlayerData LocalPlayer => GetPlayer(NetworkingManager.Instance.CurrentId);
    // Eine Liste mit allen Spielern und ihre Informationen
    public static List<PlayerData> Players = new List<PlayerData>();
    
    // Findet einen Spieler anhand seiner ID
    public static PlayerData? GetPlayer(string id)
    {
        return Players.Find(player => player.Id == id);
    }
    
    public string Name { get; }
    public string Id { get; private set; }
    public bool IsHost { get; private set; }
    public RoleType Role { get; private set; }
    public bool IsLocal => LocalPlayer.Id == Id;
    public bool IsMarkedAsDead {get; private set; }
    public bool IsAlive {get; private set; } = true;
    public bool InLove {get; private set; }

    public PlayerData(string name, string id, bool isHost = false, RoleType role = RoleType.NoRole)
    {
        Name = name;
        Id = id;
        IsHost = isHost;
        Role = role;
    }

    // Der Spieler wird zum Host erklärt
    public void SetHost()
    {
        foreach (var playerData in Players)
        {
            playerData.IsHost = false;
        }

        IsHost = true;
    }

    // Setzt server-weit die Rolle des Spielers
    public void RpcSetRole(RoleType role)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.SetRole, 
                "{'Id': '" + Id + "', 'Role': '" + role + "'}")));
    }

    public void SetRole(RoleType role)
    {
        Role = role;
    }
    
    // Setzt die Eigenschaft bei allen Clients
    public void RpcMarkAsDead()
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.PlayerMarkedAsDead, 
                JsonConvert.SerializeObject(new Pakets.SimplePlayerId(Id)))));
    }
    
    // Setzt die Eigenschaft bei allen Clients
    public void RpcUnmarkAsDead()
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.PlayerUnmarkedAsDead, 
                JsonConvert.SerializeObject(new Pakets.SimplePlayerId(Id)))));
    }
    
    public void MarkAsDead(bool value)
    {
        IsMarkedAsDead = value;
    }
    
    // Setzt die Eigenschaft bei allen Clients
    public static void RpcProcessDeaths()
    {
        if (LocalPlayer.IsHost)
        {
            var markedAsDead = PlayerData.Players.FindAll(player => player.IsMarkedAsDead);
            NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.PlayerProcessDeaths, "")));
            
            Task.Delay(1000).Wait();
            
            // Rolle wird offenbart, sofern dies eingestellt ist
            if (SettingsManager.RevealRoleOnDeath.Value)
            {
                UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                    string.Join("\n", markedAsDead.ConvertAll(player => $"{player.Name} war {player.Role}")));
                Task.Delay(2000).Wait();
            }
            
            // Falls ein Jäger gestorben ist, wird seine Aktion ausgeführt
            var deadJäger = markedAsDead.FindAll(player => player.Role == RoleType.Jäger);
            if (deadJäger.Count > 0)
            {
                foreach (var jäger in deadJäger)
                {
                    Jäger.CallJäger(jäger);
                }
            }
            
            // Findet alle toten verliebten Personen
            var deadLovers = markedAsDead.FindAll(player => player.InLove);
            // Wenn ein Verliebter gestorben ist ...
            if (deadLovers.Count == 1)
            {
                // ... und einer noch lebt ...
                var aliveLover = Players.Find(player => player is { InLove: true, IsAlive: true });
                if (aliveLover != null)
                {
                    // ... stirbt dieser auch
                    UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, 
                        $"{aliveLover.Name} war unsterblich verliebt in {deadLovers.First().Name}." +
                        $"\nDie Nachricht über den Tod von {deadLovers.First().Name} hat {aliveLover.Name}" +
                        $"\n so sehr getroffen, dass {aliveLover.Name} sich das Leben nimmt.");
                    aliveLover.RpcMarkAsDead();
                    Task.Delay(1000).Wait();
                    PlayerData.RpcProcessDeaths();
                }
            }
        }
    }
    
    // Spieler, die als tot markiert wurden, werden auf tot gesetzt und sind damit tot
    public static void ProcessDeaths()
    {
        foreach (var player in Players)
        {
            if (player.IsMarkedAsDead)
            {
                player.IsAlive = player.IsMarkedAsDead = false;
            }
        }
    }

    // Setzt bei allen Clients, dass dieser Spieler verliebt ist
    public void RpcSetInLove()
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPaket(NetworkingManager.Instance.CurrentId, PaketDataType.PlayerInLove, 
                JsonConvert.SerializeObject(new Pakets.SimplePlayerId(Id)))));
    }
    
    public void SetInLove()
    {
        InLove = true;
    }
}