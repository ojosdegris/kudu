using System;
using System.IO;
using System.Net;
using Kudu.Contracts.SiteExtensions;
using Kudu.Core.Infrastructure;
using Kudu.Core.Settings;
using Newtonsoft.Json.Linq;

namespace Kudu.Core.SiteExtensions
{
    /// <summary>
    /// TODO: locking when read/write settings
    /// </summary>
    public class SiteExtensionArmSettings : JsonSettings
    {
        private const string _statusSettingsFileName = "SiteExtensionSetting.json";
        private const string _provisioningStateSetting = "provisioningState";
        private const string _commentMessageSetting = "comment";
        private const string _statusSetting = "status";
        private const string _operationSetting = "operation";

        private string _filePath;
        private JObject _cache;

        public string ProvisioningState
        {
            get
            {
                return _cache.Value<string>(_provisioningStateSetting);
            }

            set
            {
                _cache[_provisioningStateSetting] = value;
            }
        }

        public string Comment
        {
            get
            {
                return _cache.Value<string>(_commentMessageSetting);
            }

            set
            {
                _cache[_commentMessageSetting] = value;
            }
        }

        public HttpStatusCode Status
        {
            get
            {
                string statusStr = _cache.Value<string>(_statusSetting);
                HttpStatusCode statusCode = HttpStatusCode.OK;
                Enum.TryParse<HttpStatusCode>(statusStr, out statusCode);
                return statusCode;
            }

            set
            {
                _cache[_statusSetting] = Enum.GetName(typeof(HttpStatusCode), value);
            }
        }

        /// <summary>
        /// <para>Property to indicate current operation</para>
        /// <para>Empty means there is no recent operation</para>
        /// </summary>
        public string Operation
        {
            get
            {
                return _cache.Value<string>(_operationSetting);
            }

            set
            {
                _cache[_operationSetting] = value;
            }
        }

        private SiteExtensionArmSettings(string path)
            : base(path)
        {
            _filePath = path;
            _cache = base.Read();
        }

        public static SiteExtensionArmSettings CreateSettingInstance(string rootPath, string id)
        {
            var settings = new SiteExtensionArmSettings(GetFilePath(rootPath, id));
            settings.ProvisioningState = Constants.SiteExtensionProvisioningStateCreated;
            settings.Operation = Constants.SiteExtensionOperationInstall;
            settings.SaveArmSettings();
            return settings;
        }

        public static SiteExtensionArmSettings GetSettings(string rootPath, string id)
        {
            return new SiteExtensionArmSettings(GetFilePath(rootPath, id));
        }

        public void RefreshArmSettingsCache()
        {
            _cache = base.Read();
        }

        public void SaveArmSettings()
        {
            base.Save(_cache);
        }

        public void FillSiteExtensionInfo(SiteExtensionInfo info)
        {
            info.ProvisioningState = ProvisioningState;
            info.Comment = Comment;
        }

        public void ReadSiteExtensionInfo(SiteExtensionInfo info)
        {
            ProvisioningState = info.ProvisioningState;
            Comment = info.Comment;
        }

        public void RemoveArmSettings()
        {
            if (FileSystemHelpers.FileExists(_filePath))
            {
                OperationManager.Attempt(() => FileSystemHelpers.DeleteFileSafe(_filePath));
            }
        }

        private static string GetFilePath(string rootPath, string id)
        {
            return Path.Combine(rootPath, id, _statusSettingsFileName);
        }
    }
}
