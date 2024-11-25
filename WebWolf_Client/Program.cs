using System.Diagnostics;
using System.Net.WebSockets;
using Websocket.Client;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

class Program
{
    public static bool KeepAlive;
    
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => DebugLog($"Unhandled Exception: {eventArgs.ExceptionObject}");
        var isConnected = UiHandler.StartGameMenu();
        if (isConnected)
        {
            KeepAlive = true;
            NetworkingManager.Instance.InitialConnectionSuccessful = true;
            GameManager.ChangeState(GameManager.GameState.InLobby);
            // Lobby wird erst angezeigt, wenn die Spieler mit dem Client synchronisiert sind
            while(!NetworkingManager.Instance.ArePlayersSynced) {}
            UiHandler.DisplayLobby();
        }
        else
        {
            UiHandler.DisplayDisconnectionScreen(new DisconnectionInfo(DisconnectionType.Error, 
                WebSocketCloseStatus.EndpointUnavailable, "Connection failed", 
                null, null));
        }

        while (KeepAlive) { }

        DebugLog("PROGRAM ENDED");
        Console.ReadKey();
        if (NetworkingManager.Instance.Client.IsRunning)
        {
            NetworkingManager.Instance.Client.Stop(WebSocketCloseStatus.NormalClosure, "Programm ended");
        }
    }

    // Schreibt eine Debug-Nachricht in eine Log-Datei die zu dem aktuellen Programm gehört
    public static void DebugLog(string log)
    {
        File.AppendAllText($"$./debug-{Process.GetCurrentProcess().Id}.log", $"[{DateTime.Now}] {log}\n");
    }
}
