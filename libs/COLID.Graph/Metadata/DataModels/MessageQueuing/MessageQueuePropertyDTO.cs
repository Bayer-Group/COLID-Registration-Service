using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.MessageQueuing
{
    public class MessageQueuePropertyDTO
    {
        /// <summary>
        /// The outbound values
        /// </summary>
        public IList<MessageQueueDirectionPropertyDTO> Outbound { get; set; }

        /// <summary>
        /// The inbound values, especially for the linktypes
        /// </summary>
        public IList<MessageQueueDirectionPropertyDTO> Inbound { get; set; }

        public MessageQueuePropertyDTO()
        {
            Outbound = new List<MessageQueueDirectionPropertyDTO>();
            Inbound = new List<MessageQueueDirectionPropertyDTO>();
        }
    }
}
