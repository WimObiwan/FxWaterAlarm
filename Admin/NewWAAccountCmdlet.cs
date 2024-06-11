using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.New,"WAAccount")]
[OutputType(typeof(Guid))]
public class NewWAAccountCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true)]
    public string Email { get; set; } = null!;

    [Parameter(
        Position = 1)]
    public Guid? AccountUid { get; set; }

    [Parameter(
        Position = 2)]
    public string? Name { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        Guid accountUid = AccountUid ?? Guid.NewGuid();

        await _mediator.Send(new CreateAccountCommand() { 
            Uid = accountUid,
            Email = Email,
            Name = Name
        });

        WriteObject(accountUid);
    }
}
