using System.CommandLine;
using Core.Commands;
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
        command.AddCommand(GetCreateSubCommand());
        command.AddCommand(GetUpdateSubCommand());
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
            _logger.LogInformation("{Id} {Uid} {Name} {Email}",
                result.Id, result.Uid, result.Name, result.Email);
            System.Console.WriteLine("{0} {1} {2}",
                result.Uid, result.Name, result.Email);
        }
    }

    private Command GetCreateSubCommand()
    {
        var subCommand = new Command("create", "Create account.");

        var createIdOption = new Option<Guid?>(new[] { "-i", "--ai", "--accountid" }, "Account identifier");
        subCommand.AddOption(createIdOption);

        var createEmailOption = new Option<string>(new[] { "-e", "--email" }, "Account email address")
        {
            IsRequired = true
        };
        subCommand.AddOption(createEmailOption);

        var createNameOption = new Option<string?>(new[] { "-n", "--name" }, "Account name");
        subCommand.AddOption(createNameOption);

        subCommand.SetHandler(
            Create,
            createIdOption, createEmailOption, createNameOption);

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

        var createIdOption = new Option<Guid>(new[] { "-i", "--ai", "--accountid" }, "Account identifier")
        {
            IsRequired = true
        };
        subCommand.AddOption(createIdOption);

        var createEmailOption = new Option<string?>(new[] { "-e", "--email" }, "Account email address");
        subCommand.AddOption(createEmailOption);

        var createNameOption = new Option<string?>(new[] { "-n", "--name" }, "Account name");
        subCommand.AddOption(createNameOption);

        subCommand.SetHandler(
            Update,
            createIdOption, createEmailOption, createNameOption);

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