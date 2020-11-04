using System;
using System.Text.Json.Serialization;

namespace Prisma.Natives.Boot.Config
{
    [Serializable]
    internal class BootConfig
    {
        [JsonPropertyName("natives_in_appdir")]
        public bool NativesInApplicationDirectory { get; set; } = true;

        [JsonPropertyName("skip_checksum_verification")]
        public bool SkipChecksumVerification { get; set; }

        [JsonPropertyName("sdl_modules")]
        public SdlModuleConfig SdlModules { get; private set; } = new SdlModuleConfig();
    }
}