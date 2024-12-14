using Newtonsoft.Json;
using WebWolf_Client.Networking;
using WebWolf_Client.Roles;
using WebWolf_Client.Ui;

namespace WebWolf_Client;

public class GameManager
{
    public static GameState State { get; private set; } = GameState.NoGame;
    public static InGameStateType InGameState { get; private set; } = InGameStateType.NoGame;

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

        if (PlayerData.LocalPlayer.IsHost)
        {
            // Zeigt das passende Menü an
            UiHandler.RpcUiMessage(UiMessageType.DisplayInGameMenu);
            
            // Nacht Ablauf
            if (newState == InGameStateType.Night)
            {
                Task.Delay(1000).Wait();
                foreach (var role in RoleManager.Roles)
                {
                    RoleManager.RpcCallRole(role.RoleType);
                    Task.Delay(1000).Wait();
                }
                RpcStartNightOrDay(false);
            }
            // Tag Ablauf
            else if (newState == InGameStateType.Day)
            {
                // Tote Spieler werden aufgedeckt
                var markedAsDead = PlayerData.Players.FindAll(player => player.IsMarkedAsDead);
                UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle,
                    "Die Sonne geht auf...\nBei der morgendlichen Dorfversammlung fällt euch auf, dass...\n" 
                    + (markedAsDead.Count > 0 ? string.Join(", ", markedAsDead.ConvertAll(player => player.Name)) + 
                                                " gestorben " + (markedAsDead.Count > 1 ? "sind!": "ist!") : "alle wohlauf sind!"));
                
                PlayerData.RpcProcessDeaths();
                Task.Delay(1000).Wait();
                UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle);
                Task.Delay(2000).Wait();
                
                // Am Ende des Tages wird die nächste Nacht gestartet
                if (PlayerData.LocalPlayer.IsHost)
                {
                    RpcStartNightOrDay(true);
                }
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
        {
            // Rollen werden zugewiesen und angezeigt
            RoleManager.AssignRoles();
            while (PlayerData.Players.Find(player => player.Role == RoleType.NoRole) != null) ;
            UiHandler.RpcUiMessage(UiMessageType.DisplayCardReveal);
            
            // Kurze Begrüßung
            UiHandler.RpcUiMessage(UiMessageType.DrawPlayerNameCircle, "Nach einem anstrengenden Umzug nach Düsterwald\n" +
                                                                       "fallt ihr alle müde in eure Betten.\nUnd die erste Nacht beginnt...");
            Task.Delay(1000).Wait();
            
            // Die erste Nacht beginnt
            RpcStartNightOrDay(true);
        }
    }

    public static void RpcStartNightOrDay(bool isNight)
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