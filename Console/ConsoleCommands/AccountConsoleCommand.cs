using System.CommandLine;
using Core.Commands;
using Core.Entities;
using Core.Queries;
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
        command.AddCommand(GetListSubCommand());
        command.AddCommand(GetReadSubCommand());
        command.AddCommand(GetReadByLinkSubCommand());
        command.AddCommand(GetCreateSubCommand());
        command.AddCommand(GetUpdateSubCommand());
        command.AddCommand(GetSetLinkSubCommand());
        command.AddCommand(GetAddSensorSubCommand());
        return command;
    }

    private Command GetListSubCommand()
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

    private Command GetReadSubCommand()
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
            new ReadAccountQuery
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
            System.Console.WriteLine($"  - Sensors: {account.AccountSensors.Count}");
            foreach (var accountSensor in account.AccountSensors)
            {
                System.Console.WriteLine($"    - Uid:     {accountSensor.Sensor.Uid}");
                System.Console.WriteLine($"      - DevEui:  {accountSensor.Sensor.DevEui}");
            }
        }
    }

    private Command GetReadByLinkSubCommand()
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
            new ReadAccountByLinkQuery
            {
                Link = link
            });

        ShowAccount(account);
    }

    private Command GetCreateSubCommand()
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

    private Command GetUpdateSubCommand()
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
                Email = string.IsNullOrEmpty(email) ? null : new Tuple<bool, string>(true, email),
                Name = name == null ? null : new Tuple<bool, string?>(true, name)
            });

        System.Console.WriteLine("{0}", uid);
    }

    private Command GetSetLinkSubCommand()
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
            new RegenerateLinkAccountCommand
            {
                AccountUid = uid,
                Link = link
            });

        System.Console.WriteLine("{0}", uid);
    }

    private Command GetAddSensorSubCommand()
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
}