using System.Reflection;
using Spectre.Console;

namespace WebWolf_Client;

public class ConsoleUtils
{
    public static void RenderLogo()
    {
        var image =
            new CanvasImage(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("WebWolf_Client.Resources.Werwolf.jpg") ?? throw new InvalidOperationException());

        image.MaxWidth(20);//((int) (Console.WindowHeight * 0.6f));
        AnsiConsole.Write(new Align(image, HorizontalAlignment.Center, VerticalAlignment.Top));
        AnsiConsole.Write(new FigletText("WebWolf").Centered().Color(Color.RosyBrown));
    }
    
    public static void ClearConsoleLine(int line = 1)
    {
        int currentLineCursor = Console.CursorTop;
        for (int i = 0; i < line; i++)
        {
            Console.SetCursorPosition(0, currentLineCursor - i);
            Console.Write(new string(' ', Console.WindowWidth)); 
        }
        Console.SetCursorPosition(0, currentLineCursor - line);
    }
}