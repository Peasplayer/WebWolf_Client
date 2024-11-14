using System.Security.Cryptography;
using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

class Program
{
    static void Main(string[] args)
    {
        RenderLogo();
        
        AnsiConsole.WriteLine("");
        PlayerData.LocalPlayer = new PlayerData(ChooseName(), null);
        
        var net = new NetworkingManager();
        net.StartConnection("ws://localhost:8443/json");
        Console.ReadKey();
    }

    private static void RenderLogo()
    {
        var image = new CanvasImage("C:\\Users\\je446\\Downloads\\Werwolf.jpg");
        
        image.MaxWidth(30);
        AnsiConsole.Write(image);
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
