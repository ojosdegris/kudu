using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Kudu.Contracts.SiteExtensions;
using Kudu.Core;
using Kudu.Core.SiteExtensions;
using Kudu.Services.Arm;

namespace Kudu.Services.SiteExtensions
{
    public class SiteExtensionController : ApiController
    {
        private readonly ISiteExtensionManager _manager;
        private readonly IEnvironment _environment;

        public SiteExtensionController(ISiteExtensionManager manager, IEnvironment environment)
        {
            _manager = manager;
            _environment = environment;
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
            SiteExtensionInfo extension = null;
            HttpResponseMessage responseMessage = null;
            if (ArmUtils.IsArmRequest(Request))
            {
                SiteExtensionArmSettings armSettings = SiteExtensionArmSettings.GetSettings(_environment.SiteExtensionSettingsPath, id);
                if (!armSettings.IsTerminalStatus() && !string.IsNullOrWhiteSpace(armSettings.Operation))
                {
                    // some operation is ongoing, return status from setting directly
                    extension = new SiteExtensionInfo { Id = id };
                    armSettings.FillSiteExtensionInfo(extension);
                    responseMessage = Request.CreateResponse(armSettings.Status, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                }
                else if (armSettings.IsTerminalStatus() && !string.IsNullOrWhiteSpace(armSettings.Operation))
                {
                    // operation just finished

                    if (string.Equals(Constants.SiteExtensionOperationUninstall, armSettings.Operation, StringComparison.OrdinalIgnoreCase))
                    {
                        extension = new SiteExtensionInfo { Id = id };
                        armSettings.FillSiteExtensionInfo(extension);

                        if (string.Equals(Constants.SiteExtensionProvisioningStateSucceeded, armSettings.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                        {
                            // extension is goine, remove setting file
                            armSettings.RemoveArmSettings();
                        }

                        responseMessage = Request.CreateResponse(armSettings.Status, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                    }
                    else if (string.Equals(Constants.SiteExtensionOperationInstall, armSettings.Operation, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(Constants.SiteExtensionProvisioningStateSucceeded, armSettings.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                        {
                            extension = await _manager.GetLocalExtension(id, checkLatest);

                            if (extension == null)
                            {
                                // should NOT happen
                                extension = new SiteExtensionInfo { Id = id };
                                responseMessage = Request.CreateResponse(HttpStatusCode.NotFound, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                            }
                            else
                            {
                                responseMessage = Request.CreateResponse(armSettings.Status, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                                // Notify GEO to restart website
                                responseMessage.Headers.Add(Constants.SiteOperationHeaderKey, Constants.SiteOperationRestart);
                            }

                            // clear operation, since opeation is done
                            armSettings.Operation = null;
                            armSettings.SaveArmSettings();
                        }
                        else
                        {
                            extension = new SiteExtensionInfo { Id = id };
                            armSettings.FillSiteExtensionInfo(extension);
                            responseMessage = Request.CreateResponse(armSettings.Status, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                        }
                    }
                }

                // normal GET request
                if (responseMessage == null)
                {
                    extension = await _manager.GetLocalExtension(id, checkLatest);
                    if (extension == null)
                    {
                        extension = new SiteExtensionInfo { Id = id };
                        responseMessage = Request.CreateResponse(HttpStatusCode.NotFound, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                    }
                    else
                    {
                        armSettings.FillSiteExtensionInfo(extension);
                        responseMessage = Request.CreateResponse(HttpStatusCode.OK, ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(extension, Request));
                    }
                }
            }
            else
            {
                extension = await _manager.GetLocalExtension(id, checkLatest);

                if (extension == null)
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, id));
                }

                responseMessage = Request.CreateResponse(HttpStatusCode.OK, extension);
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
                // trigger installation, but do not wait. Expecting poll for status.
#pragma warning disable 4014
                _manager.InstallExtension(id, requestInfo.Version, requestInfo.FeedUrl);
#pragma warning restore 4014

                ArmEntry<SiteExtensionInfo> entry = (ArmEntry<SiteExtensionInfo>)ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(result, Request);
                HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Created, entry);
                return responseMessage;
            }
            else
            {
                result = await _manager.InstallExtension(id, requestInfo.Version, requestInfo.FeedUrl);

                if (string.Equals(Constants.SiteExtensionProvisioningStateFailed, result.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                {
                    SiteExtensionArmSettings armSettings = SiteExtensionArmSettings.GetSettings(_environment.SiteExtensionSettingsPath, id);
                    throw new HttpResponseException(Request.CreateErrorResponse(armSettings.Status, result.Comment));
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
        }

        [HttpDelete]
        public async Task<HttpResponseMessage> UninstallExtension(string id)
        {
            SiteExtensionInfo result = null;
            if (ArmUtils.IsArmRequest(Request))
            {
                result = await _manager.InitUninstallSiteExtension(id);
                // trigger uninstallation, but do not wait. Expecting poll for status.
#pragma warning disable 4014
                _manager.UninstallExtension(id);
#pragma warning restore 4014

                ArmEntry<SiteExtensionInfo> entry = (ArmEntry<SiteExtensionInfo>)ArmUtils.AddEnvelopeOnArmRequest<SiteExtensionInfo>(result, Request);
                HttpResponseMessage responseMessage = Request.CreateResponse(HttpStatusCode.Accepted, entry);
                return responseMessage;
            }
            else
            {
                result = await _manager.UninstallExtension(id);
                if (string.Equals(Constants.SiteExtensionProvisioningStateFailed, result.ProvisioningState, StringComparison.OrdinalIgnoreCase))
                {
                    SiteExtensionArmSettings armSettings = SiteExtensionArmSettings.GetSettings(_environment.SiteExtensionSettingsPath, id);
                    throw new HttpResponseException(Request.CreateErrorResponse(armSettings.Status, result.Comment));
                }

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    string.Equals(Constants.SiteExtensionProvisioningStateSucceeded, result.ProvisioningState, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
