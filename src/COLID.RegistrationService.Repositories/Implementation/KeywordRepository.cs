using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using VDS.RDF.Query;
using COLID.RegistrationService.Common.DataModel.Keywords;
using COLID.Graph.Metadata.Repositories;
using Microsoft.Extensions.Logging;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class KeywordRepository : BaseRepository<Keyword>, IKeywordRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasKeywordsGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { InsertingGraph };

        public KeywordRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<KeywordRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public bool CheckIfKeywordLabelExists(string label, out string id)
        {
            // Declare out variable
            id = string.Empty;

            // If string is null or whitespace no keyword exists
            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            #region Define query

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = "SELECT ?subject @fromNamedGraphs WHERE { ?subject a @type. ?subject rdfs:label ?label. FILTER(ucase(str(?label)) = ucase(@label)) }"
            };

            parameterizedString.SetPlainLiteral("fromNamedGraphs", GetNamedSubGraphs(QueryGraphs));
            parameterizedString.SetUri("type", new Uri(Type));
            parameterizedString.SetLiteral("label", label);

            #endregion Define query

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            // If a keyword with the same label exists, the id is defined as out variable and returned true.
            if (results.Any())
            {
                id = results.Results.FirstOrDefault().GetNodeValuesFromSparqlResult("subject").Value;
                return true;
            }

            return false;
        }
    }
}
