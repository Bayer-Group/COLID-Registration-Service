using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.ResourceTemplates;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all Resource template related operations.
    /// </summary>
    public interface IResourceTemplateRepository : IBaseRepository<ResourceTemplate>
    {

        /// <summary>
        /// Checks if a resource template is used by a consumer group
        /// </summary>
        /// <param name="identifier">Identifier of resource template to check</param>
        /// <param name="referenceId">Identifier of consumer group reference</param>
        /// <param name="resourceTemplateGraph">Named graph for resource templates</param>
        /// <param name="consumerGroupGraph">Named graph for consumer groups</param>
        /// <returns>true if reference exists, otherwise false</returns>
        bool CheckResourceTemplateHasConsumerGroupReference(string identifier, Uri resourceTemplateGraph, Uri consumerGroupGraph, out string referenceId);

    }
}
