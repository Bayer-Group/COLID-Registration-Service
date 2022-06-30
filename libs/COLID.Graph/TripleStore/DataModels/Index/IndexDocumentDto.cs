using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.MessageQueuing;
using COLID.Graph.TripleStore.DataModels.Resources;

namespace COLID.Graph.TripleStore.DataModels.Index
{
    public class IndexDocumentDto
    {
        public Uri DocumentId { get; set; }

        public ResourceCrudAction Action { get; set; }

        public string DocumentLifecycleStatus { get; set; }

        public IDictionary<string, MessageQueuePropertyDTO> Document { get; set; }


        public IndexDocumentDto(Uri documentId, ResourceCrudAction action, string documentLifecycleStatus, IDictionary<string, MessageQueuePropertyDTO> document = null)
        {
            DocumentId = documentId;
            Action = action;
            DocumentLifecycleStatus = documentLifecycleStatus;
            Document = document ?? new Dictionary<string, MessageQueuePropertyDTO>();
        }
    }
}
