using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Base;
using Swashbuckle.AspNetCore.Filters;

namespace COLID.RegistrationService.WebApi.Swagger.Examples
{
    /// <summary>
    /// Example for the ResourceRequestDTO
    /// </summary>
    public class ResourceRequestDTOExample : IExamplesProvider<ResourceRequestDTO>
    {
        /// <summary>
        /// Generates and returns an example for the class to be shown in Swagger UI.
        /// This method gets called via reflection by Swashbuckle.
        /// </summary>
        /// <returns>An example of the class</returns>
        public ResourceRequestDTO GetExamples()
        {
            return new ResourceRequestDTO()
            {
                Properties = new Dictionary<string, List<dynamic>>()
                {
                    [Graph.Metadata.Constants.RDF.Type] = new List<dynamic>() { Graph.Metadata.Constants.Resource.Type.Ontology },
                    [Graph.Metadata.Constants.Resource.DateCreated] = new List<dynamic>() { "2020-01-28T16:41:35.346Z" },
                    [Graph.Metadata.Constants.Resource.DateModified] = new List<dynamic>() { "2020-01-28T16:42:05.790Z" },
                    [Graph.Metadata.Constants.Resource.IncludesOntology] = new List<dynamic>() { "https://pid.bayer.com/0039ee80-6e1e-452b-b470-b28ae1f238a3" },
                    [Graph.Metadata.Constants.Resource.Distribution] = new List<dynamic>()
                    {
                        new BaseEntityRequestDTOExample().GetExamples()
                    },
                    [Graph.Metadata.Constants.Resource.MainDistribution] = new List<dynamic>()
                    {
                        new BaseEntityRequestDTOExample().GetExamples()
                    },
                    [Graph.Metadata.Constants.Resource.BaseUri] = new List<dynamic>()
                    {
                        new Entity()
                        {
                            Id = "https://pid.bayer.com/kos/19050/",
                            Properties = new Dictionary<string, List<dynamic>>()
                            {
                                [Graph.Metadata.Constants.RDF.Type] = new List<dynamic>() { Graph.Metadata.Constants.Identifier.Type }
                            }
                        }
                    },
                    [Graph.Metadata.Constants.Resource.HasInformationClassification] = new List<dynamic>() { "https://pid.bayer.com/kos/19050/Internal" },
                    [Graph.Metadata.Constants.Resource.LifecycleStatus] = new List<dynamic>() { "https://pid.bayer.com/kos/19050/underDevelopment" },
                    [Graph.Metadata.Constants.Resource.Author] = new List<dynamic>() { "any@bayer.com" },
                    [Graph.Metadata.Constants.Resource.HasVersion] = new List<dynamic>() { "1" },
                    [Graph.Metadata.Constants.Resource.HasResourceDefintion] = new List<dynamic>() { "The PID Ontology (PIDO) enables the descripton of metadata about resources which can have a dereferenceable URI as global unique identifier." },
                    [Graph.Metadata.Constants.Resource.HasDataSteward] = new List<dynamic>() { "this@bayer.com", "that@bayer.com" },
                    [Graph.Metadata.Constants.Resource.LastChangeUser] = new List<dynamic>() { "this@bayer.com" },
                    [Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus] = new List<dynamic>() { Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft },
                    [Graph.Metadata.Constants.Resource.Keyword] = new List<dynamic>() { "COLID" },
                    [Graph.Metadata.Constants.Resource.HasConsumerGroup] = new List<dynamic>() { "https://pid.bayer.com/kos/19050#2b3f0380-dd22-4666-a28b-7f1eeb82a5ff" },
                    [Graph.Metadata.Constants.EnterpriseCore.PidUri] = new List<dynamic>()
                    {
                        new Entity()
                        {
                            Id = "https://pid.bayer.com/kos/19050/",
                            Properties = new Dictionary<string, List<dynamic>>()
                            {
                                [Graph.Metadata.Constants.RDF.Type] = new List<dynamic>() { Graph.Metadata.Constants.Identifier.Type },
                                [Graph.Metadata.Constants.Identifier.HasUriTemplate] = new List<dynamic>() { "https://pid.bayer.com/kos/19050#13cd004a-a410-4af5-a8fc-eecf9436b58b" }
                            }
                        }
                    },
                    [Graph.Metadata.Constants.Resource.HasLabel] = new List<dynamic>() { "PID Ontology 4 (PID Metadata Ontology 4)" },
                    [Graph.Metadata.Constants.Resource.EditorialNote] = new List<dynamic>() { "Development Version 4. Added life cycle status to distribution Endpoints." },
                }
            };
        }
    }
}
