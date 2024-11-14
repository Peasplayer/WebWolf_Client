using System.Security.Cryptography;
using Spectre.Console;
using WebWolf_Client.Networking;

namespace WebWolf_Client;

class Program
{
    private static string Name;
    static void Main(string[] args)
    {
        RenderLogo();
        
        AnsiConsole.Write("Suche dir einen Spielername aus");
        Name = ChooseName();
        
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
        var name = AnsiConsole.Ask<string>("");
        if (name.Length > 15)
        {
            AnsiConsole.Clear();
            RenderLogo();
            AnsiConsole.MarkupLine("[red]Der Name darf maximal 15 Zeichen lang sein![/]");
            return ChooseName();
        }
        
        if (name.Contains(" "))
        {
            AnsiConsole.Clear();
            RenderLogo();
            AnsiConsole.MarkupLine("[red]Der Name darf keine Leerzeichen enthalten![/]");
            return ChooseName(); 
        }

        return name;
    }
    
}
