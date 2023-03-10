using System.CommandLine;
using Core.Commands;
using Core.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Console.ConsoleCommands;

public class SensorConsoleCommand : IConsoleCommand
{
    private readonly ILogger<SensorConsoleCommand> _logger;
    private readonly IMediator _mediator;

    public SensorConsoleCommand(IMediator mediator, ILogger<SensorConsoleCommand> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public Command GetCommandLineCommand()
    {
        var command = new Command("sensor", "Sensor actions.");
        command.AddCommand(GetListSubCommand());
        command.AddCommand(GetCreateSubCommand());
        return command;
    }

    private Command GetListSubCommand()
    {
        var subCommand = new Command("list", "List sensors.");

        subCommand.SetHandler(List);

        return subCommand;
    }

    private async Task List()
    {
        var results = await _mediator.Send(new SensorsQuery());

        foreach (var result in results)
        {
            _logger.LogInformation("{Id} {Uid} {DevEui}",
                result.Id, result.Uid, result.DevEui);
            System.Console.WriteLine("{0} {1}",
                result.Uid, result.DevEui);
        }
    }

    private Command GetCreateSubCommand()
    {
        var subCommand = new Command("create", "Create sensor.");

        var idOption = new Option<Guid?>(new[] { "-i", "--si", "--sensorid" }, "Sensor identifier");
        subCommand.AddOption(idOption);

        var devEuiOption = new Option<string>(new[] { "-d", "--deveui" }, "Sensor DevEui")
        {
            IsRequired = true
        };
        subCommand.AddOption(devEuiOption);

        subCommand.SetHandler(
            Create,
            idOption, devEuiOption);

        return subCommand;
    }

    private async Task Create(Guid? id, string devEui)
    {
        var uid = id ?? Guid.NewGuid();

        await _mediator.Send(
            new CreateSensorCommand
            {
                Uid = uid,
                DevEui = devEui
            });

        System.Console.WriteLine("{0}", uid);
    }
}