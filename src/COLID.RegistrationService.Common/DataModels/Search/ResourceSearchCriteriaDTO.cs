using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace COLID.RegistrationService.Common.DataModel.Search
{
    public class ResourceSearchCriteriaDTO
    {
        /// <summary>
        /// SearchText for the full text search, where label, definition and PID URI of an entry are searched.
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// Filters all entries that have the entry lifecycle status  draft.
        /// </summary>
        public bool Draft { get; set; }

        /// <summary>
        /// Filters all entries that have the entry lifecycle status published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Filters all entries that have the entry lifecycle status markedForDeletion.
        /// </summary>
        public bool MarkedForDeletion { get; set; }

        /// <summary>
        /// Filters all entries that have the specified consumer group.
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// Filters all entries that have the specified last change user by email.
        /// </summary>
        [EmailAddress]
        public string LastChangeUser { get; set; }

        /// <summary>
        /// Filters all entries that have the specified author by email.
        /// </summary>
        [EmailAddress]
        public string Author { get; set; }

        /// <summary>
        /// Filters all entries that have the specified type by uri.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Specifies which property is to be used for ordering.
        /// </summary>
        public string OrderPredicate { get; set; }

        /// <summary>
        /// Keyword used to sort result sets in either ascending (asc) or descending (desc) order
        /// </summary>
        [RegularExpression("asc|desc")]
        public string Sequence { get; set; }

        /// <summary>
        /// Specifies how many entries should be displayed in the result. If no limit is set, a maximum of 10 entries will be returned.
        /// </summary>
        [Range(0, 1000)]
        public int Limit { get; set; }

        /// <summary>
        /// Starting point on the sliding window. If no offset is set, offset will be 0.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Specifies the PID URIs for which resources should be returned. Specified filters affect the list.
        /// </summary>
        public List<Uri> PidUris { get; set; }

        /// <summary>
        /// Specifies the base uris for which resources should be returned. Specified filters affect the list.
        /// </summary>
        public List<Uri> BaseUris { get; set; }
    }
}
