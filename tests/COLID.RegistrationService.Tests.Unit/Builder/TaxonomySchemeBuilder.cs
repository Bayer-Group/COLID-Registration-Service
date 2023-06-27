using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Taxonomies;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public class TaxonomySchemeBuilder
    {
        private IList<Taxonomy> _prop = new List<Taxonomy>();

        public IList<Taxonomy> Build()
        {
            return _prop;
        }

        public TaxonomySchemeBuilder GenerateSampleMathematicalTaxonomyList()
        {
            _prop.Add(GetBroaderTaxonomy("Classification Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0"));
            _prop.Add(GetBroaderTaxonomy("Regression Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/1"));
            _prop.Add(GetBroaderTaxonomy("Time Series Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/2"));
            _prop.Add(GetBroaderTaxonomy("Clustering Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/3"));
            _prop.Add(GetNarrowerTaxonomy("Deep Learning Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0"));
            _prop.Add(GetNarrowerTaxonomy("Convolutional Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/4", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));
            _prop.Add(GetNarrowerTaxonomy("Neural Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/5", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));
            _prop.Add(GetNarrowerTaxonomy("LTSM Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/6", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));
            _prop.Add(GetNarrowerTaxonomy("Generative Adversarial Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/7", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));
            _prop.Add(GetNarrowerTaxonomy("Deep Learning Model 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0"));
            _prop.Add(GetNarrowerTaxonomy("Convolutional Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/11", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));
            _prop.Add(GetNarrowerTaxonomy("Neural Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/12", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));
            _prop.Add(GetNarrowerTaxonomy("LTSM Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/13", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));
            _prop.Add(GetNarrowerTaxonomy("Generative Adversarial Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/14", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));
            _prop.Add(GetNarrowerTaxonomy("Deep Learning Model 3", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/10", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0"));
            return this;
        }

        public TaxonomySchemeBuilder GenerateSampleInformationClassificationTaxonomyList()
        {
            _prop.Add(GetBroaderTaxonomy("Open", Graph.Metadata.Constants.Resource.InformationClassification.Open));
            _prop.Add(GetBroaderTaxonomy("Restricted", Graph.Metadata.Constants.Resource.InformationClassification.Restricted));
            _prop.Add(GetBroaderTaxonomy("Secret", Graph.Metadata.Constants.Resource.InformationClassification.Secret));
            _prop.Add(GetBroaderTaxonomy("Internal", Graph.Metadata.Constants.Resource.InformationClassification.Internal));
            return this;
        }

        public TaxonomyResultDTO GenerateSampleTaxonomy()
        {
            var deepLearningModel = GetNarrowerTaxonomyResult("Deep Learning Model",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            var deepLearningModel2 = GetNarrowerTaxonomyResult("Deep Learning Model 2",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            var deepLearningModel3 = GetNarrowerTaxonomyResult("Deep Learning Model 3",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/10",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("Convolutional Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/4", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("Neural Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/5", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("LTSM Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/6", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("Generative Adversarial Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/7", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("Convolutional Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/11", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("Neural Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/12", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("LTSM Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/13", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("Generative Adversarial Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/14", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            var classificationModel = GetBroaderTaxonomyResult("Classification Model",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            classificationModel.Children.Add(deepLearningModel);
            classificationModel.Children.Add(deepLearningModel2);
            classificationModel.Children.Add(deepLearningModel3);

            return classificationModel;
        }

        public IList<TaxonomyResultDTO> GenerateSampleTaxonomies()
        {
            var list = new List<TaxonomyResultDTO>();

            #region Classification Model

            var classificationModel = GetBroaderTaxonomyResult("Classification Model",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            TaxonomyResultDTO deepLearningModel = GetNarrowerTaxonomyResult("Deep Learning Model",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            TaxonomyResultDTO deepLearningModel2 = GetNarrowerTaxonomyResult("Deep Learning Model 2",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            TaxonomyResultDTO deepLearningModel3 = GetNarrowerTaxonomyResult("Deep Learning Model 3",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/10",
                "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/0");

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("Convolutional Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/4", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("Neural Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/5", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("LTSM Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/6", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel.Children
                .Add(GetNarrowerTaxonomyResult("Generative Adversarial Networks", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/7", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/8"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("Convolutional Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/11", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("Neural Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/12", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("LTSM Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/13", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            deepLearningModel2.Children
                .Add(GetNarrowerTaxonomyResult("Generative Adversarial Networks 2", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/14", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/9"));

            classificationModel.Children.Add(deepLearningModel);
            classificationModel.Children.Add(deepLearningModel2);
            classificationModel.Children.Add(deepLearningModel3);

            #endregion Classification Model

            list.Add(classificationModel);
            list.Add(GetBroaderTaxonomyResult("Regression Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/1"));
            list.Add(GetBroaderTaxonomyResult("Time Series Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/2"));
            list.Add(GetBroaderTaxonomyResult("Clustering Model", "https://pid.bayer.com/2fdbd958-b0c3-4a4d-96a9-41641964140d/3"));

            return list;
        }

        private Taxonomy GetBroaderTaxonomy(string label, string id)
        {
            return new TaxonomyBuilder()
                .WithId(id)
                .WithType("https://pid.bayer.com/kos/19050/MathematicalModelCategory")
                .WithPrefLabel(label)
                .Build();
        }

        private TaxonomyResultDTO GetBroaderTaxonomyResult(string label, string id)
        {
            return new TaxonomyBuilder()
                .WithId(id)
                .WithType("https://pid.bayer.com/kos/19050/MathematicalModelCategory")
                .WithPrefLabel(label)
                .BuildResultDTO();
        }

        private Taxonomy GetNarrowerTaxonomy(string label, string id, string broader)
        {
            return new TaxonomyBuilder()
                .WithId(id)
                .WithType("https://pid.bayer.com/kos/19050/MathematicalModelCategory")
                .WithBroader(broader)
                .WithPrefLabel(label)
                .Build();
        }

        private TaxonomyResultDTO GetNarrowerTaxonomyResult(string label, string id, string broader)
        {
            return new TaxonomyBuilder()
                .WithId(id)
                .WithType("https://pid.bayer.com/kos/19050/MathematicalModelCategory")
                .WithBroader(broader)
                .WithPrefLabel(label)
                .BuildResultDTO();
        }
    }
}
