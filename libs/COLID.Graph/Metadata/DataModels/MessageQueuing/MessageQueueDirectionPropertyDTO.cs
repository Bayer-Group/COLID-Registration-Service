namespace COLID.Graph.Metadata.DataModels.MessageQueuing
{
    public class MessageQueueDirectionPropertyDTO
    {
        /// <summary>
        /// The displayed the value and can be a string or entity
        /// </summary>
        public dynamic Value { get; set; }

        /// <summary>
        /// The URI of the value, if available. Otherwise null.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The Edge of the value
        /// </summary>
        public string Edge { get; set; }

        public MessageQueueDirectionPropertyDTO(dynamic value, string uri, string edge = null)
        {
            Value = value;
            Uri = uri;
            Edge = edge;
        }
    }
}
