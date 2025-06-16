using Microsoft.Toolkit.Uwp.Notifications;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationAppDemo
{
    class Program
    {
        private static bool _running = true;

        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _running = false;
                eventArgs.Cancel = true; // 防止进程立即终止
            };

            Console.WriteLine("Hello C Sharp");

            try
            {
                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("192.168.1.111", 1883)
                    .WithClientId("C#_NotificationAPP_" + Guid.NewGuid().ToString())
                    .WithCleanSession(true)
                    .Build();

                // 连接mqtt服务器
                var connectResult = await mqttClient.ConnectAsync(options, CancellationToken.None);

                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    Console.WriteLine("MQTT Connected.");

                    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic("GlobalNotification")
                        .Build());

                    Console.WriteLine("Subscribed to espiot");

                    // 设置消息回调
                    mqttClient.ApplicationMessageReceivedAsync += async e =>
                    {
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                        Console.WriteLine($"收到消息: {payload}");

                        dynamic data = JObject.Parse(payload);
                        Console.WriteLine(data.msgType);
                        if(data.msgType == "offline")
                        {
                            Console.WriteLine("device offline");
                            ShowNotification("设备离线", $"设备离线:\n {data.source}\n");
                        }
                        else if (data.msgType == "online")
                        {
                            Console.WriteLine("device online");
                            ShowNotification("设备上线", $"设备上线\n{data.source}");
                        }else
                        {
                            Console.WriteLine(data);
                        }
                    };
                    // 保持程序运行
                    while (_running)
                    {
                        await Task.Delay(1000); // 每秒检查一次
                    }

                    // 断开连接
                    await mqttClient.DisconnectAsync();
                    Console.WriteLine("已断开MQTT连接");
                }
                else
                {
                    Console.WriteLine($"MQTT连接失败: {connectResult.ResultCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常: {ex.Message}");
            }
        }


        static void ShowNotification(string title, string content)
        {
            try
            {
                // 确保图片路径存在
                var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "noti.png");

                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(content)
                    //.AddAppLogoOverride(new Uri(imgPath), ToastGenericAppLogoCrop.Circle)
                    .Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"通知发送失败: {ex.Message}");
            }
        }
    }
}