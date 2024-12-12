using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;

namespace WebWolf_Client;

public class GameManager
{
    public static GameState State { get; private set; } = GameState.NoGame;
    public static InGameStateType InGameState { get; private set; } = InGameStateType.NoGame;
    
    // Liste an Spielern die in der Nacht gestorben sind
    public static List<string> DeadPlayersDuringNight { get; } = new List<string>();

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

        // Zeigt das passende Menü an
        UiHandler.DisplayInGameMenu();
        
        // Nacht Ablauf
        if (newState == InGameStateType.Night)
        {
            if (PlayerData.LocalPlayer.IsHost)
            {
                DeadPlayersDuringNight.Clear();
                
                Task.Delay(1000).Wait();
                foreach (var role in RoleManager.Roles)
                {
                    RoleManager.CallRole(role.RoleType);
                    Task.Delay(1000).Wait();
                }
                StartNightOrDay(false);
            }
        }
        // Tag Ablauf
        else if (newState == InGameStateType.Day)
        {
            // Toten Spieler werden aufgedeckt
            var center = UiHandler.DrawPlayerNameCircle();
            UiHandler.RenderTextAroundPoint(center, "Die Sonne geht auf...\nBei der morgendlichen Dorfversammlung fällt euch auf, dass...\n"
                                                    + (DeadPlayersDuringNight.Count > 0 ? string.Join(", ", DeadPlayersDuringNight.ConvertAll(playerId => PlayerData.GetPlayer(playerId).Name)) +
                                                        " gestorben " + (DeadPlayersDuringNight.Count > 1 ? "sind!": "ist!") : "alle wohlauf sind!"));
            
            foreach (var playerId in DeadPlayersDuringNight)
            {
                PlayerData.GetPlayer(playerId).IsAlive = false;
            }
            DeadPlayersDuringNight.Clear();
            Task.Delay(1000).Wait();
            UiHandler.DrawPlayerNameCircle();
            Task.Delay(2000).Wait();
            
            // Am Ende des Tages wird die nächste Nacht gestartet
            if (PlayerData.LocalPlayer.IsHost)
            {
                StartNightOrDay(true);
            }
        }
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
        }
    }

    public static void StartNightOrDay(bool isNight)
    {
        NetworkingManager.Instance.Client.Send(JsonConvert.SerializeObject(
            new BroadcastPacket(NetworkingManager.Instance.CurrentId, PacketDataType.StartNightOrDay, JsonConvert.SerializeObject(new Packets.SimpleBoolean(isNight)))));
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