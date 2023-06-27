using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace COLID.Graph.TripleStore.DataModels.Base
{
    public class EntitySearch
    {
        [RegularExpression(Metadata.Constants.Regex.InvalidUriChars, ErrorMessage = "The characters '\', '\"' and ''' are not allowed")]
        public string SearchText { get; set; }

        [Required]
        public string Limit { get; set; }

        [Required]
        public string Offset { get; set; }

        [Required]
        public string Type { get; set; }

        public IList<string> Identifiers { get; set; }

        public EntitySearch()
        {
            Identifiers = new List<string>();
        }

        public EntitySearch(string type, string offset, string limit, string searchText = null, IList<string> identifiers = null)
        {
            Type = type;
            Offset = offset;
            Limit = limit;
            Identifiers = identifiers ?? new List<string>();
        }
    }
}
