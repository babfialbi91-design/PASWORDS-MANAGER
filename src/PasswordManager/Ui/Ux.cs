using PasswordManager.Services;
using Spectre.Console;

namespace PasswordManager.Ui;

public static class Ux
{
    public static void Header(string title, string subtitle = "")
    {
        try { Console.Clear(); } catch (IOException) { /* مخرج مُعاد توجيهه */ }
        var rule = new Rule($"[bold deepskyblue2]{title.EscapeMarkup()}[/]")
        {
            Justification = Justify.Left,
            Style = new Style(foreground: Color.Grey)
        };
        AnsiConsole.Write(rule);
        if (!string.IsNullOrEmpty(subtitle))
            AnsiConsole.MarkupLine($"[grey]{subtitle.EscapeMarkup()}[/]\n");
    }

    public static void Info(string message) => AnsiConsole.MarkupLine($"[grey]ⓘ {message.EscapeMarkup()}[/]");

    public static void Success(string message) => AnsiConsole.MarkupLine($"[green]✓ {message.EscapeMarkup()}[/]");

    public static void Warn(string message) => AnsiConsole.MarkupLine($"[orange1]⚠ {message.EscapeMarkup()}[/]");

    public static void Error(string message) => AnsiConsole.MarkupLine($"[red]✗ {message.EscapeMarkup()}[/]");

    public static void PressToContinue(string message = "اضغط أي مفتاح للمتابعة...")
    {
        AnsiConsole.MarkupLine($"\n[grey]{message.EscapeMarkup()}[/]");
        try { Console.ReadKey(true); } catch (InvalidOperationException) { Thread.Sleep(1500); }
    }

    public static bool Confirm(string question, bool defaultValue = false)
    {
        return AnsiConsole.Confirm(question.EscapeMarkup(), defaultValue);
    }

    public static string Ask(string question, string? defaultValue = null)
    {
        var prompt = new TextPrompt<string>(question.EscapeMarkup());
        if (defaultValue is not null)
            prompt.DefaultValue(defaultValue);
        return prompt.Show(AnsiConsole.Console);
    }

    public static string AskSecret(string question)
    {
        return new TextPrompt<string>(question.EscapeMarkup())
            .Secret('•')
            .Show(AnsiConsole.Console);
    }

    public static T Pick<T>(string title, IEnumerable<T> choices) where T : notnull
    {
        var selection = new SelectionPrompt<T>()
            .Title(title.EscapeMarkup())
            .PageSize(10)
            .UseConverter(x => x?.ToString() ?? string.Empty);
        selection.AddChoices(choices);
        return AnsiConsole.Prompt(selection);
    }

    public static string StrengthLabel(string password)
        => PasswordQuality.StrengthLabel(password);

    public static string StrengthColor(string password)
    {
        return PasswordQuality.StrengthLabel(password) switch
        {
            "قوية جداً" => "green",
            "قوية" => "green",
            "متوسطة" => "yellow",
            "ضعيفة" => "red",
            _ => "grey"
        };
    }

    public static string SafeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? "[grey]—[/]" : value.EscapeMarkup();
}
