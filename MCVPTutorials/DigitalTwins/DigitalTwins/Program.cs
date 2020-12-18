// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;
    using DigitalTwinsDataContracts.v2;
    //using Newtonsoft.Json;
    using global::Azure;
    using global::Azure.DigitalTwins.Core;
    using global::Azure.Identity;
    using global::Azure.DigitalTwins.Core.Serialization;

    public class Program
    {
        /**************************************************************************
         * The domain name must be modified for this sample to function.  No other modifications are necessary.
         **************************************************************************/

        // replace DomainName value with your machine name.  With a test setup you would use the Cluster-Host-Name secret in your KeyVault.
        private const string domainName = "cloud-vm";

        /// <summary>
        /// This function acts as if it is a device within a vehicle, first connecting to the Vehicle Connection Service to get
        /// connection information, and then constructing a telemetry message and sending it to IoTEdge.
        /// </summary>
        public static async Task Main(string[] args)
        {
            // this is an example telemetry type to collect and send its value to the cloud.

            string pfxPath = @"C:\trustedzone\contosocar01\contosodevice01.pfx";
            Console.WriteLine("---Digital Twins Sample---");


            bool loop = true;

            while (loop)
            {
                int result;
                string c = ReadOptions();

                if (int.TryParse(c, out result))
                {
                    int i = int.Parse(c.ToString());
                    switch (i)
                    {
                        case 0:
                            loop = false;
                            break;
                        case 1:
                            await WritePropertyAsync(pfxPath);
                            break;
                        case 2:
                            await CommandCreateTwinAsync(pfxPath);
                            break;
                        case 3:
                            await ReceiveRespondCreateTwinAsync(pfxPath);
                            break;
                        case 4:
                            await CreateRelationshipAsync(pfxPath);
                            break;
                        default: break;
                    }
                }
            }
        }

        public static async Task WritePropertyAsync(string pfxPath)
        {
            Console.Write("Property name:");
            string propertyname = Console.ReadLine();
            string telemetryName = string.Format("setpropertytelemetry-{0}", propertyname);
            Console.WriteLine("Sample telemetry jason: {\"dtID\": \"vehicle-contosocar01\", \"path\": \"/Speed\", \"value\": \"20\", \"type\": \"integer\"}");
            Console.Write("Enter telemetry json: ");
            string jsonTelem = Console.ReadLine();
            Telemetry telem = new Telemetry(pfxPath, domainName, telemetryName);
            await telem.Send(jsonTelem);
        }

        public static async Task CommandCreateTwinAsync(string pfxPath)
        {
            DefaultAzureCredential cred = new DefaultAzureCredential();
            DigitalTwinsClient dtcli = new DigitalTwinsClient(new Uri("https://mobility-vss.api.wus2.digitaltwins.azure.net"), cred);

            BasicDigitalTwin twinData = new BasicDigitalTwin();

            //Note the modelId requires that the model is preloaded in the cloud
            Console.Write("modelId (dtmi): ");
            twinData.Metadata.ModelId = Console.ReadLine();

            Console.Write("twinId (e.g. lfFoglight-contosocar01): ");
            twinData.Id = Console.ReadLine();

            string payload = JsonSerializer.Serialize(twinData);

            Command cmd = new Command(pfxPath, domainName);
            await cmd.SendCommandToVehicleAsync("createtwin", payload);
        }

        public static async Task ReceiveRespondCreateTwinAsync(string pfxPath)
        {
            Command cmd = new Command(pfxPath, domainName);

            await cmd.ReceiveRespondCmdAsync();
        }

        public static async Task CreateRelationshipAsync(string pfxPath)
        {
            Console.Write("source twinId: ");
            string sourceTwinId = Console.ReadLine();

            Console.Write("target twinId: ");
            string targetTwinId = Console.ReadLine();

            string relationshipId = Guid.NewGuid().ToString();

            DefaultAzureCredential cred = new DefaultAzureCredential();
            DigitalTwinsClient dtcli = new DigitalTwinsClient(new Uri("https://mobility-vss.api.wus2.digitaltwins.azure.net"), cred);

            string relationshipName = GetRelationshipFromDigitalTwinIds(dtcli, sourceTwinId, targetTwinId);

            var relationship = new BasicRelationship
            {
                Id = relationshipId,
                SourceId = sourceTwinId,
                TargetId = targetTwinId,
                Name = relationshipName
            };

            Telemetry telem = new Telemetry(pfxPath, domainName, "createrelationship-generic");
            string payload = System.Text.Json.JsonSerializer.Serialize(relationship);

            await telem.Send(payload);
        }

        public static string PrettifyJson(string json)
        {
            object jsonObj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            return System.Text.Json.JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
        }

        public static bool GetInfo(in X509Certificate2 cert, out string deviceName, out string vehicleId)
        {
            deviceName = "";
            vehicleId = "";

            if (string.IsNullOrEmpty(domainName))
            {
                Console.WriteLine("Domain name is not set. Please try again");
                return false;
            }

            // Pulls the vehicle and device name info from the CN section of the cert as instructed in previous tutorial
            if (cert.Subject.Length > 6)
            {
                string[] subjectList = cert.Subject.Split(',');
                foreach (string entry in subjectList)
                {
                    if (entry.Trim().StartsWith("CN=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] cnList = entry.Split('=');

                        if (cnList.Length == 2)
                        {
                            string[] infoList = cnList[1].Split('.');

                            if (infoList.Length != 2)
                            {
                                Console.WriteLine("Please change the Common Name in your certificate to follow the {vehicleId}.{deviceName} format.");
                                return false;
                            }

                            vehicleId = infoList[0];
                            deviceName = infoList[1];

                            Console.WriteLine("deviceName = {0}", deviceName);
                            Console.WriteLine("vehicleId = {0}", vehicleId);

                            return true;
                        }
                    }
                }
            }
            Console.WriteLine("Error retrieving deviceName and vehicleId from pfx file");
            return false;
        }

        private static string GetRelationshipFromDigitalTwinIds(DigitalTwinsClient client, string sourceTwinId, string targetTwinId)
        {
            string relationshipId = "";
            string sourcedtmi = "";
            string targetdtmi = "";

            //obtain the dtmi for the target
            string q = string.Format("SELECT v.$metadata.$model FROM DIGITALTWINS v WHERE $dtId = '{0}' AND IS_PRIMITIVE(v.$metadata.$model)", targetTwinId);

            if (q.Length > 0)
            {
                Pageable<string> result = client.Query(q);

                foreach (string s in result)
                {
                    using (JsonDocument jdoc = JsonDocument.Parse(s))
                    {
                        JsonElement ele = jdoc.RootElement;
                        targetdtmi = ele.GetProperty("$model").ToString();
                    }
                }
            }

            //obtain the dtmi for the source
            q = string.Format("SELECT v.$metadata.$model FROM DIGITALTWINS v WHERE $dtId = '{0}' AND IS_PRIMITIVE(v.$metadata.$model)", sourceTwinId);

            if ((q.Length > 0) && (targetdtmi.Length > 0))
            {
                Pageable<string> result = client.Query(q);

                foreach (string s in result)
                {
                    using (JsonDocument jdoc = JsonDocument.Parse(s))
                    {
                        JsonElement ele = jdoc.RootElement;
                        sourcedtmi = ele.GetProperty("$model").ToString();
                    }
                }

                dtdlInterfaceModel targetDTDL = GetDTDLModel(client, sourcedtmi);
                if (targetDTDL != null)
                {
                    foreach (dtdlRelationshipModel r in targetDTDL.relationships)
                    {
                        if (r.target == targetdtmi)
                        {
                            relationshipId = r.name;
                            Console.WriteLine("Relationship created: {0}", System.Text.Json.JsonSerializer.Serialize<dtdlRelationshipModel>(r));
                            break;
                        }
                    }
                }
            }

            return relationshipId;
        }

        private static dtdlInterfaceModel GetDTDLModel(DigitalTwinsClient client, string dtmi)
        {
            dtdlInterfaceModel dtdlModel = new dtdlInterfaceModel();

            try
            {
                Response<ModelData> r = client.GetModel(dtmi);

                if (r.Value != null)
                {
                    dtdlModel = DigitalTwinInMemory.LoadInterfaceModel(r.Value.Model);
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Response {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return dtdlModel;
        }

        private static string ReadOptions()
        {
            Console.WriteLine("===============================");
            Console.WriteLine("Options:");
            Console.WriteLine(" 0. Quit");
            Console.WriteLine(" 1. Write property");
            Console.WriteLine(" 2. Send command to create twin");
            Console.WriteLine(" 3. Receive and respond to create twin command");
            Console.WriteLine(" 4. Delete twin");
            Console.WriteLine("===============================");
            Console.Write("> ");
            return Console.ReadLine();
        }

        private static bool GetPath(in string[] args, out string pfxPath)
        {
            if (args.Length > 0)
            {
                pfxPath = args[0];
            }
            else
            {
                Console.Write("Pfx file path (e.g. c:\\trustedzone\\contosodevice01.pfx): ");
                pfxPath = Console.ReadLine();
            }

            FileInfo info = new FileInfo(pfxPath);

            if (!info.Exists)
            {
                Console.WriteLine("Path to the pfx file is incorrect or missing. Please try again.");
                return false;
            }

            return true;
        }
    }
}
