namespace HintServiceMeow.Plugin
{
    using System.Collections.Generic;
    using System.ComponentModel;
#if EXILED
    using Exiled.API.Interfaces;
#endif

#if EXILED
    internal class PluginConfig : IConfig
#else
    internal class PluginConfig
#endif
    {
        public bool IsEnabled { get; set; } = true;

        public bool Debug { get; set; } = false;

        [Description("Logs every hint pipeline decision. Enable only while diagnosing flicker or compatibility issues.")]
        public bool TraceHintPipeline { get; set; } = false;

        [Description("By using this feature, it might make plugin that is imcompatible with HintServiceMeow compatible. This is a experimental feature")]
        public bool UseHintCompatibilityAdapter { get; set; } = false;

        [Description("When a vanilla hint cannot be absorbed by the compatibility adapter, redraw HSM quickly instead of hiding HSM until the vanilla hint expires.")]
        public bool PreferHsmOverVanillaHints { get; set; } = true;

        [Description("The assembly that you do not want to included in the Compatibility Adapter. Use command GetCompatAssemblyName to get the name of all the assemblies")]
        public List<string> DisabledCompatAdapter { get; set; } =
        [
            "Some Plugin",
            "Some Other Plugin",
        ];

        [Description("The default time to show for each type of common hint. Short means that the hint has on title but not decription")]
        public int ItemHintDisplayTime { get; set; } = 10;

        public int ShortItemHintDisplayTime { get; set; } = 5;

        public int MapHintDisplayTime { get; set; } = 10;

        public int ShortMapHintDisplayTime { get; set; } = 7;

        public int RoleHintDisplayTime { get; set; } = 15;

        public int ShortRoleHintDisplayTime { get; set; } = 5;

        public int OtherHintDisplayTime { get; set; } = 5;
    }
}
