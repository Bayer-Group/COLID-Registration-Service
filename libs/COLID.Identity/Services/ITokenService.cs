using System.Threading.Tasks;
using COLID.Identity.Configuration;

namespace COLID.Identity.Services
{
    public interface ITokenService<TTokenServiceSettings> where TTokenServiceSettings : BaseServiceTokenOptions
    {
        /// <summary>
        /// This method retrieves the access token for the WebAPI that has previously
        /// been retrieved and cached. This method will fail if an access token for the WebAPI
        /// has not been retrieved and cached.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task{System.string}"/> with an access token as its result.</returns>
        Task<string> GetAccessTokenForWebApiAsync();
    }
}
