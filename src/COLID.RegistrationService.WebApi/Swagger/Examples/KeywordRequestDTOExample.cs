using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Common.DataModel.Keywords;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the KeywordRequestDTO
    /// </summary>
    public class KeywordRequestDTOExample : IExamplesProvider<KeywordRequestDTO>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public KeywordRequestDTO GetExamples()
        {
            return new KeywordRequestDTO()
            {
                Properties = new Dictionary<string, List<dynamic>>()
                {
                    [RDF.Type] = new List<dynamic>() { Graph.Metadata.Constants.Keyword.Type },
                    [RDFS.Label] = new List<dynamic>() { "COLID" },
                }
            };
        }
    }
}
