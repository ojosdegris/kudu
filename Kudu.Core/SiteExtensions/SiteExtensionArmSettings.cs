using System.IO;
using Kudu.Contracts.SiteExtensions;
using Kudu.Core.Infrastructure;
using Kudu.Core.Settings;
using Newtonsoft.Json.Linq;

namespace Kudu.Core.SiteExtensions
{
    /// <summary>
    /// TODO: locking when read/write settings
    /// </summary>
    internal class SiteExtensionArmSettings : JsonSettings
    {
        private const string _statusSettingsFileName = "SiteExtensionSetting.json";
        private const string _provisioningStateStatusSetting = "provisioningState";
        private const string _commentMessageStatusSetting = "comment";

        private string _filePath;
        private JObject _cache;

        public string ProvisioningState
        {
            get
            {
                return _cache.Value<string>(_provisioningStateStatusSetting);
            }

            set
            {
                _cache[_provisioningStateStatusSetting] = value;
            }
        }

        public string Comment
        {
            get
            {
                return _cache.Value<string>(_commentMessageStatusSetting);
            }

            set
            {
                _cache[_commentMessageStatusSetting] = value;
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
