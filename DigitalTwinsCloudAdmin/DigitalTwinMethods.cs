namespace DigitalTwins
{
    using System;
    using Azure.DigitalTwins.Core;
    using Azure.DigitalTwins.Core.Serialization;
    using Azure;
    using System.Threading.Tasks;
    using System.Text.Json;
    using DigitalTwinsDataContracts.v2;
    using System.Linq;

    class DigitalTwinMethods
    {
        public static async Task ReadProperty(DigitalTwinsClient client)
        {
            string dtID;
            string prop;

            Console.Write("Digital twin ID: ");
            dtID = Console.ReadLine();

            Console.Write("Property name: ");
            prop = Console.ReadLine();

            string q = string.Format("SELECT v.{0}, v.$metadata.{0}.lastUpdateTime FROM DIGITALTWINS v WHERE v.$dtId = '{1}' AND IS_PRIMITIVE(v.{0}) and IS_PRIMITIVE(v.$metadata.{0}.lastUpdateTime)", prop, dtID);
            Console.WriteLine("Query: {0}\n\r", q);

            AsyncPageable<string> result = client.QueryAsync(q);

            await foreach (string s in result)
            {
                Console.WriteLine(s);
            }
        }

        public static async Task WriteProperty(DigitalTwinsClient client)
        {
            string input;
            string dtid = "";
            string patchpayload = "";

            Console.WriteLine("Sample json");
            Console.WriteLine("{\"dtID\": \"vehicle-contosocar01\", \"path\": \"/Speed\", \"value\": \"20\", \"type\": \"integer\"}");
            Console.Write("Add json:");
            input = Console.ReadLine();

            GetPatchPayload(input, ref dtid, ref patchpayload);
            Console.WriteLine("patchPayload = {0}", patchpayload);

            await client.UpdateDigitalTwinAsync(dtid, patchpayload);
        }

        public static async Task WriteTelemetry(DigitalTwinsClient client)
        {
            string input;
            string dtid = "";
            string patchpayload = "";

            Console.WriteLine("Sample json");
            Console.WriteLine("{\"dtID\": \"hood-contosocar01\", \"path\": \"/IsOpenTelemetry\", \"value\": \"true\", \"type\": \"boolean\"}");
            Console.Write("Add json:");
            input = Console.ReadLine();

            GetPatchPayload(input, ref dtid, ref patchpayload);
            Console.WriteLine("patchPayload = {0}", patchpayload);

            try
            {
                await client.PublishTelemetryAsync(dtid, patchpayload);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Response {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a twin with the specified properties
        /// </summary>
        public static async Task CreateTwin(DigitalTwinsClient client)
        {
            Console.Write("vehicleId: ");
            string vehicleID = Console.ReadLine();

            Console.Write("modelId: ");
            string dtmi = Console.ReadLine();

            try
            {
                Response<ModelData> res = client.GetModel(dtmi);
                string twinId = string.Format("{0}-{1}", res.Value.DisplayName.Values.ElementAt(0), vehicleID);

                Console.Write("twinId (suggest {0}): ", twinId);
                twinId = Console.ReadLine();

                var twinData = new BasicDigitalTwin
                {
                    Id = twinId,
                    Metadata =
                {
                    ModelId = dtmi,
                },
                };

                try
                {
                    await client.CreateDigitalTwinAsync(twinData.Id, JsonSerializer.Serialize(twinData));
                    Console.WriteLine($"Twin '{twinId}' created successfully!");
                }
                catch (RequestFailedException e)
                {
                    Console.WriteLine($"Error {e.Status}: {e.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex}");
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static async Task GetTwin(DigitalTwinsClient client)
        {
            Console.Write("twinId: ");
            string twinId = Console.ReadLine();

            try
            {
                Response<string> res = await client.GetDigitalTwinAsync(twinId);
                if (res != null)
                {
                    Console.WriteLine(Program.PrettifyJson(res.Value.ToString()));
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        public static void DeleteTwin(DigitalTwinsClient client)
        {
            Console.Write("twinId: ");
            string twinId = Console.ReadLine();

            try
            {   
                Response res = client.DeleteDigitalTwin(twinId);

                Console.WriteLine("Deleting twin {0}, HTTP status {1}", twinId, res.ReasonPhrase);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        public static async Task CreateRelationship(DigitalTwinsClient client)
        {

            Console.Write("source twinId: ");
            string sourceTwinId = Console.ReadLine();

            Console.Write("target twinId: ");
            string targetTwinId = Console.ReadLine();

            string relationshipId = Guid.NewGuid().ToString();

            string relationshipName = GetRelationshipFromDigitalTwinIds(client, sourceTwinId, targetTwinId);

            var relationship = new BasicRelationship
            {
                Id = relationshipId,
                SourceId = sourceTwinId,
                TargetId = targetTwinId,
                Name = relationshipName
            };

            string rel = JsonSerializer.Serialize(relationship);

            try
            {
                await client.CreateRelationshipAsync(sourceTwinId, relationshipId, rel);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        public static async Task GetRelationships(DigitalTwinsClient client)
        {
            Console.Write("source twinId: ");
            string sourceTwinId = Console.ReadLine();

            try
            {
                AsyncPageable<string> list = client.GetRelationshipsAsync(sourceTwinId);
                
                await foreach(string s in list)
                {
                    BasicRelationship rel = JsonSerializer.Deserialize<BasicRelationship>(s);
                    Console.WriteLine("id={0}, name={1}", rel.Id, rel.Name);
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        public static async Task DeleteRelationship(DigitalTwinsClient client)
        {

            Console.Write("source twinId: ");
            string sourceTwinId = Console.ReadLine();

            Console.Write("relationshipId: ");
            string relationshipId = Console.ReadLine();

            try
            {
                await client.DeleteRelationshipAsync(sourceTwinId, relationshipId);
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine($"Error {e.Status}: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
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

                dtdlInterfaceModel targetDTDL = DigitalTwinModelMethods.GetDTDLModel(client, sourcedtmi);
                if (targetDTDL != null)
                {
                    foreach (dtdlRelationshipModel r in targetDTDL.relationships)
                    {
                        if (r.target == targetdtmi)
                        {
                            relationshipId = r.name;
                            Console.WriteLine("Relationship created: {0}", JsonSerializer.Serialize<dtdlRelationshipModel>(r));
                            break;
                        }
                    }
                }
            }

            return relationshipId;
        }

        private static void GetPatchPayload(string jsonInput, ref string dtid, ref string patchpayload )
        {
            var updateOps = new UpdateOperationsUtility();
            DTUnit du = JsonSerializer.Deserialize<DTUnit>(jsonInput);
            dtid = du.dtID;

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

            patchpayload = updateOps.Serialize();
        }
    }
}
