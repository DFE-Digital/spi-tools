using static System.Console;

namespace Dfe.Spi.LocalPreparer.Common.Presentation;

public class Navigation<T>
{

    private int _selectedIndex;
    private Dictionary<string, T> _options;
    private string _prompt;
    private bool _clearLogo;

    public Navigation(Dictionary<string, T> options, string prompt, bool clearLogo = false)
    {
        _options = options;
        _prompt = prompt;
        _selectedIndex = 0;
        _clearLogo = clearLogo;
    }


    private void DisplayOptions()
    {
        if (!string.IsNullOrEmpty(_prompt))
            WriteLine(_prompt + Environment.NewLine);
        for (int i = 0; i < _options.Count; i++)
        {
            var currentOption = _options.ElementAt(i);
            if (i == _selectedIndex)
            {
                ForegroundColor = ConsoleColor.Black;
                BackgroundColor = ConsoleColor.White;
            }
            else
            {
                ForegroundColor = ConsoleColor.White;
                BackgroundColor = ConsoleColor.Black;
            }
            WriteLine($"  {currentOption.Key}  ");
        }
        ResetColor();
    }


    public T Run(bool getValue = false)
    {

        ConsoleKey keyPressed;
        do
        {
            if (!_clearLogo)
                ClearWithLogo();
            DisplayOptions();

            ConsoleKeyInfo keyInfo = ReadKey(true);
            keyPressed = keyInfo.Key;

            if (keyPressed == ConsoleKey.UpArrow)
            {
                _selectedIndex--;
                if (_selectedIndex == -1)
                {
                    _selectedIndex = _options.Count - 1;
                }
            }
            else if (keyPressed == ConsoleKey.DownArrow)
            {
                _selectedIndex++;
                if (_selectedIndex == _options.Count)
                {
                    _selectedIndex = 0;
                }
            }

        } while (keyPressed != ConsoleKey.Enter);

        if (getValue)
            return (T)(object)_options.ElementAt(_selectedIndex).Value;

        return (T)(object)_options.ElementAt(_selectedIndex).Key;


    }

    public void ClearWithLogo()
    {
        Clear();
        Logo.Display();
        Console.WriteLine(Environment.NewLine);
    }

}
