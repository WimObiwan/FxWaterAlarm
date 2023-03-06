using System.CommandLine;

namespace Console;

internal interface ICommandLineEngine
{
    Task<int> Execute(string[] args);
}

internal interface IConsoleCommand
{
    Command GetCommandLineCommand();
}

internal class CommandLineEngine : ICommandLineEngine
{
    private readonly IEnumerable<IConsoleCommand> _commands;

    public CommandLineEngine(IEnumerable<IConsoleCommand> commands)
    {
        _commands = commands;
    }

    public async Task<int> Execute(string[] args)
    {
        var rootCommand = new RootCommand(
            "Console tool to manage WaterAlarm.");

        foreach (var consoleCommand in _commands) rootCommand.AddCommand(consoleCommand.GetCommandLineCommand());

        return await rootCommand.InvokeAsync(args);
    }
}