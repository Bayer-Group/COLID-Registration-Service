using AutoMapper;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.Graph.TripleStore.MappingProfiles
{
    public class EntityNameResolver : IValueResolver<Entity, BaseEntityResultDTO, string>
    {
        public EntityNameResolver()
        {
        }

        public string Resolve(Entity source, BaseEntityResultDTO destination, string destMember, ResolutionContext context)
        {
            string prefLabel = source?.Properties.GetValueOrNull(Metadata.Constants.SKOS.PrefLabel, true);
            string rdfLabel = source?.Properties.GetValueOrNull(Metadata.Constants.RDFS.Label, true);

            if (!string.IsNullOrWhiteSpace(prefLabel))
            {
                return prefLabel;
            }

            if (!string.IsNullOrWhiteSpace(rdfLabel))
            {
                return rdfLabel;
            }

            return string.Empty;
        }
    }
}
