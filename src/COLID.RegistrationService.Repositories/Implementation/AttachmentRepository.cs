using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Utilities;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.DataModels.Attachments;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class AttachmentRepository : BaseRepository<Attachment>, IAttachmentRepository
    {
        public AttachmentRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<AttachmentRepository> logger) : base(configuration, tripleStoreRepository, logger)
        { }

        public bool IsAttachmentAllowedToDelete(string id, Uri resourceNamedGraph, Uri resourceDraftNamedGraph, Uri historicNamedGraph)
        {
            Guard.ArgumentNotNullOrWhiteSpace(id, "ID can not be null");
            Guard.IsValidUri(new Uri(id));

            /* The attachment may be deleted if there are no two different resources with different PID URIs
             * pointing to this attachment (in case of resource versions) or if there are generally
             * any published or historical resources pointing to this attachment.
             */
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                    @"SELECT ?pidUri ?lifecycleStatus 
                      FROM @resourceGraph
                      FROM @resourceDraftNamedGraph
                      FROM @historicGraph
                      WHERE {
                          @attachmentId a @attachmentType .
                          ?resource @hasAttachment @attachmentId .
                          ?resource @hasPidUri ?pidUri .
                          ?resource @hasEntryLifecycleStatus ?lifecycleStatus .
                      }"
            };

            parameterizedString.SetUri("resourceGraph", resourceNamedGraph);
            parameterizedString.SetUri("resourceDraftNamedGraph", resourceDraftNamedGraph);
            parameterizedString.SetUri("historicGraph", historicNamedGraph);
            parameterizedString.SetUri("attachmentId", new Uri(id));
            parameterizedString.SetUri("attachmentType", new Uri(AttachmentConstants.Type));
            parameterizedString.SetUri("hasAttachment", new Uri(AttachmentConstants.HasAttachment));
            parameterizedString.SetUri("hasPidUri", new Uri(EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (results.Any())
            {
                var resultList = results.Select(e =>
                {
                    var pidUris = e.GetNodeValuesFromSparqlResult("pidUri").Value;
                    var entryLifecycleStatus = e.GetNodeValuesFromSparqlResult("lifecycleStatus").Value;
                    return new Tuple<string, string>(pidUris, entryLifecycleStatus);
                });

                if (resultList.Select(r => r.Item1).Distinct().Count() > 1)
                {
                    return false;
                }

                if(LifecycleStatusCriticalForDeletion(resultList))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if any lifecyclestatus is present in the results, that disallowes the deletion of the attachment.
        /// </summary>
        /// <param name="resultList">The results containing all present lifecycleStatus</param>
        /// <returns>True if lifecycleStatus present, which disallowes a deletion. Else false.</returns>
        private static bool LifecycleStatusCriticalForDeletion(IEnumerable<Tuple<string, string>> resultList)
        {
            var lifecycleStatus = resultList.Select(r => r.Item2).Distinct();
            return lifecycleStatus.Any(lcs =>
                lcs == Resource.ColidEntryLifecycleStatus.Published ||
                //lcs == Resource.ColidEntryLifecycleStatus.Historic ||
                lcs == Resource.ColidEntryLifecycleStatus.MarkedForDeletion);
        }
    }
}
