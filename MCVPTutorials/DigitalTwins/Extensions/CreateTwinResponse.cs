// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------
namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using System.Threading.Tasks;
    using Microsoft.Azure.ConnectedCar.ExtensionDevelopmentKit.Shared;
    using Microsoft.Azure.ConnectedCar.Instrumentation;
    using Microsoft.Azure.ConnectedCar.Sdk;
    using Microsoft.Azure.ConnectedCar.Sdk.ExtensionBaseTypes;
    using Newtonsoft.Json;

    public class SampleCommandStatusHandler : CommandStatusHandler
    {
        public override string ExtensionName => "SampleCommandStatusHandler";

        public override async Task HandleStatusAsync(CommandStatusResult statusResult, RequestDetails requestDetails, RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            if (client.CanSendUserNotification)
            {
                var notificationPayload = new
                {
                    Command = statusResult.CommandName,
                    VehicleId = statusResult.VehicleId,
                    CorrelationId = statusResult.Payload.CorrelationId,
                    Message = statusResult.Payload.Payload,
                    Time = statusResult.Payload.Time
                };
                await client.SendUserNotificationAsync(notificationPayload);
            }
        }
    }
}