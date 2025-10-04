using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApi;
using WebApi.Models;

namespace MixinApi
{
    public class WebInformation : IWebInformation
    {
        public string Name => "MixinApi";

        public string DisplayName => "افزونه میکسین";

        public string Description => "This is a dll that stores all Mixin api";

        public string ExecutablePath => AppDomain.CurrentDomain.BaseDirectory;

        public string Version => "1.0.2";
        public string IconPath => string.Empty;

        public List<WebConfig> Configurations =>
            new List<WebConfig>
            {
                new WebConfig
                {
                    Key = "MixinApiKey",
                    DefaultValue = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                    DisplayName = "Mixin Api Key",
                    Description = "This is the key for the Mixin API",
                    IsRequired = true,
                    IsProtected = true,
                    IsReadOnly = false
                }
            };
    }
}
