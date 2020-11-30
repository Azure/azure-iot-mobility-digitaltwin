namespace DigitalTwins
{
    using System;
    using Azure.Identity;
    using Azure.DigitalTwins.Core;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Collections.Generic;
    using DigitalTwinsDataContracts.v2;

    /*
    {
        "dtID": "vehicle-contosocar01",
        "path": "/Speed",
        "value": "20"
    }
    */

    class Program
    {
        public static List<dtdlInterfaceModel> gVehicle { get; set; }

        static async Task Main(string[] args)
        {
            DefaultAzureCredential cred = new DefaultAzureCredential();
            DigitalTwinsClient client = new DigitalTwinsClient(new Uri("https://mobility-vss.api.wus2.digitaltwins.azure.net"), cred);

            gVehicle = new List<dtdlInterfaceModel>();
            
            bool loop = true;

            while(loop)
            {
                int result;
                string c = ReadOptions();

                if(int.TryParse(c, out result))
                {
                    int i = int.Parse(c.ToString());
                    switch(i)
                    {
                        case 0: loop = false;
                            break;
                        case 1: await DigitalTwinMethods.ReadProperty(client);
                            break;
                        case 2: await DigitalTwinMethods.WriteProperty(client);
                            break;
                        case 3: await DigitalTwinModelMethods.ListModels(client);
                            break;
                        case 4: await DigitalTwinModelMethods.GetModel(client);
                            break;
                        case 5: await DigitalTwinModelMethods.GetSchemaType(client);
                            break;
                        case 6: await DigitalTwinModelMethods.CreateModels(client);
                            break;
                        case 7: await DigitalTwinMethods.CreateTwin(client);
                            break;
                        case 8: await DigitalTwinMethods.GetTwin(client);
                            break;
                        case 9: DigitalTwinMethods.DeleteTwin(client);
                            break;
                        case 10: await DigitalTwinMethods.CreateRelationship(client);
                            break;
                        case 11: await DigitalTwinMethods.GetRelationships(client);
                            break;
                        case 12: await DigitalTwinMethods.DeleteRelationship(client);
                            break;
                        default: break;
                    }
                }
            }
        }

        static string ReadOptions()
        {
            Console.WriteLine("===============================");
            Console.WriteLine("Options:");
            Console.WriteLine(" 0. Quit");
            Console.WriteLine(" 1. Read property");
            Console.WriteLine(" 2. Write property");
            Console.WriteLine(" 3. List models");
            Console.WriteLine(" 4. Get model");
            Console.WriteLine(" 5. Get schema type of property");
            Console.WriteLine(" 6. Upload models");
            Console.WriteLine(" 7. Create twin");
            Console.WriteLine(" 8. Get twin");
            Console.WriteLine(" 9. Delete twin");
            Console.WriteLine(" 10. CreateRelationship");
            Console.WriteLine(" 11. List Relationships");
            Console.WriteLine(" 12. Delete Relationship");
            Console.WriteLine("===============================");
            Console.Write("> ");
            return Console.ReadLine();
        }

        public static string PrettifyJson(string json)
        {
            object jsonObj = JsonSerializer.Deserialize<object>(json);
            return JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
