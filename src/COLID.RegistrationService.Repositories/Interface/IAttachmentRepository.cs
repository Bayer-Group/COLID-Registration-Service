using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Attachments;
using COLID.Graph.TripleStore.Repositories;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all consumer group related operations.
    /// </summary>
    public interface IAttachmentRepository : IBaseRepository<Attachment>
    {
        bool IsAttachmentAllowedToDelete(string id, Uri historicNamedGraph, Uri resourceNamedGraph, Uri draftResourceNamedGraph);
    }
}
