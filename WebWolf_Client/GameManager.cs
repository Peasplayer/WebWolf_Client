using WebWolf_Client.Networking;

namespace WebWolf_Client;

public class GameManager
{
    public static GameState State { get; private set; } = GameState.NoGame;

    public static void ChangeState(GameState newState)
    {
        State = newState;
    }

    public static void OnPlayerJoin(Packets.PlayerDataPattern data)
    {
        Program.DebugLog($"Player {data.Name} joined with ID {data.ID}");
        PlayerData.Players.Add(new PlayerData(data.Name, data.ID));

        if (GameManager.State == GameState.InLobby)
        {
            UiHandler.DisplayLobby(false);
        }
    }
    
    public static void OnPlayerLeave(Packets.PlayerDataPattern data)
    {
        Program.DebugLog($"Player {data.Name} left with ID {data.ID}");
        PlayerData.Players.Remove(PlayerData.GetPlayer(data.ID));

        if (GameManager.State == GameState.InLobby)
        {
            UiHandler.DisplayLobby(false);
        }
    }
    
    public enum GameState
    {
        NoGame,
        InLobby,
        InGame
    }
}