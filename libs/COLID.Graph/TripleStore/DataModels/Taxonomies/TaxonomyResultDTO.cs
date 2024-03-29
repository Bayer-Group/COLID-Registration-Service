﻿using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.TripleStore.DataModels.Taxonomies
{
    public class TaxonomyResultDTO : BaseEntityResultDTO
    {
        public bool HasParent => Properties.Any(p => p.Key == COLID.Graph.Metadata.Constants.SKOS.Broader);

        public bool HasChild => Children.Any();
        public bool FoundInSearch { get; set; }
        public bool Expanded { get; set; }

        public IList<TaxonomyResultDTO> Children { get; set; }

        public TaxonomyResultDTO()
        {
            Children = new List<TaxonomyResultDTO>();
        }
    }
}
