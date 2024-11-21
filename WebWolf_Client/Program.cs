using System.Diagnostics;
using System.Net.WebSockets;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

class Program
{
    static void Main(string[] args)
    {
        var isConnected = UiHandler.StartGameMenu();
        if (isConnected)
        {
            GameManager.ChangeState(GameManager.GameState.InLobby);
            UiHandler.DisplayLobby();
        }
        
        Console.ReadKey();
    }

    public static void DebugLog(string log)
    {
        File.AppendAllText("./debug.log", $"[{DateTime.Now}][{Process.GetCurrentProcess().Id}] {log}\n");
    }
}
