namespace WebWolf_Client;

public class ConsoleUtils
{
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