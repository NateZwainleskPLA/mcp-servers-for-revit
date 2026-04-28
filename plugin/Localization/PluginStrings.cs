using System.Globalization;
using System.Resources;

namespace revit_mcp_plugin.Localization
{
    public static class PluginStrings
    {
        private static readonly ResourceManager ResourceManager =
            new ResourceManager("revit_mcp_plugin.Resources.Strings", typeof(PluginStrings).Assembly);

        public static string Get(string name)
        {
            return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
        }

        public static string SettingsWindowTitle => Get(nameof(SettingsWindowTitle));
        public static string RibbonPanelTitle => Get(nameof(RibbonPanelTitle));
        public static string RibbonSwitchButtonText => Get(nameof(RibbonSwitchButtonText));
        public static string RibbonSwitchButtonToolTip => Get(nameof(RibbonSwitchButtonToolTip));
        public static string RibbonSettingsButtonText => Get(nameof(RibbonSettingsButtonText));
        public static string RibbonSettingsButtonToolTip => Get(nameof(RibbonSettingsButtonToolTip));
        public static string CommandSetNavItem => Get(nameof(CommandSetNavItem));
        public static string CommandSetSettingsTitle => Get(nameof(CommandSetSettingsTitle));
        public static string CommandSetSettingsSubtitle => Get(nameof(CommandSetSettingsSubtitle));
        public static string AvailableCommandSetsHeader => Get(nameof(AvailableCommandSetsHeader));
        public static string FeatureListHeader => Get(nameof(FeatureListHeader));
        public static string CommandListHeader => Get(nameof(CommandListHeader));
        public static string CommandListForCommandSet => Get(nameof(CommandListForCommandSet));
        public static string EnableColumnHeader => Get(nameof(EnableColumnHeader));
        public static string NameColumnHeader => Get(nameof(NameColumnHeader));
        public static string DescriptionColumnHeader => Get(nameof(DescriptionColumnHeader));
        public static string NoSelectionMessage => Get(nameof(NoSelectionMessage));
        public static string OpenCommandSetFolderButton => Get(nameof(OpenCommandSetFolderButton));
        public static string RefreshButton => Get(nameof(RefreshButton));
        public static string SelectAllButton => Get(nameof(SelectAllButton));
        public static string DeselectAllButton => Get(nameof(DeselectAllButton));
        public static string SaveButton => Get(nameof(SaveButton));
        public static string NoCommandSetsFoundTitle => Get(nameof(NoCommandSetsFoundTitle));
        public static string NoCommandSetsFoundMessage => Get(nameof(NoCommandSetsFoundMessage));
        public static string ErrorTitle => Get(nameof(ErrorTitle));
        public static string ErrorLoadingCommandSets => Get(nameof(ErrorLoadingCommandSets));
        public static string SettingsSavedTitle => Get(nameof(SettingsSavedTitle));
        public static string SettingsSavedMessage => Get(nameof(SettingsSavedMessage));
        public static string ErrorSavingSettings => Get(nameof(ErrorSavingSettings));
        public static string ErrorOpeningCommandsFolder => Get(nameof(ErrorOpeningCommandsFolder));
        public static string Unspecified => Get(nameof(Unspecified));
        public static string Unknown => Get(nameof(Unknown));
    }
}
