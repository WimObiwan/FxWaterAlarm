using System.CommandLine;
using Core.Commands;
using Core.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Console.Commands;

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
        var listSubCommand = new Command("list", "List accounts.");
        listSubCommand.SetHandler(List);

        var createSubCommand = new Command("create", "Create account.");
        var createIdOption = new Option<Guid?>(new[] { "-i", "--id" }, "Account identifier");
        createSubCommand.AddOption(createIdOption);
        var createEmailOption = new Option<string>(new[] { "-e", "--email" }, "Account email address")
        {
            IsRequired = true
        };
        createSubCommand.AddOption(createEmailOption);
        var createNameOption = new Option<string?>(new[] { "-n", "--name" }, "Account name");
        createSubCommand.AddOption(createNameOption);
        // createSubCommand.SetHandler(
        //     async (createIdOptionValue, createEmailOptionValue, createNameOptionValue) =>
        //         await Create(createIdOptionValue, createEmailOptionValue, createNameOptionValue),
        //     createIdOption, createEmailOption, createNameOption);
        createSubCommand.SetHandler(
            Create,
            createIdOption, createEmailOption, createNameOption);

        var command = new Command("account", "Account actions.");
        command.AddCommand(listSubCommand);
        command.AddCommand(createSubCommand);
        return command;
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
    }
}