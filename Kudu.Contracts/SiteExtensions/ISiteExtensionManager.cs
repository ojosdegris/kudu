using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kudu.Contracts.SiteExtensions
{
    public interface ISiteExtensionManager
    {
        Task<IEnumerable<SiteExtensionInfo>> GetRemoteExtensions(string filter, bool allowPrereleaseVersions, string feedUrl);

        Task<SiteExtensionInfo> GetRemoteExtension(string id, string version, string feedUrl);

        Task<IEnumerable<SiteExtensionInfo>> GetLocalExtensions(string filter, bool checkLatest);

        Task<SiteExtensionInfo> GetLocalExtension(string id, bool checkLatest);

        /// <summary>
        /// Install or update a site extension
        /// </summary>
        Task<SiteExtensionInfo> InstallExtension(string id, string version, string feedUrl);

        Task<bool> UninstallExtension(string id);

        /// <summary>
        /// Before installing a site extension, create setting file for async operation.
        /// </summary>
        Task<SiteExtensionInfo> InitInstallSiteExtension(string id);
    }
}
