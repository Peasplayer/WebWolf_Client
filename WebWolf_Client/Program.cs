using System.Diagnostics;
using System.Net.WebSockets;
using Websocket.Client;
using WebWolf_Client.Networking;
using WebWolf_Client.Ui;

namespace WebWolf_Client;

class Program
{
    public const string URL = "ws://77.90.17.73:8443";

    public static List<string> DebugNames = new List<string>()
        { "Horst", "Dieter", "Wilfred", "Lennox", "Jannis", "Dreschner", "Martinez", "Benutzer", "Helmut", "Günther", "Tom" };
    
    public static bool KeepAlive;
    
    static void Main(string[] args)
    {
        // Zum Debuggen, so können die Logs zugeordnet werden
        Console.Title = $"WebWolf Client ({Process.GetCurrentProcess().Id})";
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => DebugLog($"Unhandled Exception: {eventArgs.ExceptionObject}");
        var isConnected = UiHandler.StartGameMenu();
        if (isConnected)
        {
            KeepAlive = true;
            NetworkingManager.Instance.InitialConnectionSuccessful = true;
            GameManager.ChangeState(GameManager.GameState.InLobby);
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
        try
        {
            File.AppendAllText($"./debug-{Process.GetCurrentProcess().Id}.log", $"[{DateTime.Now}] {log}\n");
        }
        catch (Exception _)
        {
            DebugLog($"Exception during logging: {_}");
            DebugLog(log);
        }
    }
}
