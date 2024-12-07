using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Text;
using DocumentFormat.OpenXml.Spreadsheet;

namespace wsn_keboo.Data;

public class MqttClientHandler
{
    private IMqttClient _mqttClient;
    private MqttFactory _mqttFactory;
    private MqttClientOptions _mqttClientOptions;
    public event Action<string>? DataReceived;
    public Action<string, string>? Connected;
    public Action<string>? Error;   

    public MqttClientHandler()
    {
        _mqttFactory = new MqttFactory();
        _mqttClient = _mqttFactory.CreateMqttClient();
        _mqttClientOptions = new MqttClientOptionsBuilder()
                                .WithTcpServer("localhost").Build();
        _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
    }

    public async Task SubscribeTopic(string topic)
    {
        var _mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(
                                f => { f.WithTopic(topic); }).Build();
        await _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
        await _mqttClient.SubscribeAsync(_mqttSubscribeOptions, CancellationToken.None);
        
    }

    public async Task SendMessageAsync(string message, string topic)
    {
        if (_mqttClient.IsConnected)
        {
            var _mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(
                                f => { f.WithTopic(topic); }).Build();
            var message1 = new MqttApplicationMessageBuilder()
                .WithTopic(_mqttSubscribeOptions.TopicFilters[0].Topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .Build();

            await _mqttClient.PublishAsync(message1, CancellationToken.None);
            Connected?.Invoke("Connected", "" + DateTime.Now.ToString(" [h:mm:ss] ") + ": Opened port, Sent Command");
        }
        else
        {
            Connected?.Invoke("Not Connected", "" + DateTime.Now.ToString(" [h:mm:ss] ") + ": Opened port, Sent Command");
        }
    }

    private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs args)
    {
        if (args.ApplicationMessage.PayloadSegment.Array != null)
        {
            var message = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment.Array, 
                                                    args.ApplicationMessage.PayloadSegment.Offset, 
                                                    args.ApplicationMessage.PayloadSegment.Count);
            DataReceived?.Invoke(message);
        }
        else
        {
            Error?.Invoke("Payload may be null");
        }
        return Task.CompletedTask;
    }

}
