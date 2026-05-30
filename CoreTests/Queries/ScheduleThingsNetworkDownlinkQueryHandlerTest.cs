using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Core.Configuration;
using Core.Queries;
using Microsoft.Extensions.Options;

namespace CoreTests.Queries;

public class ScheduleThingsNetworkDownlinkQueryHandlerTest
{
    [Fact]
    public async Task Handle_ResolvesDeviceAndSchedulesDownlink()
    {
        var options = Options.Create(new ThingsNetworkOptions
        {
            ApiBaseUrl = "https://eu1.cloud.thethings.network",
            WebhookId = "wateralarm-admin",
            DeviceLookupPageSize = 100,
            Applications =
            [
                new ThingsNetworkApplicationOptions
                {
                    ApplicationId = "app-1",
                    ApiKey = "test-api-key"
                }
            ]
        });

        string? postBody = null;
        var messageHandler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v3/applications/app-1/devices")
            {
                const string body = """
                    {
                      "end_devices": [
                        {
                          "ids": {
                            "device_id": "fx-waterlevel3-6",
                            "dev_eui": "A801234567890123"
                          }
                        }
                      ]
                    }
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }

            if (request.Method == HttpMethod.Post &&
                request.RequestUri!.AbsolutePath == "/api/v3/as/applications/app-1/webhooks/wateralarm-admin/devices/fx-waterlevel3-6/down/push")
            {
                postBody = request.Content!.ReadAsStringAsync().Result;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("unexpected request")
            });
        });

        using var httpClient = new HttpClient(messageHandler);
        var handler = new ScheduleThingsNetworkDownlinkQueryHandler(options, httpClient);

        var result = await handler.Handle(new ScheduleThingsNetworkDownlinkQuery
        {
            DevEui = "a8-01-23-45-67-89-01-23",
            Payload = [0x01, 0x00, 0x01, 0x2C],
            FPort = 15,
            Priority = "NORMAL",
            Confirmed = false
        }, CancellationToken.None);

        Assert.Equal("A801234567890123", result.DevEui);
        Assert.Equal("app-1", result.ApplicationId);
        Assert.Equal("fx-waterlevel3-6", result.DeviceId);
        Assert.Equal("AQABLA==", result.FrmPayloadBase64);

        Assert.NotNull(postBody);
        using var json = JsonDocument.Parse(postBody!);
        var downlinks = json.RootElement.GetProperty("downlinks");
        Assert.Equal("AQABLA==", downlinks[0].GetProperty("frm_payload").GetString());
        Assert.Equal(15, downlinks[0].GetProperty("f_port").GetInt32());
        Assert.Equal("NORMAL", downlinks[0].GetProperty("priority").GetString());
        Assert.False(downlinks[0].GetProperty("confirmed").GetBoolean());
    }

    [Fact]
    public async Task Handle_SearchesConfiguredApplications_UsesMatchingAppKeyAndWebhook()
    {
        var options = Options.Create(new ThingsNetworkOptions
        {
            ApiBaseUrl = "https://eu1.cloud.thethings.network",
            WebhookId = "global-webhook",
            DeviceLookupPageSize = 100,
            Applications =
            [
                new ThingsNetworkApplicationOptions
                {
                    ApplicationId = "app-1",
                    ApiKey = "key-app-1"
                },
                new ThingsNetworkApplicationOptions
                {
                    ApplicationId = "app-2",
                    ApiKey = "key-app-2",
                    WebhookId = "app-2-webhook"
                }
            ]
        });

        var getRequests = new List<(string Path, string? Token)>();
        string? postPath = null;
        string? postToken = null;

        var messageHandler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v3/applications/app-1/devices")
            {
                getRequests.Add((request.RequestUri.AbsolutePath, request.Headers.Authorization?.Parameter));
                const string body = """
                    {
                      "end_devices": [
                        {
                          "ids": {
                            "device_id": "different-device",
                            "dev_eui": "FFFFFFFFFFFFFFFF"
                          }
                        }
                      ]
                    }
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v3/applications/app-2/devices")
            {
                getRequests.Add((request.RequestUri.AbsolutePath, request.Headers.Authorization?.Parameter));
                const string body = """
                    {
                      "end_devices": [
                        {
                          "ids": {
                            "device_id": "fx-waterlevel3-6",
                            "dev_eui": "A801234567890123"
                          }
                        }
                      ]
                    }
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }

            if (request.Method == HttpMethod.Post &&
                request.RequestUri!.AbsolutePath == "/api/v3/as/applications/app-2/webhooks/app-2-webhook/devices/fx-waterlevel3-6/down/push")
            {
                postPath = request.RequestUri.AbsolutePath;
                postToken = request.Headers.Authorization?.Parameter;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("unexpected request")
            });
        });

        using var httpClient = new HttpClient(messageHandler);
        var handler = new ScheduleThingsNetworkDownlinkQueryHandler(options, httpClient);

        var result = await handler.Handle(new ScheduleThingsNetworkDownlinkQuery
        {
            DevEui = "A801234567890123",
            Payload = [0x01, 0x00, 0x01, 0x2C]
        }, CancellationToken.None);

        Assert.Equal("app-2", result.ApplicationId);
        Assert.Equal("fx-waterlevel3-6", result.DeviceId);
        Assert.Equal(2, getRequests.Count);
        Assert.Equal("key-app-1", getRequests[0].Token);
        Assert.Equal("key-app-2", getRequests[1].Token);
        Assert.Equal("key-app-2", postToken);
        Assert.Equal("/api/v3/as/applications/app-2/webhooks/app-2-webhook/devices/fx-waterlevel3-6/down/push", postPath);
    }

    [Fact]
    public async Task Handle_DeviceNotFound_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new ThingsNetworkOptions
        {
            ApiBaseUrl = "https://eu1.cloud.thethings.network",
            WebhookId = "wateralarm-admin",
            DeviceLookupPageSize = 100,
            Applications =
            [
                new ThingsNetworkApplicationOptions
                {
                    ApplicationId = "app-1",
                    ApiKey = "test-api-key"
                }
            ]
        });

        var messageHandler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v3/applications/app-1/devices")
            {
                const string body = """
                    {
                      "end_devices": [
                        {
                          "ids": {
                            "device_id": "different-device",
                            "dev_eui": "FFFFFFFFFFFFFFFF"
                          }
                        }
                      ]
                    }
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("unexpected request")
            });
        });

        using var httpClient = new HttpClient(messageHandler);
        var handler = new ScheduleThingsNetworkDownlinkQueryHandler(options, httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ScheduleThingsNetworkDownlinkQuery
            {
                DevEui = "A801234567890123",
                Payload = [0x01, 0x00, 0x01, 0x2C]
            }, CancellationToken.None));

        Assert.Contains("No TTN end device was found", exception.Message);
    }

    private sealed class DelegateHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public DelegateHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "missing");

            return _handler(request, cancellationToken);
        }
    }
}
