using System;
using System.Collections.Generic;
using System.Text.Json;
using DigitalTwinsDataContracts.v2;

namespace DigitalTwins
{
    class DigitalTwinInMemory
    {
        public static dtdlInterfaceModel LoadInterfaceModel(string interfaceModelJson)
        {
            dtdlInterfaceModel v = new dtdlInterfaceModel();

            using (JsonDocument jdoc = JsonDocument.Parse(interfaceModelJson))
            {
                JsonElement ele = jdoc.RootElement;

                v.id = ele.GetProperty("@id").ToString();
                v.comment = ele.GetProperty("comment").ToString();
                v.displayName = ele.GetProperty("displayName").ToString();

                JsonElement props = ele.GetProperty("contents");
                List<dtdlRelationshipModel> dtrelationships = new List<dtdlRelationshipModel>();
                List<dtdlPropertyModel> dtproperties = new List<dtdlPropertyModel>();
                List<dtdlTelemetryModel> dttelemetry = new List<dtdlTelemetryModel>();
                List<dtdlCommandModel> dtcommands = new List<dtdlCommandModel>();
                List<dtdlCommandPayload> dtrequest = new List<dtdlCommandPayload>();
                List<dtdlCommandPayload> dtresponse = new List<dtdlCommandPayload>();

                if (props.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement subele in props.EnumerateArray())
                    {
                        if (subele.ValueKind == JsonValueKind.Object)
                        {
                            string dttype = "";
                            string node = "{";
                            int i = 0;

                            foreach (JsonProperty jprop in subele.EnumerateObject())
                            {
                                if (i == 0)
                                {
                                    dttype = jprop.Value.ToString();
                                }
                                else
                                {
                                    if (jprop.Value.ValueKind == JsonValueKind.Object)
                                    {
                                        string innerDTType = jprop.Name;
                                        string innerNode = "{";
                                        int j = 0;
                                        foreach (JsonProperty p in jprop.Value.EnumerateObject())
                                        {
                                            if (jprop.Value.ValueKind == JsonValueKind.String)
                                            {
                                                innerNode += string.Format(" \"{0}\" : \"{1}\",", jprop.Name.Replace("@", ""), jprop.Value);
                                            }

                                            j++;
                                        }

                                        innerNode = node.TrimEnd(',');
                                        innerNode += " }";

                                        if (innerDTType == "request")
                                        {
                                            dtdlCommandPayload dtcommand = JsonSerializer.Deserialize<dtdlCommandPayload>(innerNode);
                                            dtrequest.Add(dtcommand);
                                        }

                                        if (innerDTType == "response")
                                        {
                                            dtdlCommandPayload dtcommand = JsonSerializer.Deserialize<dtdlCommandPayload>(innerNode);
                                            dtresponse.Add(dtcommand);
                                        }
                                    }
                                    else
                                    {
                                        node += string.Format(" \"{0}\" : \"{1}\",", jprop.Name.Replace("@", ""), jprop.Value.ToString());
                                    }
                                }

                                i++;
                            }

                            node = node.TrimEnd(',');
                            node += " }";

                            if (dttype == "Relationship")
                            {
                                dtdlRelationshipModel dtrel = JsonSerializer.Deserialize<dtdlRelationshipModel>(node);
                                dtrelationships.Add(dtrel);
                            }

                            if (dttype == "Property")
                            {
                                dtdlPropertyModel dtprop = JsonSerializer.Deserialize<dtdlPropertyModel>(node);
                                dtproperties.Add(dtprop);
                            }

                            if (dttype == "Telemetry")
                            {
                                dtdlTelemetryModel dttelem = JsonSerializer.Deserialize<dtdlTelemetryModel>(node);
                                dttelemetry.Add(dttelem);
                            }

                            if (dttype == "Command")
                            {
                                dtdlCommandModel dtcommand = JsonSerializer.Deserialize<dtdlCommandModel>(node);
                                dtcommand.request = dtrequest.ToArray();
                                dtcommand.response = dtresponse.ToArray();
                                dtcommands.Add(dtcommand);
                                dtrequest.Clear();
                                dtresponse.Clear();
                            }
                        }
                    }
                }

                v.properties = dtproperties.ToArray();
                v.relationships = dtrelationships.ToArray();
                v.telemetry = dttelemetry.ToArray();
                v.commands = dtcommands.ToArray();
            }
            return v;
        }
    }
}
