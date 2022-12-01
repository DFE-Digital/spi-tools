namespace Dfe.Spi.LocalPreparer.Common.Utils;

public static class Logo
{
    public static void Display()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($@"
  _____   __        _____       _ 
 |  __ \ / _|      / ____|     (_)
 | |  | | |_ ___  | (___  _ __  _ 
 | |  | |  _/ _ \  \___ \| '_ \| |
 | |__| | ||  __/_ ____) | |_) | |
 |_____/|_| \___(_)_____/| .__/|_|
                         | |      
                         |_| ver{StringExtensions.GetAppVersion()}", Console.ForegroundColor);

        Console.ResetColor();
    }
}
