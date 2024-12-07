using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;

//TODO: TLS support?

namespace wsn_keboo.Data;

public class MqttBroker : IDisposable
{
    private MqttServer _mqttServer;
    private MqttFactory _mqttFactory;
    private MqttServerOptions _mqttServerOptions;

    public MqttBroker()
    {
        _mqttFactory = new MqttFactory();
        _mqttServerOptions = new MqttServerOptionsBuilder()
        .WithDefaultEndpoint().Build();
        _mqttServer = _mqttFactory.CreateMqttServer(_mqttServerOptions);
    }

    public async Task Force_Disconnecting_Client()
    {
        using (_mqttServer = await StartMqttServer())
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            var affectedClient = (await _mqttServer.GetClientsAsync()).FirstOrDefault(c => c.Id == "MyClient");
            if (affectedClient != null)
            {
                await affectedClient.DisconnectAsync();
            }
        }
    }

    public async Task Publish_Message_From_Broker()
    {
        /*
         * This sample will publish a message directly at the broker.
         *
         * See _Run_Minimal_Server_ for more information.
         */

        using (_mqttServer = await StartMqttServer())
        {
            // Create a new message using the builder as usual.
            var message = new MqttApplicationMessageBuilder().WithTopic("mqttnet/samples/topic/1").WithPayload("Test").Build();

            // Now inject the new message at the broker.
            await _mqttServer.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(message)
                {
                    SenderClientId = "SenderClientId"
                });
        }
    }
    
    public async Task Run_Minimal_Server()
    {
        /*
         * Starts a simple MQTT server which will accept any TCP connection.
         */
        // The port for the default endpoint is 1883.
        // The default endpoint is NOT encrypted!
        // Use the builder classes where possible.

        // The port can be changed using the following API (not used in this example).
        // new MqttServerOptionsBuilder()
        //     .WithDefaultEndpoint()
        //     .WithDefaultEndpointPort(1234)
        //     .Build();
        await _mqttServer.StartAsync();
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
        await _mqttServer.StopAsync();
    }

    public async Task Validating_Connections()
    {
        _mqttServer.ValidatingConnectionAsync += e =>
        {
            if (e.ClientId != "ValidClientId")
            {
                e.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            }

            if (e.UserName != "ValidUser")
            {
                e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            }

            if (e.Password != "SecretPassword")
            {
                e.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            }

            return Task.CompletedTask;
        };
        await _mqttServer.StartAsync();
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
        await _mqttServer.StopAsync();
    }

    public async Task<MqttServer> StartMqttServer()
    {
        // Due to security reasons the "default" endpoint (which is unencrypted) is not enabled by default!
        await _mqttServer.StartAsync();
        return _mqttServer;
    }

    public void Dispose()
    {
        StopBrokerAsync().GetAwaiter().GetResult();
    }
    public async Task StopBrokerAsync()
    {
        if (_mqttServer != null)
        {
            await _mqttServer.StopAsync();
        }
    }
    //enable to run in admin mode, log in console for easier debugging
    class ConsoleLogger : IMqttNetLogger
    {
        readonly object _consoleSyncRoot = new();

        public bool IsEnabled => true;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
        {
            var foregroundColor = ConsoleColor.White;
            switch (logLevel)
            {
                case MqttNetLogLevel.Verbose:
                    foregroundColor = ConsoleColor.White;
                    break;

                case MqttNetLogLevel.Info:
                    foregroundColor = ConsoleColor.Green;
                    break;

                case MqttNetLogLevel.Warning:
                    foregroundColor = ConsoleColor.DarkYellow;
                    break;

                case MqttNetLogLevel.Error:
                    foregroundColor = ConsoleColor.Red;
                    break;
            }

            if (parameters?.Length > 0)
            {
                message = string.Format(message, parameters);
            }

            lock (_consoleSyncRoot)
            {
                Console.ForegroundColor = foregroundColor;
                Console.WriteLine(message);

                if (exception != null)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }
}