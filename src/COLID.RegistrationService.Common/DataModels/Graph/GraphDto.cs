using System;
using System.Collections.Generic;
using System.Text;
using COLID.RegistrationService.Common.Enums.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace COLID.RegistrationService.Common.DataModel.Graph
{
    public class GraphDto
    {
        /// <summary>
        /// Uri of the named graph
        /// </summary>
        public Uri Name { get; set; }

        /// <summary>
        /// Describes the status of the named graph in the database
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public GraphStatus Status { get; set; }

        /// <summary>
        /// Specifies the start time since the graph is used by the system, 
        /// if a metadata graph configuration is referenced.
        /// </summary>
        public string StartTime { get; set; }

        public GraphDto(Uri name, GraphStatus status, string startTime)
        {
            Name = name;
            Status = status;
            StartTime = startTime;
        }
    }
}
