using WebWolf_Client.Networking;
using WebWolf_Client.Roles;

namespace WebWolf_Client;

public class GameManager
{
    public static GameState State { get; private set; } = GameState.NoGame;

    public static void ChangeState(GameState newState)
    {
        State = newState;
        if (newState == GameState.InLobby)
        {
            UiHandler.ResetLobby();
        }
        else if (newState == GameState.InGame)
        {
            RoleManager.AssignRoles();
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
    
    public enum GameState
    {
        NoGame,
        InLobby,
        InGame
    }
}