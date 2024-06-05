using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Core.Queries;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Get,"WAAccount")]
[OutputType(typeof(Account))]
public class GetWAAccountCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new AccountsQuery());

        foreach (var result in results)
        {
            WriteObject(new Account {
                Uid = result.Uid,
                Email = result.Email,
                Name = result.Name,
                CreationTimestamp = result.CreationTimestamp,
                Link = result.Link
            });
        }
    }
}

public class Account
{
    public required Guid Uid { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }
    public required DateTime CreationTimestamp { get; init; }
    public string? Link { get; init; }
}
