using System;
using System.Collections.Generic;

namespace COLID.Swagger.Configuration
{
    public class ColidSwaggerOptions
    {
        /// <summary>
        /// The Id of the Swagger Client
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The email address to contact the developers
        /// </summary>
        public string ContactEmail { get; set; }

        /// <summary>
        /// The label of the current environment to show on the Swagger page
        /// </summary>
        /// <example>LOCAL / DEV / QA / PROD</example>
        public string EnvironmentLabel { get; set; }

        /// <summary>
        /// The URL to the documentation of COLID
        /// </summary>
        public string DocumentationUrl { get; set; }

        /// <summary>
        /// The URL to the API documentation of COLID
        /// </summary>
        public string DocumentationApiUrl { get; set; }

        /// <summary>
        /// List of all scopes for the Swagger page and their description
        /// </summary>
        public Dictionary<string, string> Scopes { get; set; }

        public ColidSwaggerOptions()
        {
        }
    }
}
