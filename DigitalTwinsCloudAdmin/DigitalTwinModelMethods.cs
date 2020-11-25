namespace DigitalTwins
{
    using System;
    using System.Text.Json;
    using Azure.DigitalTwins.Core;
    using Azure;
    using System.Threading.Tasks;
    using DigitalTwinsDataContracts.v2;
    using System.Collections.Generic;
    using System.IO;

    class DigitalTwinModelMethods
    {
        public static async Task ListModels(DigitalTwinsClient client)
        {
            string[] dependenciesFor = null;

            try
            {
                AsyncPageable<ModelData> results = client.GetModelsAsync(dependenciesFor, false);
                await foreach (ModelData md in results)
                {
                    Console.WriteLine(md.Id);
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
        }

        public static async Task GetSchemaType(DigitalTwinsClient client)
        {
            Console.Write("modelId (dtmi): ");
            string dtmi = Console.ReadLine();

            Console.Write("name: ");
            string name = Console.ReadLine();

            string schemaType = "unknown";
            string description = "NA";

            try
            {
                Response<ModelData> r = await client.GetModelAsync(dtmi);

                if (r.Value != null)
                {
                    using (JsonDocument jdoc = JsonDocument.Parse(r.Value.Model))
                    {
                        JsonElement ele = jdoc.RootElement;
                        JsonElement propList = ele.GetProperty("contents");
                        if( propList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement subele in propList.EnumerateArray())
                            {
                                if(name == subele.GetProperty("name").ToString())                       
                                {
                                    schemaType = subele.GetProperty("schema").ToString();
                                    description = subele.GetProperty("description").ToString();
                                    break;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("\tschema: {0} \r\n\tdescription: {1}", schemaType, description);
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

        public static dtdlInterfaceModel GetDTDLModel(DigitalTwinsClient client, string dtmi)
        {
            dtdlInterfaceModel dtdlModel = new dtdlInterfaceModel();

            try
            {
                Response<ModelData> r = client.GetModel(dtmi);

                if (r.Value != null)
                {
                    if (!Program.gVehicle.Exists(item => item.id == dtmi))
                    {
                        dtdlModel = DigitalTwinInMemory.LoadInterfaceModel(r.Value.Model);
                    }
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

        public static async Task GetModel(DigitalTwinsClient client)
        {
            Console.Write("modelId (dtmi): ");
            string dtmi = Console.ReadLine();

            try
            {
                Response<ModelData> r = await client.GetModelAsync(dtmi);

                if (r.Value != null)
                {
                    if (!Program.gVehicle.Exists(item => item.id == dtmi))
                    {
                        dtdlInterfaceModel dtdlModel = DigitalTwinInMemory.LoadInterfaceModel(r.Value.Model);
                        Program.gVehicle.Add(dtdlModel);
                    }

                    Console.WriteLine(Program.PrettifyJson(r.Value.Model));
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
        }

        public static async Task CreateModels(DigitalTwinsClient client)
        {
            List<string> paths = new List<string>();
            bool loop = true;

            while (loop)
            {
                Console.WriteLine("0 = Exit \r\n1 = Add full path to file");
                Console.Write("Command: ");
                string s = Console.ReadLine();
                if (s.Contains("1"))
                {
                    Console.Write("Full path: ");
                    s = Console.ReadLine();
                    paths.Add(s);
                }
                else
                {
                    loop = false;
                }
            }

            if (paths.Count < 1)
            {
                Console.WriteLine("Please supply at least one model file name to upload to the service");
                return;
            }

            try
            {
                List<string> dtdlList = new List<string>();
                paths.ForEach(delegate (string filename)
                {
                    StreamReader r = new StreamReader(filename);
                    string dtdl = r.ReadToEnd();
                    r.Close();
                    dtdlList.Add(dtdl);
                });
                Response<ModelData[]> res = await client.CreateModelsAsync(dtdlList);
                Console.WriteLine($"Model(s) created successfully!");
                foreach (ModelData md in res.Value)
                    Console.WriteLine(md.Model);
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
    }
}
