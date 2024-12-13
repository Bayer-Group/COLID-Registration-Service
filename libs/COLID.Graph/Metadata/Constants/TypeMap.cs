using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.Constants
{
    public static class TypeMap
    {

        public const string ConsumerGroupType = "ConsumerGroupType";
        public const string FirstResouceType = "FirstResouceType";
        public const string ExtendedUriTemplate = "ExtendedUriTemplate";
        public const string PermanentIdentifier = "PermanentIdentifier";
        public const string MetadataGraphConfiguration = "MetadataGraphConfiguration";
        public const string PidUriTemplate = "PidUriTemplate";
        public const string ResourceTemplate = "ResourceTemplate";
        public const string Keyword = "Keyword";
        /// <summary>
        /// Get Value of the types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeValue (string type)
        {
            switch (type)
            {
                case ConsumerGroupType:
                    return Constants.ConsumerGroup.Type;
                case FirstResouceType:
                    return Constants.Resource.Type.FirstResouceType;
                case ExtendedUriTemplate:
                    return Constants.ExtendedUriTemplate.Type;
                case PermanentIdentifier:
                    return Constants.Identifier.Type;
                case MetadataGraphConfiguration:
                    return Constants.MetadataGraphConfiguration.Type;
                case PidUriTemplate:
                    return Constants.PidUriTemplate.Type;
                case Keyword:
                    return Constants.Keyword.Type;
                case ResourceTemplate:
                    return Constants.ResourceTemplate.Type;
                default:
                   return "";
            }
        }
    }
}
