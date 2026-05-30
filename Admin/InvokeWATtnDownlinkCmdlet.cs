using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Queries;
using Core.Util;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsLifecycle.Invoke, "WATtnDownlink", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(TtnDownlink))]
public class InvokeWATtnDownlinkCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true)]
    public string[] DevEui { get; set; } = null!;

    [Parameter(
        Position = 1,
        Mandatory = true)]
    public string PayloadHex { get; set; } = null!;

    [Parameter]
    [ValidateRange(1, 255)]
    public int FPort { get; set; } = 15;

    [Parameter]
    [ValidateNotNullOrEmpty]
    public string Priority { get; set; } = "NORMAL";

    [Parameter]
    public SwitchParameter Confirmed { get; set; }

    [Parameter]
    public string? ApplicationId { get; set; }

    [Parameter]
    public string? WebhookId { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        var payload = ParseHexPayload(PayloadHex);

        foreach (var devEui in DevEui)
        {
            if (!ShouldProcess($"DevEUI: {devEui}", $"Schedule TTN downlink on f_port {FPort}"))
                continue;

            try
            {
                var result = await _mediator.Send(new ScheduleThingsNetworkDownlinkQuery
                {
                    DevEui = devEui,
                    Payload = payload,
                    FPort = FPort,
                    Priority = Priority,
                    Confirmed = Confirmed.IsPresent,
                    ApplicationId = ApplicationId,
                    WebhookId = WebhookId
                }, cancellationToken);

                WriteObject(new TtnDownlink
                {
                    DevEui = result.DevEui,
                    ApplicationId = result.ApplicationId,
                    DeviceId = result.DeviceId,
                    WebhookId = result.WebhookId,
                    FPort = result.FPort,
                    Confirmed = result.Confirmed,
                    Priority = result.Priority,
                    PayloadHex = PayloadHex,
                    FrmPayloadBase64 = result.FrmPayloadBase64
                });
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidOperation, devEui));
            }
        }
    }

    private static byte[] ParseHexPayload(string payloadHex)
    {
        try
        {
            return HexPayloadParser.Parse(payloadHex);
        }
        catch (ArgumentException ex)
        {
            throw new PSArgumentException(ex.Message, nameof(PayloadHex));
        }
    }
}

public sealed class TtnDownlink
{
    public required string DevEui { get; init; }
    public required string ApplicationId { get; init; }
    public required string DeviceId { get; init; }
    public required string WebhookId { get; init; }
    public required int FPort { get; init; }
    public required bool Confirmed { get; init; }
    public required string Priority { get; init; }
    public required string PayloadHex { get; init; }
    public required string FrmPayloadBase64 { get; init; }
}
