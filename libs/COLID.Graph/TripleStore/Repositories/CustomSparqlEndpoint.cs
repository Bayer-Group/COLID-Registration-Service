using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Repositories
{
    public class CustomSparqlEndpoint : SparqlRemoteEndpoint
    {
        private bool _bypassProxy;

        public CustomSparqlEndpoint(Uri endpointUri, IConfiguration configuration) : base(endpointUri) 
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
