// <copyright file="SetProperty.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text.Json;
    using global::Azure;
    using global::Azure.DigitalTwins.Core;
    using global::Azure.Identity;
    using global::Azure.DigitalTwins.Core.Serialization;
    using Microsoft.Azure.ConnectedCar.DataContracts;
    using Microsoft.Azure.ConnectedCar.ExtensionDevelopmentKit.Shared;
    using Microsoft.Azure.ConnectedCar.Instrumentation;
    using Microsoft.Azure.ConnectedCar.Sdk;
    using Microsoft.Azure.ConnectedCar.Sdk.ExtensionBaseTypes;


    //this indicates the prefix of telemetry name that will be saved
    [TelemetryNamePrefix(@"setpropertytelemetry-")]
    public class SetPropertyTelemetryHandler : TelemetryHandler
    {
        //The extension name to identify this manifest
        public override string ExtensionName => nameof(SetPropertyTelemetryHandler);

        public override async Task HandleMessageAsync(DeviceTelemetryMessage requestBody, RequestDetails requestDetails, RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            //write telemetry to statestore
            await client.PutDeviceTelemetryAsync(requestDetails.DeviceName, requestBody);

            // Create a secret client using the DefaultAzureCredential

            DefaultAzureCredential cred = new DefaultAzureCredential();
            DigitalTwinsClient dtcli = new DigitalTwinsClient(new Uri("https://mobility-vss.api.wus2.digitaltwins.azure.net"), cred);

            DTUnit du = JsonSerializer.Deserialize<DTUnit>((string)requestBody.Payload);

            var updateOps = new UpdateOperationsUtility();

            if (du.type == "boolean")
            {
                updateOps.AppendAddOp(du.path, bool.Parse(du.value));
            }
            else if ((du.type == "date") || (du.type == "datetime") || (du.type == "time"))
            {
                updateOps.AppendAddOp(du.path, DateTime.Parse(du.value));
            }
            else if (du.type == "double")
            {
                updateOps.AppendAddOp(du.path, double.Parse(du.value));
            }
            else if (du.type == "float")
            {
                updateOps.AppendAddOp(du.path, float.Parse(du.value));
            }
            else if (du.type == "integer")
            {
                updateOps.AppendAddOp(du.path, int.Parse(du.value));
            }
            else if (du.type == "long")
            {
                updateOps.AppendAddOp(du.path, long.Parse(du.value));
            }
            else
            {
                updateOps.AppendAddOp(du.path, du.value);
            }

            string patchPayload = updateOps.Serialize();
            await dtcli.UpdateDigitalTwinAsync(du.dtID, patchPayload);

            // send stuff to the analytics pipeline
            var telemetryItem = new
            {
                VehicleId = requestDetails.VehicleId,
                TelemetryName = requestBody.TelemetryName,
                Time = requestBody.Time,
                Payload = patchPayload
            };

            await client.SendToAnalyticsPipeline(telemetryItem);
        }
    }

    public class DTUnit
    {
        public string dtID { get; set; }
        public string path { get; set; }
        public string value { get; set; }
        public string type { get; set; }
    }
}
