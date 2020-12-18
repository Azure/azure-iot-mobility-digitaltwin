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

    [TelemetryNamePrefix(@"createrelationship-")]
    public class CreateRelationshipHandler : TelemetryHandler
    {
        //The extension name to identify this manifest
        public override string ExtensionName => nameof(CreateRelationshipHandler);

        public override async Task HandleMessageAsync(DeviceTelemetryMessage requestBody, RequestDetails requestDetails, RequestMessageHeaders headers, IExtensionGatewayClient client, ILogger log)
        {
            //write telemetry to statestore
            await client.PutDeviceTelemetryAsync(requestDetails.DeviceName, requestBody);

            // Create a secret client using the DefaultAzureCredential

            DefaultAzureCredential cred = new DefaultAzureCredential();
            DigitalTwinsClient dtcli = new DigitalTwinsClient(new Uri("https://mobility-vss.api.wus2.digitaltwins.azure.net"), cred);

            BasicRelationship rel = JsonSerializer.Deserialize<BasicRelationship>((string)requestBody.Payload);

            string relationshipId = Guid.NewGuid().ToString();

            await dtcli.CreateRelationshipAsync(rel.SourceId, relationshipId, (string)requestBody.Payload);
        }
    }
}
