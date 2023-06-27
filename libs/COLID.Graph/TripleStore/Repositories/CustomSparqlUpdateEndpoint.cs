using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using VDS.RDF.Update;

namespace COLID.Graph.TripleStore.Repositories
{
    public class CustomSparqlUpdateEndpoint : SparqlRemoteUpdateEndpoint
    {
        private bool _bypassProxy;

        public CustomSparqlUpdateEndpoint(Uri endpointUri, IConfiguration configuration) : base(endpointUri) 
        {
            _bypassProxy = configuration.GetValue<bool>("BypassProxy");
        }

        protected override void ApplyCustomRequestOptions(HttpWebRequest httpRequest)
        {
            if (_bypassProxy)
            {
                httpRequest.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                httpRequest.Proxy = null;
            }
        }
    }
}
