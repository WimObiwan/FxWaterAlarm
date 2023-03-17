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
        command.AddCommand(ListSubCommand());
        command.AddCommand(CreateSubCommand());
        command.AddCommand(SetLinkSubCommand());
        command.AddCommand(ReadLastMeasurementSubCommand());
        command.AddCommand(ReadMeasurementsSubCommand());
        return command;
    }

    private Command ListSubCommand()
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
            _logger.LogInformation("{Id} {Uid} {DevEui} {Link}",
                result.Id, result.Uid, result.DevEui, result.Link);
            System.Console.WriteLine("{0} {1} {2}",
                result.Uid, result.DevEui, result.Link);
        }
    }

    private Command CreateSubCommand()
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

    private Command SetLinkSubCommand()
    {
        var subCommand = new Command("setlink", "Set link.");

        var idOption = new Option<Guid>(new[] { "-i", "--si", "--sensorid" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(idOption);

        var linkOption = new Option<string?>(new[] { "-l", "--link" }, "Sensor link");
        subCommand.AddOption(linkOption);

        subCommand.SetHandler(
            SetLink,
            idOption, linkOption);

        return subCommand;
    }

    private async Task SetLink(Guid uid, string? link)
    {
        await _mediator.Send(
            new RegenerateSensorLinkCommand
            {
                SensorUid = uid,
                Link = link
            });

        System.Console.WriteLine("{0}", uid);
    }

    private Command ReadLastMeasurementSubCommand()
    {
        var subCommand = new Command("readlastmeasurement", "Read last measurement.");

        var devEuiOption = new Option<string>(new[] { "-d", "--deveui" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(devEuiOption);

        subCommand.SetHandler(
            ReadLastMeasurement,
            devEuiOption);

        return subCommand;
    }

    private async Task ReadLastMeasurement(string devEui)
    {
        var result = await _mediator.Send(
            new LastMeasurementQuery
            {
                DevEui = devEui
            });

        if (result == null)
        {
            _logger.LogWarning("No measurement found for DevEui {DevEui}",
                devEui);
            return;
        }

        _logger.LogInformation("{DevEui} {Timestamp} {DistanceMm} {BatV} {RssiDbm}",
            result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
        System.Console.WriteLine("{0} {1} {2} {3} {4}",
            result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
    }

    private Command ReadMeasurementsSubCommand()
    {
        var subCommand = new Command("readmeasurements", "Read measurements.");

        var devEuiOption = new Option<string>(new[] { "-d", "--deveui" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(devEuiOption);

        var fromOption = new Option<DateTime>(new[] { "-f", "--from" }, "From")
        {
            IsRequired = true
        };
        subCommand.AddOption(fromOption);

        var tillOption = new Option<DateTime?>(new[] { "-t", "--till" }, "Till date");
        subCommand.AddOption(tillOption);

        subCommand.SetHandler(
            ReadMeasurements,
            devEuiOption,
            fromOption,
            tillOption);

        return subCommand;
    }

    private async Task ReadMeasurements(string devEui, DateTime from, DateTime? till)
    {
        var results = await _mediator.Send(
            new MeasurementsQuery
            {
                DevEui = devEui,
                From = from,
                Till = till
            });

        foreach (var result in results)
        {
            _logger.LogInformation("{DevEui} {Timestamp} {DistanceMm} {BatV} {RssiDbm}",
                result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
            System.Console.WriteLine("{0} {1} {2} {3} {4}",
                result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
        }
    }
}