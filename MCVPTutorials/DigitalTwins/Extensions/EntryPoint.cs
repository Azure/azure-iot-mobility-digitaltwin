// ---------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------

namespace Microsoft.Azure.ConnectedCar.DigitalTwins
{
    using Microsoft.Azure.ConnectedCar.Sdk;

    public static class EntryPoint
    {
        /// <summary>
        /// This is the entry point for the extensions package, and should be done identically in C# extension packages, other than the string argument.
        /// The string represents the name for the extension package--two extension packages with the same name will overwrite each other, with the latest one winning.
        /// </summary>
        public static int Main(string[] args) => ExtensionPackageInitializer.RunLongLivedAsync("DigitalTwinTelemetrySample", args).Result;
    }
}
