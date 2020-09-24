using System.Collections.Generic;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.Graph.TripleStore.DataModels.Base
{
    public class EntityTypeDto : Entity
    {
        public string Label => Properties.GetValueOrNull(Metadata.Constants.RDFS.Label, true);

        public string Description => Properties.GetValueOrNull(Metadata.Constants.RDFS.Comment, true);

        public bool Instantiable => IsInstantiable();

        public IList<EntityTypeDto> SubClasses { get; set; }

        public EntityTypeDto() : base()
        {
            SubClasses = new List<EntityTypeDto>();
        }

        public EntityTypeDto(string id, IDictionary<string, List<dynamic>> properties = null) : base(id, properties)
        {
            SubClasses = new List<EntityTypeDto>();
        }

        private bool IsInstantiable()
        {
            var _abstract = Properties.GetValueOrNull(Metadata.Constants.DASH.Abstract, true);
            return !string.IsNullOrWhiteSpace(_abstract) && _abstract == "false";
        }
    }
}
