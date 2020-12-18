// <copyright file="Telemetry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Azure.ConnectedCar.DataContracts;
    using Microsoft.Azure.ConnectedCar.VehicleManagement.Client;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using DeviceMessage = Microsoft.Azure.Devices.Client.Message;

    class Telemetry
    {
        public Telemetry(string pfxPath, string domainName, string telemetryName)
        {
            this.path = pfxPath;
            this.domain = domainName;
            this.telemName = telemetryName;
        }

        private string path { get; set; }
        private string domain { get; set; }
        private string telemName { get; set; }

        public async Task Send(string jsonPayload)
        {
            using (X509Certificate2 cert = new X509Certificate2(this.path))
            {
                if (!Program.GetInfo(in cert, out string deviceName, out string vehicleUuid))
                {
                    return;
                }

                // Retrieves vehicle with Authentication Info from CVS. This step is required to parse out the service authentication key needed
                // to retrieve the vehicle connection information
                using (VehicleConnectionClient vc = new VehicleConnectionClient(new Uri(String.Format("https://{0}:8510", this.domain)), cert))
                {
                    DeviceCredentialInformation deviceInfo = new DeviceCredentialInformation
                    {
                        VehicleUuid = vehicleUuid
                    };

                    DeviceConnectionInformation connectionInfo = vc.GetDeviceConnectionInfoAsync(deviceInfo, deviceName).Result;

                    if (connectionInfo == null)
                    {
                        Console.WriteLine("Vehicle ID or device name not found. Please try again");
                        return;
                    }

                    DeviceClient deviceClient = DeviceClient.Create(
                        connectionInfo.PrimaryIoTHubName,
                        new DeviceAuthenticationWithRegistrySymmetricKey(
                            connectionInfo.IoTDeviceId,
                            connectionInfo.IoTAuthenticationInformation.SymmetricKey.PrimaryKey),
                        TransportType.Mqtt_Tcp_Only);

                    Console.WriteLine("------------------------");

                    // construct a basic Device Telemetry Message
                    DeviceTelemetryMessage telemetryMessage = new DeviceTelemetryMessage
                    {
                        TelemetryName = this.telemName,

                        // needs to match the prefix of our telemetry extension
                        Payload = jsonPayload,
                        Priority = MessagePriority.Normal
                    };

                    // construct the actual IoT message.  Setting the content type is necessary for prioritization to function properly within CVS.
                    DeviceMessage deviceMessage =
                        new DeviceMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetryMessage)));
                    deviceMessage.ContentType = "application/json";

                    await deviceClient.SendEventAsync(deviceMessage);
                    Console.WriteLine($"Sent telemetry message to IoT hub!");

                    Console.WriteLine("------------------------");
                }
            }
        }
    }
}
