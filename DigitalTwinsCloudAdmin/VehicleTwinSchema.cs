using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalTwins
{
    class VehicleTwinSchema
    {
        public class digitalTwinsFileInfo
        {
            public string fileVersion { get; set; }
        }
        public class digitalTwinsGraph
        {
            public digitalTwins[] dt { get; set; }
            public relationships[] rel { get; set; }
        }
        public class digitalTwins
        {
            public string dtId { get; set; }
            public string etag { get; set; }
            public class metadata 
            {
                public string model { get; set; }
            }
        }
        public class relationships
        {
            public string relationshipId { get; set; }
            public string etag { get; set; }
            public string sourceId { get; set; }
            public string relationshipName { get; set; }
            public string targetId { get; set; }
        }
    }
}
