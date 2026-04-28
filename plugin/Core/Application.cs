using System;
using Autodesk.Revit.UI;
using revit_mcp_plugin.Localization;
using System.Reflection;
using System.Windows.Media.Imaging;



namespace revit_mcp_plugin.Core
{
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            RibbonPanel mcpPanel = application.CreateRibbonPanel(PluginStrings.RibbonPanelTitle);

            PushButtonData pushButtonData = new PushButtonData("ID_EXCMD_TOGGLE_REVIT_MCP", PluginStrings.RibbonSwitchButtonText,
                Assembly.GetExecutingAssembly().Location, "revit_mcp_plugin.Core.MCPServiceConnection");
            pushButtonData.ToolTip = PluginStrings.RibbonSwitchButtonToolTip;
            pushButtonData.Image = new BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Ressources/icon-16.png", UriKind.RelativeOrAbsolute));
            pushButtonData.LargeImage = new BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Ressources/icon-32.png", UriKind.RelativeOrAbsolute));
            mcpPanel.AddItem(pushButtonData);

            PushButtonData mcp_settings_pushButtonData = new PushButtonData("ID_EXCMD_MCP_SETTINGS", PluginStrings.RibbonSettingsButtonText,
                Assembly.GetExecutingAssembly().Location, "revit_mcp_plugin.Core.Settings");
            mcp_settings_pushButtonData.ToolTip = PluginStrings.RibbonSettingsButtonToolTip;
            mcp_settings_pushButtonData.Image = new BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Ressources/settings-16.png", UriKind.RelativeOrAbsolute));
            mcp_settings_pushButtonData.LargeImage = new BitmapImage(new Uri("/RevitMCPPlugin;component/Core/Ressources/settings-32.png", UriKind.RelativeOrAbsolute));
            mcpPanel.AddItem(mcp_settings_pushButtonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                if (SocketService.Instance.IsRunning)
                {
                    SocketService.Instance.Stop();
                }
            }
            catch { }

            return Result.Succeeded;
        }
    }
}
