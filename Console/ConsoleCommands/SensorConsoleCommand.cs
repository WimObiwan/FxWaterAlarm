using System.CommandLine;
using Core.Commands;
using Core.Entities;
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
        command.AddCommand(ReadSubCommand());
        command.AddCommand(CreateSubCommand());
        command.AddCommand(SetLinkSubCommand());
        command.AddCommand(ReadLastMeasurementSubCommand());
        command.AddCommand(ReadLastMedianMeasurementSubCommand());
        command.AddCommand(ReadAggregatedMeasurementsSubCommand());
        command.AddCommand(ReadMeasurementTrendsSubCommand());
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

    private Command ReadSubCommand()
    {
        var subCommand = new Command("read", "Read sensor.");

        var idOption = new Option<Guid>(new[] { "-i", "--si", "--sensorid" }, "Account id")
        {
            IsRequired = true
        };
        subCommand.AddOption(idOption);

        subCommand.SetHandler(
            Read,
            idOption);

        return subCommand;
    }

    private async Task Read(Guid id)
    {
        var sensor = await _mediator.Send(
            new SensorQuery
            {
                Uid = id
            });

        ShowSensor(sensor);
    }

    private void ShowSensor(Sensor? sensor)
    {
        if (sensor == null)
        {
            System.Console.WriteLine("Sensor not found");
        }
        else
        {
            System.Console.WriteLine($"Sensor uid: {sensor.Uid}");
            System.Console.WriteLine($"  - DevEui:    {sensor.DevEui}");
            System.Console.WriteLine($"  - Link:    {sensor.Link}");
            System.Console.WriteLine($"  - Create:  {sensor.CreateTimestamp}");
            System.Console.WriteLine($"  - Accounts: {sensor.AccountSensors?.Count}");
            if (sensor.AccountSensors != null)
                foreach (var accountSensor in sensor.AccountSensors)
                {
                    System.Console.WriteLine($"    - Uid:     {accountSensor.Account.Uid}");
                    System.Console.WriteLine($"      - DevEui:  {accountSensor.Account.Email}");
                }
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

    private Command ReadLastMedianMeasurementSubCommand()
    {
        var subCommand = new Command("readlastmedianmeasurement", "Read last measurement.");

        var devEuiOption = new Option<string>(new[] { "-d", "--deveui" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(devEuiOption);

        var fromOption = new Option<DateTime?>(new[] { "-f", "--from" }, "From date");
        subCommand.AddOption(fromOption);

        var durtionOption = new Option<TimeSpan?>(new[] { "-l", "--duration" }, "Duration");
        subCommand.AddOption(durtionOption);

        subCommand.SetHandler(
            ReadLastMedianMeasurement,
            devEuiOption,
            fromOption,
            durtionOption);

        return subCommand;
    }

    private async Task ReadLastMedianMeasurement(string devEui, DateTime? from, TimeSpan? duration)
    {
        DateTime from2;
        if (from.HasValue && !duration.HasValue)
            from2 = from.Value;
        else if (duration.HasValue && !from.HasValue)
            from2 = DateTime.UtcNow.Add(-duration.Value);
        else
        {
            _logger.LogError("From or duration necessary");
            return;
        }
        var result = await _mediator.Send(
            new LastMedianMeasurementQuery
            {
                DevEui = devEui,
                From = from2
            });

        if (result == null)
        {
            _logger.LogWarning("No measurement found for DevEui {DevEui}",
                devEui);
            return;
        }

        _logger.LogInformation("{DevEui} {Timestamp} {DistanceMm} {BatV} {RssiDbm}",
            result.DevEui, result.Timestamp, result.MeanDistanceMm, result.BatV, result.RssiDbm);
        System.Console.WriteLine("{0} {1} {2} {3} {4}",
            result.DevEui, result.Timestamp, result.MeanDistanceMm, result.BatV, result.RssiDbm);
    }

    private Command ReadAggregatedMeasurementsSubCommand()
    {
        var subCommand = new Command("readaggregatedmeasurements", "Read aggregated measurements.");

        var devEuiOption = new Option<string>(new[] { "-d", "--deveui" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(devEuiOption);

        var intervalOption = new Option<TimeSpan>(new[] { "-i", "--interval" }, "Interval duration")
        {
            IsRequired = true
        };
        subCommand.AddOption(intervalOption);

        var fromOption = new Option<DateTime?>(new[] { "-f", "--from" }, "From date");
        subCommand.AddOption(fromOption);

        var tillOption = new Option<DateTime?>(new[] { "-t", "--till" }, "Till date");
        subCommand.AddOption(tillOption);

        subCommand.SetHandler(
            ReadAggregatedMeasurements,
            devEuiOption,
            intervalOption,
            fromOption,
            tillOption);

        return subCommand;
    }

    private async Task ReadMeasurements(string devEui, DateTime? from, DateTime? till)
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

    private async Task ReadAggregatedMeasurements(string devEui, TimeSpan interval, DateTime? from, DateTime? till)
    {
        var results = await _mediator.Send(
            new AggregatedMeasurementsQuery
            {
                DevEui = devEui,
                From = from,
                Till = till,
                Interval = interval
            });

        foreach (var result in results)
        {
            _logger.LogInformation("{DevEui} {Timestamp} {DistanceMm} {BatV} {RssiDbm}",
                result.DevEui, result.Timestamp, result.LastDistanceMm, result.BatV, result.RssiDbm);
            System.Console.WriteLine("{0} {1} {2} {3} {4}",
                result.DevEui, result.Timestamp, result.LastDistanceMm, result.BatV, result.RssiDbm);
        }
    }

    private Command ReadMeasurementTrendsSubCommand()
    {
        var subCommand = new Command("readmeasurementtrends", "Read last measurement.");

        var devEuiOption = new Option<string>(new[] { "-d", "--deveui" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(devEuiOption);

        var timestampOption = new Option<DateTime>(new[] { "-t", "--timestamp" }, "Timestamp")
        {
            IsRequired = true
        };
        subCommand.AddOption(timestampOption);

        subCommand.SetHandler(
            ReadMeasurementTrends,
            devEuiOption,
            timestampOption);

        return subCommand;
    }

    private async Task ReadMeasurementTrends(string devEui, DateTime timestamp)
    {
        var result = await _mediator.Send(
            new MeasurementLastBeforeQuery()
            {
                DevEui = devEui,
                Timestamp = timestamp
            });

        if (result != null)
        {
            _logger.LogInformation("{DevEui} {Timestamp} {DistanceMm} {BatV} {RssiDbm}",
                result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
            System.Console.WriteLine("{0} {1} {2} {3} {4}",
                result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
        }
    }
}