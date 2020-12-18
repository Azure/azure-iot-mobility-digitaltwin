// <copyright file="SetProperty.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using System;
    using System.Threading.Tasks;
    using System.Text.Json;
    using Microsoft.Azure.ConnectedCar.DataContracts;
    using Microsoft.Azure.ConnectedCar.ExtensionDevelopmentKit.Shared;
    using Microsoft.Azure.ConnectedCar.Instrumentation;
    using Microsoft.Azure.ConnectedCar.Sdk;
    using Microsoft.Azure.ConnectedCar.Sdk.ExtensionBaseTypes;
    using Newtonsoft.Json.Linq;

    public class CreateTwinCommand : Command
    {
        public override string ExtensionName => "createtwin";

        public override async Task<WebCommandResponsePayload> ExecuteCommandAsync(JToken requestBody, RequestDetails requestDetails, RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            // Defines the message to be sent to the device.
            // Note that you MUST update the "InsertDeviceNameFromProvisioningStepHere" string to represent the device that was provisioned during the Provisioning getting started sample.
            DeviceCommandMessage message = new DeviceCommandMessage("contosodevice01", "CreateTwin", requestBody);

            // Status related to the command will be sent to the SampleCommandStatusHandler class.
            CommandStatusDetails statusDetails = new CommandStatusDetails("SampleCommandStatusHandler", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(230));
            await client.SendDeviceCommandAsync(message, statusDetails);

            // indicates back to the user that the device command was successfully enqueued (but may not have been processed yet by the vehicle!).
            return new WebCommandResponsePayload(WebCommandStatus.Success, WebCommandResponsePayload.EmptyPayload);
        }
    }
}