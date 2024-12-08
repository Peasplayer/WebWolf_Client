using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;

namespace WebWolf_Client;

public class GameManager
{
    public static GameState State { get; private set; } = GameState.NoGame;
    public static InGameStateType InGameState { get; private set; } = InGameStateType.NoGame;
    
    public static Dictionary<string, int> WaitingForRole { get; } = new Dictionary<string, int>();

    public static void ChangeState(GameState newState)
    {
        State = newState;
        if (newState == GameState.InLobby)
        {
            UiHandler.ResetLobby();
        }
        else if (newState == GameState.InGame)
        {
            OnGameStart();
        }
    }
    
    public static void ChangeInGameState(InGameStateType newState)
    {
        InGameState = newState;
    }

    public static void OnPlayerJoin(Packets.PlayerDataPattern data)
    {
        Program.DebugLog($"Player {data.Name} joined with ID {data.Id}");
        PlayerData.Players.Add(new PlayerData(data.Name, data.Id));

        if (GameManager.State == GameState.InLobby)
        {
            UiHandler.DisplayLobby();
        }
    }
    
    public static void OnPlayerLeave(Packets.PlayerDataPattern data)
    {
        Program.DebugLog($"Player {data.Name} left with ID {data.Id}");
        PlayerData.Players.Remove(PlayerData.GetPlayer(data.Id));

        if (GameManager.State == GameState.InLobby)
        {
            UiHandler.DisplayLobby();
        }
    }

    public static void OnGameStart()
    {
        if (PlayerData.LocalPlayer.IsHost)
            RoleManager.AssignRoles();
        
        while (PlayerData.Players.Find(player => player.Role == RoleType.NoRole) != null) ;

        UiHandler.DisplayCardReveal();
        
        var center = UiHandler.DrawPlayerNameCircle();
        UiHandler.RenderTextAroundPoint(center, "Nach einem anstrengenden Umzug nach Düsterwald\nfallt ihr alle müde in eure Betten.\nUnd die erste Nacht beginnt...");
        Task.Delay(1000).Wait();
        
        if (PlayerData.LocalPlayer.IsHost)
        {
            StartNightOrDay(true);
            Task.Delay(1000).Wait();
            CallRole(RoleType.Seherin);
        }
    }

    public static void StartNightOrDay(bool isNight)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.StartNightOrDay, JsonConvert.SerializeObject(new Packets.SimpleBoolean(isNight)))));
    }
    
    public static void CallRole(RoleType role)
    {
        WaitingForRole.Clear();
        foreach (var playerData in PlayerData.Players)
        {
            if (playerData.Role == role)
                WaitingForRole.Add(playerData.Id, 0);
        }

        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.CallRole, JsonConvert.SerializeObject(new Packets.SimpleRole(role)))));

        var timer = Task.Delay(14000);
        while (WaitingForRole.Count > 0)
        {
            if (timer.IsCompleted)
            {
                foreach (var player in WaitingForRole)
                {
                    if (player.Value == 0)
                        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
                            new SendToPacket(NetworkingManager.Instance.CurrentId, PacketDataType.RoleCanceled, JsonConvert.SerializeObject(new Packets.SimpleRole(role)), player.Key)));
                }
                break;
            }
        }
        
        while (WaitingForRole.Count > 0){}
        Console.WriteLine("Bazinga!");
        Program.DebugLog("Bazinga!");
    }
    
    public enum GameState
    {
        NoGame,
        InLobby,
        InGame
    }

    public enum InGameStateType
    {
        NoGame,
        Day,
        Night
    }
}