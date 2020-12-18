// <copyright file="Command.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using System;
    using System.Threading;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json;
    using System.Collections.Generic;
    using Microsoft.Azure.ConnectedCar.DataContracts;
    using Microsoft.Azure.ConnectedCar.VehicleManagement.Client;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using global::Azure.DigitalTwins.Core;
    using global::Azure.Identity;
    using global::Azure.DigitalTwins.Core.Serialization;

    using DeviceMessage = Microsoft.Azure.Devices.Client.Message;

    class Command
    {
        public Command(string pfxPath, string domainName)
        {
            this.path = pfxPath;
            this.domain = domainName;
        }

        private string path { get; set; }
        private string domain { get; set; }

        /// <summary>
        ///     This function is from the perspective of a user interacting with a webservice to send a remote command to the vehicle
        ///     Note that it is necessary to have run the Provisioning Training Kit first, in order to have a vehicle to work with.
        /// </summary>
        public async Task SendCommandToVehicleAsync(string commandName, string payload)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                using (X509Certificate2 cert = new X509Certificate2(this.path))
                {
                    if (!Program.GetInfo(in cert, out string deviceName, out string vehicleId))
                    {
                        return;
                    }

                    Console.Write("Enter a UserID: ");
                    string userId = Console.ReadLine();

                    Console.Write("Enter a notification ID: ");
                    string notificationId = Console.ReadLine();

                    //Console.Write("CommandApiKey_Key1: ");
                    
                    string commandApiKey = "TKUACfEKZYFRnMGBrDQORQCYAIGYMKDnARYXpE3DnfTDExVrBCPDYBKkPzFKIWROTP8KIBWW0RzKKjYQslPRdSRDTfROVRBcYCLQQTIHSDADMA5W8ENNR8KGEBMoPEXWsTbOGVbSYDWWnBQCFILMLiUBV8NPCCAJRLEJWRPKnRNAKOOSQgyNWHjAYTIGOMSDPsQJezJZfqZYWcWZJHkQKaRRYH7kyPGTlXHOE3CUGgGPwOAWDAfZJczITAKOJ2AN";

                    string commandEndpoint = $"https://{this.domain}:8282/api/command/users/{userId}/vehicles/{vehicleId}/commands/{commandName}/notifications/{notificationId}?api-version=2019-07-01";

                    HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, commandEndpoint)
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    };
                    message.Headers.Add(RequestHeaders.GsCCApiKey, commandApiKey);

                    HttpResponseMessage responseMessage = await httpClient.SendAsync(message);

                    Console.WriteLine("The status code result was: " + responseMessage.StatusCode);
                }
            }
        }

        /// <summary>
        ///     This function is from the perspective of the vehicle where the command is pulled from IoT hub.
        ///     Next the vehicle turns around and send telemetry back up to the cloud in response
        ///     Note that it is necessary to have run the Provisioning Training Kit first, in order to have a vehicle to work with.
        /// </summary>
        public async Task ReceiveRespondCmdAsync()
        {
            using (X509Certificate2 cert = new X509Certificate2(this.path))
            {
                if (!Program.GetInfo(in cert, out string deviceName, out string vehicleUuid))
                {
                    return;
                }

                // Retrieves connection information from CVS for the device.  Connection information tells the device how to connect to IoTHub, including
                // authN and which endpoint to communicate with.
                using (IVehicleConnectionClient connectionClient = new VehicleConnectionClient(new Uri($"https://{this.domain}:8510"), cert))
                {
                    DeviceCredentialInformation deviceCredInfo = new DeviceCredentialInformation()
                    {
                        VehicleUuid = vehicleUuid
                    };
                    DeviceConnectionInformation connectionInfo = await connectionClient.GetDeviceConnectionInfoAsync(deviceCredInfo, deviceName);

                    DeviceClient primaryDeviceClient = DeviceClient.Create(
                        connectionInfo.PrimaryIoTHubName,
                        new DeviceAuthenticationWithRegistrySymmetricKey(connectionInfo.IoTDeviceId, connectionInfo.IoTAuthenticationInformation.SymmetricKey.PrimaryKey),
                    TransportType.Mqtt_Tcp_Only);

                    DeviceClient secondaryDeviceClient = DeviceClient.Create(
                        connectionInfo.SecondaryIoTHubName,
                        new DeviceAuthenticationWithRegistrySymmetricKey(connectionInfo.IoTDeviceId, connectionInfo.IoTAuthenticationInformation.SymmetricKey.PrimaryKey),
                    TransportType.Mqtt_Tcp_Only);

                    // If the message was timed out prior to running this function, this receive will fail.
                    Console.WriteLine("Start retrieving device command message.");
                    bool received = false;
                    DateTimeOffset bailoutTime = DateTimeOffset.UtcNow.AddSeconds(60);

                    while (!received && DateTimeOffset.UtcNow < bailoutTime)
                    {
                        List<Task<Tuple<DeviceClient, DeviceMessage>>> tasks = new List<Task<Tuple<DeviceClient, DeviceMessage>>>();
                        tasks.Add(ReceiveDeviceMessage(primaryDeviceClient));
                        tasks.Add(ReceiveDeviceMessage(secondaryDeviceClient));

                        while (tasks.Count > 0)
                        {
                            Task<Tuple<DeviceClient, DeviceMessage>> finishedTask = await Task.WhenAny(tasks);
                            tasks.Remove(finishedTask);

                            Tuple<DeviceClient, DeviceMessage> msg = await finishedTask;
                            if (msg.Item2 != null)
                            {
                                received = true;

                                string hubName = msg.Item1 == primaryDeviceClient ? "primary" : "secondary";
                                Console.WriteLine($"Retrieved device command message from {hubName} IoTHub.");
                                if (msg.Item1 == primaryDeviceClient)
                                {
                                    await HandleDeviceMessage(msg.Item2, primaryDeviceClient, secondaryDeviceClient,
                                        "primary", "secondary");
                                }
                                else
                                {
                                    await HandleDeviceMessage(msg.Item2, secondaryDeviceClient, primaryDeviceClient,
                                        "secondary", "primary");
                                }
                            }
                            else if (!received)
                            {
                                string hubName = msg.Item1 == primaryDeviceClient ? "primary" : "secondary";
                                Console.WriteLine($"Unable to retrieve device command message from {hubName} IoTHubs.");
                                Thread.Sleep(2000);
                            }
                        }
                    }

                    Console.WriteLine("------------------------");
                }
            }
        }

        private static async Task<Tuple<DeviceClient, DeviceMessage>> ReceiveDeviceMessage(DeviceClient client)
        {
            DeviceMessage deviceCommandMessage = await client.ReceiveAsync(TimeSpan.FromSeconds(2));
            return new Tuple<DeviceClient, DeviceMessage>(client, deviceCommandMessage);
        }

        private static async Task HandleDeviceMessage(DeviceMessage deviceCommandMessage,
            DeviceClient primaryClient, DeviceClient secondaryClient,
            string firstHubName, string secondHubName)
        {
            // this section demonstrates how to parse a non-binary DeviceCommandMessage that was sent down.
            // Later more advanced kits will discuss using binary payloads as another possibility.
            string rawContent;
            using (StreamReader reader = new StreamReader(deviceCommandMessage.BodyStream))
            {
                rawContent = await reader.ReadToEndAsync();
            }

            //DeviceCommandMessage parsedMessage = JsonConvert.DeserializeObject<DeviceCommandMessage>(rawContent);
            DeviceCommandMessage parsedMessage = System.Text.Json.JsonSerializer.Deserialize<DeviceCommandMessage>(rawContent);

            // Create a secret client using the DefaultAzureCredential
            DefaultAzureCredential cred = new DefaultAzureCredential();
            DigitalTwinsClient dtcli = new DigitalTwinsClient(new Uri("https://mobility-vss.api.wus2.digitaltwins.azure.net"), cred);

            BasicDigitalTwin twinData = new BasicDigitalTwin();

            JsonElement ele = (JsonElement)parsedMessage.Payload;

            twinData.Id = ele.GetProperty("$dtId").ToString();
            twinData.Metadata.ModelId = ele.GetProperty("$metadata").GetProperty("$model").ToString();

            await dtcli.CreateDigitalTwinAsync(twinData.Id, System.Text.Json.JsonSerializer.Serialize(twinData));

            await primaryClient.CompleteAsync(deviceCommandMessage);

            Console.WriteLine("The received command name is: " + parsedMessage.CommandName);

            Console.Write("Enter a response string:\n>");
            string responseString = Console.ReadLine();

            // construct a basic Device Telemetry Message with a minimal set of fields.  This will be used to respond to the command.
            DeviceTelemetryMessage telemetryMessage = new DeviceTelemetryMessage
            {
                TelemetryName = parsedMessage.CommandName,
                Payload = new { rawContent },
                Priority = MessagePriority.Normal,
                CorrelationId = parsedMessage.CorrelationId // setting the correlation ID allows the platform to recognize this is a response to the previous device command message.
            };

            // construct the actual IoT message.  Setting the content type is necessary for prioritization to function properly within CVS.
            using (DeviceMessage deviceMessage = new DeviceMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetryMessage))))
            {
                deviceMessage.ContentType = "application/json";

                // enqueues the event within IoTHub.  The completion of this event means that it has been enqueued to the cloud, but
                // does not imply that the cloud has actually processed the telemetry message yet.
                try
                {
                    await primaryClient.SendEventAsync(deviceMessage);
                    Console.WriteLine($"Sent telemetry message to {firstHubName} IoTHub in response!");
                }
                catch (Exception)
                {
                    await secondaryClient.SendEventAsync(deviceMessage);
                    Console.WriteLine($"Sent telemetry message to {secondHubName} IoTHub in response!");
                }
            }
        }
    }
}
