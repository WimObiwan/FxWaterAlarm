using System.CommandLine;
using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Console.ConsoleCommands;

public class AccountConsoleCommand : IConsoleCommand
{
    private readonly ILogger<AccountConsoleCommand> _logger;
    private readonly IMediator _mediator;

    public AccountConsoleCommand(IMediator mediator, ILogger<AccountConsoleCommand> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public Command GetCommandLineCommand()
    {
        var command = new Command("account", "Account actions.");
        command.AddCommand(ListSubCommand());
        command.AddCommand(ReadSubCommand());
        command.AddCommand(ReadByLinkSubCommand());
        command.AddCommand(CreateSubCommand());
        command.AddCommand(UpdateSubCommand());
        command.AddCommand(SetLinkSubCommand());
        command.AddCommand(ListSensorsSubCommand());
        command.AddCommand(AddSensorSubCommand());
        command.AddCommand(UpdateSensorSubCommand());
        command.AddCommand(RemoveSensorSubCommand());
        command.AddCommand(CheckSensorAlarmsSubCommand());
        command.AddCommand(CheckAllSensorAlarmsSubCommand());
        return command;
    }

    private Command ListSubCommand()
    {
        var subCommand = new Command("list", "List accounts.");

        subCommand.SetHandler(List);

        return subCommand;
    }

    private async Task List()
    {
        var results = await _mediator.Send(new AccountsQuery());

        foreach (var result in results)
        {
            _logger.LogInformation("Account: {Id} {Uid} {Name} {Email} {Link}",
                result.Id, result.Uid, result.Name, result.Email, result.Link);
            System.Console.WriteLine($"{result.Uid} {result.Name} {result.Email} {result.Link}");
        }
    }

    private Command ReadSubCommand()
    {
        var subCommand = new Command("read", "Read account.");

        var idOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account id")
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
        var account = await _mediator.Send(
            new AccountQuery
            {
                Uid = id
            });

        ShowAccount(account);
    }

    private void ShowAccount(Account? account)
    {
        if (account == null)
        {
            System.Console.WriteLine("Account not found");
        }
        else
        {
            System.Console.WriteLine($"Account uid: {account.Uid}");
            System.Console.WriteLine($"  - Email:   {account.Email}");
            System.Console.WriteLine($"  - Name:    {account.Name}");
            System.Console.WriteLine($"  - Link:    {account.Link}");
            System.Console.WriteLine($"  - Create:  {account.CreationTimestamp}");
            System.Console.WriteLine($"  - Sensors: {account.AccountSensors?.Count}");
            if (account.AccountSensors != null)
                foreach (var accountSensor in account.AccountSensors)
                {
                    System.Console.WriteLine($"    - Uid:     {accountSensor.Sensor.Uid}");
                    System.Console.WriteLine($"      - DevEui:  {accountSensor.Sensor.DevEui}");
                }
        }
    }

    private Command ReadByLinkSubCommand()
    {
        var subCommand = new Command("readbylink", "Read account by link.");

        var linkOption = new Option<string>(new[] { "-l", "--link" }, "Account link")
        {
            IsRequired = true
        };
        subCommand.AddOption(linkOption);

        subCommand.SetHandler(
            ReadByLink,
            linkOption);

        return subCommand;
    }

    private async Task ReadByLink(string link)
    {
        var account = await _mediator.Send(
            new AccountByLinkQuery
            {
                Link = link
            });

        ShowAccount(account);
    }

    private Command CreateSubCommand()
    {
        var subCommand = new Command("create", "Create account.");

        var idOption = new Option<Guid?>(new[] { "-i", "--ai", "--accountid" }, "Account identifier");
        subCommand.AddOption(idOption);

        var emailOption = new Option<string>(new[] { "-e", "--email" }, "Account email address")
        {
            IsRequired = true
        };
        subCommand.AddOption(emailOption);

        var nameOption = new Option<string?>(new[] { "-n", "--name" }, "Account name");
        subCommand.AddOption(nameOption);

        subCommand.SetHandler(
            Create,
            idOption, emailOption, nameOption);

        return subCommand;
    }

    private async Task Create(Guid? id, string email, string? name)
    {
        var uid = id ?? Guid.NewGuid();

        await _mediator.Send(
            new CreateAccountCommand
            {
                Uid = uid,
                Email = email,
                Name = name
            });

        System.Console.WriteLine("{0}", uid);
    }

    private Command UpdateSubCommand()
    {
        var subCommand = new Command("update", "Update account.");

        var idOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(idOption);

        var emailOption = new Option<string?>(new[] { "-e", "--email" }, "Account email address");
        subCommand.AddOption(emailOption);

        var nameOption = new Option<string?>(new[] { "-n", "--name" }, "Account name");
        subCommand.AddOption(nameOption);

        subCommand.SetHandler(
            Update,
            idOption, emailOption, nameOption);

        return subCommand;
    }

    private async Task Update(Guid uid, string? email, string? name)
    {
        await _mediator.Send(
            new UpdateAccountCommand
            {
                Uid = uid,
                Email = Optional.From(email),
                Name = Optional.From(name)
            });

        System.Console.WriteLine("{0}", uid);
    }

    private Command SetLinkSubCommand()
    {
        var subCommand = new Command("setlink", "Set link.");

        var idOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(idOption);

        var linkOption = new Option<string?>(new[] { "-l", "--link" }, "Account link");
        subCommand.AddOption(linkOption);

        subCommand.SetHandler(
            SetLink,
            idOption, linkOption);

        return subCommand;
    }

    private async Task SetLink(Guid uid, string? link)
    {
        await _mediator.Send(
            new RegenerateAccountLinkCommand
            {
                AccountUid = uid,
                Link = link
            });

        System.Console.WriteLine("{0}", uid);
    }

    private Command ListSensorsSubCommand()
    {
        var subCommand = new Command("listsensors", "List sensors.");

        var idOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(idOption);

        subCommand.SetHandler(ListSensors, idOption);

        return subCommand;
    }

    private async Task ListSensors(Guid uid)
    {
        var accountSensors = await _mediator.Send(
            new AccountSensorsQuery
            {
                Uid = uid
            });

        foreach (var accountSensor in accountSensors)
        {
            System.Console.WriteLine($"{accountSensor.Sensor.Uid} {accountSensor.Sensor.Link} {accountSensor.Name} {accountSensor.CapacityL} "
                + $"{accountSensor.DistanceMmEmpty}  {accountSensor.DistanceMmFull} {accountSensor.AlertsEnabled}");
        }
    }

    private Command AddSensorSubCommand()
    {
        var subCommand = new Command("addsensor", "Add sensor to account.");

        var accountIdOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(accountIdOption);

        var sensorIdOption = new Option<Guid>(new[] { "-s", "--si", "--sensorid" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(sensorIdOption);

        subCommand.SetHandler(
            AddSensor,
            accountIdOption, sensorIdOption);

        return subCommand;
    }

    private async Task AddSensor(Guid accountId, Guid sensorId)
    {
        await _mediator.Send(
            new AddSensorToAccountCommand
            {
                AccountUid = accountId,
                SensorUid = sensorId
            });
    }

    private Command UpdateSensorSubCommand()
    {
        var subCommand = new Command("updatesensor", "Update sensor from account.");

        var accountIdOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(accountIdOption);

        var sensorIdOption = new Option<Guid>(new[] { "-s", "--si", "--sensorid" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(sensorIdOption);

        var nameOption = new Option<string?>(new[] { "-n", "--name" }, "Name");
        subCommand.AddOption(nameOption);
        var distanceEmptyMmOption = new Option<int?>(new[] { "-e", "--de", "--distanceempty" }, "Distance empty in mm");
        subCommand.AddOption(distanceEmptyMmOption);
        var distanceFullMmOption = new Option<int?>(new[] { "-f", "--df", "--distancefull" }, "Distance full in mm");
        subCommand.AddOption(distanceFullMmOption);
        var capacityLOption = new Option<int?>(new[] { "-c", "--capacity" }, "Capacity in liter");
        subCommand.AddOption(capacityLOption);
        var alertsEnabled = new Option<bool?>(new[] { "-a", "--alertsEnabled" }, "Alerts enabled");
        subCommand.AddOption(alertsEnabled);

        subCommand.SetHandler(
            UpdateSensor,
            accountIdOption, sensorIdOption, nameOption, distanceEmptyMmOption, distanceFullMmOption, capacityLOption, alertsEnabled);

        return subCommand;
    }

    private async Task UpdateSensor(Guid accountId, Guid sensorId, string? name, int? distanceEmptyMm,
        int? distanceFullMm, int? capacityL, bool? alertsEnabled)
    {
        await _mediator.Send(
            new UpdateAccountSensorCommand
            {
                AccountUid = accountId,
                SensorUid = sensorId,
                Name = Optional.From(name),
                DistanceMmEmpty = Optional.From(distanceEmptyMm),
                DistanceMmFull = Optional.From(distanceFullMm, -1),
                CapacityL = Optional.From(capacityL, -1),
                AlertsEnabled = Optional.From(alertsEnabled)
            });
    }

    private Command RemoveSensorSubCommand()
    {
        var subCommand = new Command("removesensor", "Remove sensor from account.");

        var accountIdOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(accountIdOption);

        var sensorIdOption = new Option<Guid>(new[] { "-s", "--si", "--sensorid" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(sensorIdOption);

        subCommand.SetHandler(
            RemoveSensor,
            accountIdOption, sensorIdOption);

        return subCommand;
    }

    private async Task RemoveSensor(Guid accountId, Guid sensorId)
    {
        await _mediator.Send(
            new RemoveSensorFromAccountCommand
            {
                AccountUid = accountId,
                SensorUid = sensorId
            });
    }

    private Command CheckSensorAlarmsSubCommand()
    {
        var subCommand = new Command("checksensoralarms", "Check sensor alarms.");

        var accountIdOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(accountIdOption);

        var sensorIdOption = new Option<Guid>(new[] { "-s", "--si", "--sensorid" }, "Sensor identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(sensorIdOption);

        subCommand.SetHandler(
            CheckSensorAlarms,
            accountIdOption, sensorIdOption);

        return subCommand;
    }

    private async Task CheckSensorAlarms(Guid accountId, Guid sensorId)
    {
        await _mediator.Send(
            new CheckAccountSensorAlarmsCommand
            {
                AccountUid = accountId,
                SensorUid = sensorId
            });
    }

    private Command CheckAllSensorAlarmsSubCommand()
    {
        var subCommand = new Command("checkallsensoralarms", "Check all sensor alarms.");

        subCommand.SetHandler(
            CheckAllSensorAlarms);

        return subCommand;
    }

    private async Task CheckAllSensorAlarms()
    {
        await _mediator.Send(
            new CheckAllAccountSensorAlarmsCommand
            {
            });
    }

}