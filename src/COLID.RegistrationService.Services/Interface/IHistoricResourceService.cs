//using System;
//using System.Collections.Generic;
//using COLID.Graph.Metadata.DataModels.Metadata;
//using COLID.Graph.Metadata.DataModels.Resources;
//using COLID.RegistrationService.Common.DataModel.Resources;

//namespace COLID.RegistrationService.Services.Interface
//{
//    public interface IHistoricResourceService
//    {
//        /// <summary>
//        /// Determine all historic entries, identified by the given pidUri, and returns overview information of them.
//        /// </summary>
//        /// <param name="pidUri">the resource to search for</param>
//        /// <returns>a list of resource-information related to the pidUri</returns>
//        IList<HistoricResourceOverviewDTO> GetHistoricOverviewByPidUri(string pidUriString);

//        /// <summary>
//        /// Determine a single historic entry, identified by the given unique id.
//        /// </summary>
//        /// <param name="id">the resource id to search for</param>
//        /// <returns>a single historized resource</returns>
//        Resource GetHistoricResource(string id);

//        /// <summary>
//        /// Determine a single historic entry, identified by the given unique id and pidUri.
//        /// </summary>
//        /// <param name="pidUri">the resource pidUri to search for</param>
//        /// <param name="id">the resource id to search for</param>
//        /// <returns>a single historized resource</returns>
//        Resource GetHistoricResource(string pidUri, string id);

//        /// <summary>
//        /// Based on a given information, a resource will be stored within a separate graph,
//        /// which is only responsible for historization purposes. The movement of resources to
//        /// the new graph requires some modifications on the 'future' historic resource.
//        /// </summary>
//        /// <param name="exisingResource">the existing resource to store</param>
//        /// <param name="metadata">the metadata properties to store</param>
//        void CreateHistoricResource(Resource exisingResource, IList<MetadataProperty> metadata);

//        /// <summary>
//        /// Creates all inbound links in the historic graph of a given resource, which will be added to the historic graph (historized).
//        /// If a resource gets "historized", other published resource could have links to this resource. These links need to
//        /// be added to the history graph, since they are part of the history chain of a historized resource.
//        /// </summary>
//        /// <param name="newHistoricResource">the resource which will be historized.</param>
//        void CreateInboundLinksForHistoricResource(Resource newHistoricResource);

//        /// <summary>
//        /// Deletes the inbound and outbound links of a draft resource in the historic graph, identified by the given PID URI.
//        /// </summary>
//        /// <param name="pidUri"></param>
//        void DeleteDraftResourceLinks(Uri pidUri);

//        /// <summary>
//        /// Deletes the whole resource history chain in the history graph with all inbound links,
//        /// all outbound links, and all distribution endpoints by the given PID URI.
//        /// </summary>
//        /// <param name="pidUri">the single PID URI of the complete history chain.</param>
//        void DeleteHistoricResourceChain(Uri pidUri);
//    }
//}
