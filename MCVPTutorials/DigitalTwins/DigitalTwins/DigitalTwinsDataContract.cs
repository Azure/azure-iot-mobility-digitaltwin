// <copyright file="DigitalTwinsDataContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace DigitalTwinsDataContracts.v2
{
    class DTUnit
    {
        public string dtID { get; set; }
        public string path { get; set; }
        public string value { get; set; }
        public string type { get; set; }
    }

    class dtdlInterfaceModel
    {
        public string id { get; set; }
        public string context { get; set; }
        public string comment { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public dtdlRelationshipModel[] relationships { get; set; }
        public dtdlPropertyModel[] properties { get; set; }
        public dtdlTelemetryModel[] telemetry { get; set; }
        public dtdlCommandModel[] commands { get; set; }
        public string extends { get; set; }
        public string schemas { get; set; }
    }

    class dtdlRelationshipModel
    {
        public string name { get; set; }
        public string id { get; set; }
        public string comment { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public string maxMultiplicity { get; set; }
        public string minMultiplicity { get; set; }
        public dtdlPropertyModel[] properties { get; set; }
        public string target { get; set; }
        public string writable { get; set; }
    }

    class dtdlPropertyModel
    {
        public string name { get; set; }
        public string schema { get; set; }
        public string id { get; set; }
        public string comment { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public string optional { get; set; }
        public string writable { get; set; }
    }

    class dtdlTelemetryModel
    {
        public string name { get; set; }
        public string schema { get; set; }
        public string id { get; set; }
        public string context { get; set; }
        public string comment { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public string unit { get; set; }
    }

    class dtdlCommandModel
    {
        public string name { get; set; }
        public string id { get; set; }
        public string comment { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public string commandType { get; set; }
        public dtdlCommandPayload[] request { get; set; }
        public dtdlCommandPayload[] response { get; set; }
    }

    class dtdlCommandPayload
    {
        public string name { get; set; }
        public string schema { get; set; }
        public string id { get; set; }
        public string comment { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
    }
}