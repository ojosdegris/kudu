using System.IO;
using Kudu.Core.Settings;

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
        private SiteExtensionArmSettings _cache;

        public string ProvisioningState
        {
            get
            {
                EnsureCache();
                return _cache.GetValue(_provisioningStateStatusSetting);
            }

            set
            {
                EnsureCache();
                _cache.SetValue(_provisioningStateStatusSetting, value);
            }
        }

        public string Comment
        {
            get
            {
                EnsureCache();
                return _cache.GetValue(_commentMessageStatusSetting);
            }

            set
            {
                EnsureCache();
                _cache.SetValue(_commentMessageStatusSetting, value);
            }
        }

        private SiteExtensionArmSettings(string path)
            : base(path)
        {
            _filePath = path;
        }

        public static SiteExtensionArmSettings CreateSettingInstance(string rootPath, string id)
        {
            var settings = new SiteExtensionArmSettings(GetFilePath(rootPath, id));
            settings.SetValue(_provisioningStateStatusSetting, Constants.SiteExtensionProvisioningStateCreated);
            return settings;
        }

        public static SiteExtensionArmSettings GetSettings(string rootPath, string id)
        {
            return new SiteExtensionArmSettings(GetFilePath(rootPath, id));
        }

        public void RefreshCache()
        {
            _cache = new SiteExtensionArmSettings(_filePath);
        }

        private void EnsureCache()
        {
            if (_cache == null)
            {
                _cache = new SiteExtensionArmSettings(_filePath);
            }
        }

        private static string GetFilePath(string rootPath, string id)
        {
            return Path.Combine(rootPath, id, _statusSettingsFileName);
        }
    }
}
