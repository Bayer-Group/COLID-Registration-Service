using System;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all resource linking related operations.
    /// </summary>
    public interface IResourceLinkingService
    {
        /// <summary>
        /// Links a resource to a list of other resources, determined by the two given pid uris.
        /// </summary>
        /// <param name="pidUri">base resource to use</param>
        /// <param name="pidUriToLink">resources to attach to</param>
        /// <returns></returns>
        /// <exception cref="BusinessException">in case of missing version or existing later (newer) version</exception>
        string LinkResourceIntoList(Uri pidUri, Uri pidUriToLink);

        /// <summary>
        /// Unlinking a resource from the version chain.
        /// </summary>
        /// <param name="pidUri">Pid uri of resource, which should be be removed</param>
        /// <param name="deletingProcess">Indicates whether the unlinking takes place in a deletion process, so that different rules apply or not</param>
        /// <param name="message">returns the message of the operation</param>
        /// <returns>Returns a boolean depending on whether the resource was successfully unliked or not</returns>
        bool UnlinkResourceFromList(Uri pidUri, bool deletingProcess, out string message);
    }
}
