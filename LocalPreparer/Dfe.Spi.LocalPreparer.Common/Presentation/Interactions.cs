using System.Text;
namespace Dfe.Spi.LocalPreparer.Common.Presentation;

public static class Interactions
{
    private static string _value = string.Empty;

    public static T Input<T>(string prompt, string? defaultValue = null, bool isPassword = false, bool validationFailed = false, ConsoleColor colour = ConsoleColor.White, Action? escAction = null)
    {
        if (!validationFailed)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(Environment.NewLine + prompt);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            if (!string.IsNullOrEmpty(defaultValue)) Console.WriteLine($"Current value: {defaultValue}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Please enter a valid value!");
            Console.ResetColor();
        }
        Console.Write("> ");

        var isValid = TryReadLine(out string result);
        if (!isValid && escAction != null)
            escAction.Invoke();
        _value = result;

        if (typeof(T) != typeof(string)) return default;
        if (string.IsNullOrEmpty(_value))
            return Input<T>(prompt, defaultValue, isPassword, true, colour, escAction);
        return (T)Convert.ChangeType(_value, typeof(string));
    }


    private static bool TryReadLine(out string result)
    {
        var buf = new StringBuilder();
        for (; ; )
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                result = "";
                return false;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                result = buf.ToString();
                Console.Write(Environment.NewLine);
                return true;
            }
            else if (key.Key == ConsoleKey.Backspace && buf.Length > 0)
            {
                buf.Remove(buf.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (key.KeyChar != 0)
            {
                buf.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }
        }
    }


    public static void RaiseError(List<string> invalidItems, Action? escAction)
    {
        Console.WriteLine($"{Environment.NewLine}There is a problem with executing selected operation, please review and fix the following:{Environment.NewLine}");
        foreach (var item in invalidItems)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" - {item}");
            Console.ResetColor();
        }
        if (escAction != null)
        {
            Console.WriteLine($"{Environment.NewLine} Press any key to continue...");
            Console.ReadLine();
            escAction.Invoke();
        }
    }

    public static void WriteColourLine(string value, ConsoleColor colour)
    {
        Console.ForegroundColor = colour;
        Console.WriteLine(value);
        Console.ResetColor();
    }

}
