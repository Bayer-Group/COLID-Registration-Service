using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.Extensions;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Tests.Common.Builder
{
    public abstract class AbstractEntityBuilder<T> where T : Entity
    {
        protected IDictionary<string, List<dynamic>> _prop = new Dictionary<string, List<dynamic>>();

        public abstract T Build();

        public virtual AbstractEntityBuilder<T> WithPidUri(string pidUriString, string uriTemplate = "https://pid.bayer.com/kos/19050#cb849b80-6d00-4c3a-8810-6e4f91ee0cd1")
        {
            // Create properties for Pid Uri
            IDictionary<string, List<dynamic>> pidUriProp = new Dictionary<string, List<dynamic>>();
            pidUriProp.Add(RDF.Type, new List<dynamic>() { Identifier.Type });

            if (!string.IsNullOrWhiteSpace(uriTemplate))
            {
                pidUriProp.Add(Identifier.HasUriTemplate, new List<dynamic>() { uriTemplate });
            }

            // Create Entity and assign properties to PID Uri
            Entity pidUri = new Entity(pidUriString, pidUriProp);

            // Create properties for resource
            CreateOrOverwriteProperty(EnterpriseCore.PidUri, pidUri);

            return this;
        }

        protected void CreateOrOverwriteProperty(string identifier, dynamic content)
        {
            _prop.AddOrUpdate(identifier, new List<dynamic>() { content });
        }

        protected void CreateOrOverwriteMultiProperty(string identifier, List<dynamic> content)
        {
            _prop.AddOrUpdate(identifier, content);
        }

        protected void CheckArgument(string value, string regex)
        {
            var match = Regex.Match(value, RegistrationService.Common.Constants.Regex.Email);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format("Passed argument {0} doesn't match with the valid regex pattern {1}", (value, regex)));
            }
        }
    }
}
