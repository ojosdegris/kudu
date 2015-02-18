using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Kudu.Contracts.SiteExtensions;
using Kudu.Services.Arm;

namespace Kudu.Services.SiteExtensions
{
    public class SiteExtensionController : ApiController
    {
        private readonly ISiteExtensionManager _manager;

        public SiteExtensionController(ISiteExtensionManager manager)
        {
            _manager = manager;
        }

        [HttpGet]
        public async Task<IEnumerable<SiteExtensionInfo>> GetRemoteExtensions(string filter = null, bool allowPrereleaseVersions = false, string feedUrl = null)
        {
            return await _manager.GetRemoteExtensions(filter, allowPrereleaseVersions, feedUrl);
        }

        [HttpGet]
        public async Task<SiteExtensionInfo> GetRemoteExtension(string id, string version = null, string feedUrl = null)
        {
            SiteExtensionInfo extension = await _manager.GetRemoteExtension(id, version, feedUrl);

            if (extension == null)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, id));
            }

            return extension;
        }

        [HttpGet]
        public async Task<IEnumerable<SiteExtensionInfo>> GetLocalExtensions(string filter = null, bool checkLatest = true)
        {
            return await _manager.GetLocalExtensions(filter, checkLatest);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetLocalExtension(string id, bool checkLatest = true)
        {
            SiteExtensionInfo extension = await _manager.GetLocalExtension(id, checkLatest);
            HttpResponseMessage responseMessage = null;
            if (extension != null)
            {
                responseMessage = Request.CreateResponse(HttpStatusCode.OK, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                if (ArmUtils.IsArmRequest(Request)
                    && string.Equals(Constants.SiteExtensionProvisioningStateSucceeded, extension.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                {
                    // Notify GEO to restart website
                    responseMessage.Headers.Add("X-MS-SITE-OPERATION", Constants.SiteOperationRestart);
                }
            }
            else
            {
                extension = new SiteExtensionInfo();
                extension.Id = id;
                responseMessage = Request.CreateResponse(HttpStatusCode.NotFound, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
            }

            return responseMessage;
        }

        [HttpPut]
        public async Task<HttpResponseMessage> InstallExtension(string id, SiteExtensionInfo requestInfo)
        {
            if (requestInfo == null)
            {
                requestInfo = new SiteExtensionInfo();
            }

            SiteExtensionInfo result = await _manager.InitInstallSiteExtension(id);

            if (ArmUtils.IsArmRequest(Request))
            {
                // trigger installation, but do not wait. Poll for status.
#pragma warning disable 4014
                _manager.InstallExtension(id, requestInfo.Version, requestInfo.FeedUrl);
#pragma warning restore 4014

                return Request.CreateResponse(HttpStatusCode.Created, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(result, Request));
            }
            else
            {
                result = await _manager.InstallExtension(id, requestInfo.Version, requestInfo.FeedUrl);

                if (string.Equals(Constants.SiteExtensionProvisioningStateFailed, result.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(result.Id))
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, result.Comment));
                    }
                    else
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.Comment));
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> UninstallExtension(string id)
        {
            SiteExtensionInfo result = await _manager.UninstallExtension(id);

            if (ArmUtils.IsArmRequest(Request))
            {
                return Request.CreateResponse(HttpStatusCode.Accepted, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(result, Request));
            }
            else
            {
                if (string.Equals(Constants.SiteExtensionProvisioningStateFailed, result.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, result.Comment));
                }

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    string.Equals(Constants.SiteExtensionProvisioningStateSucceeded, result.ProvisioningState, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
