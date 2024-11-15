using System.Reflection;
using System.Security.Cryptography;
using Spectre.Console;
using WebSocketSharp;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

class Program
{
    static void Main(string[] args)
    {
        var isConnected = UIHandler.StartGameMenu();
        if (isConnected)
        {
            UIHandler.DisplayLobby();
        }
        
        Console.ReadKey();
    }
    
    public static string ChooseName()
    {
        var name = AnsiConsole.Ask<string>("Suche dir einen Spielernamen aus: ");
        if (name.Length > 15)
        {
            
            ConsoleUtils.ClearConsoleLine(2);
            AnsiConsole.MarkupLine("[red]Der Name darf maximal 15 Zeichen lang sein![/]");
            return ChooseName();
        }
        
        if (name.Contains(" "))
        {
           
            ConsoleUtils.ClearConsoleLine(2);
            AnsiConsole.MarkupLine("[red]Der Name darf keine Leerzeichen enthalten![/]");
            return ChooseName(); 
        }

        return name;
    }
}
