using System.Diagnostics;
using System.Net.WebSockets;
using Spectre.Console;
using WebWolf_Client.Networking;
using WebWolf_Client.Ui;

namespace WebWolf_Client;

class Program
{

    public static bool KeepAlive;
    
    static void Main(string[] args)
    {
        // Zum Debuggen, so können die Logs zugeordnet werden
        Console.Title = $"WebWolf Client ({Process.GetCurrentProcess().Id})";
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => DebugLog($"Unhandled Exception: {eventArgs.ExceptionObject}");
        AnsiConsole.Cursor.Hide();
        
        UiHandler.DisplayMainMenu();

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
        {var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\WebWolf";
                     if (!Directory.Exists(path))
                         Directory.CreateDirectory(path);
                     File.AppendAllText(path + $"\\debug-{Process.GetCurrentProcess().Id}.log", $"[{DateTime.Now}] {log}\n");
            
        }
        catch (Exception _)
        {
            // ignored
        }
    }
}
