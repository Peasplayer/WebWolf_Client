using System.Diagnostics;

namespace WebWolf_Client;

class Program
{
    static void Main(string[] args)
    {
        var isConnected = UiHandler.StartGameMenu();
        if (isConnected)
        {
            GameManager.ChangeState(GameManager.GameState.InLobby);
            UiHandler.DisplayLobby(true);
        }
        
        Console.ReadKey();
    }

    public static void DebugLog(string log)
    {
        File.AppendAllText("./debug.log", $"[{DateTime.Now}][{Process.GetCurrentProcess().Id}] {log}\n");
    }
}
