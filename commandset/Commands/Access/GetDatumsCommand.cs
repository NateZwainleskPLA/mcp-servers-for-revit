using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetDatumsCommand : ExternalEventCommandBase
    {
        private static readonly object _executionLock = new object();
        private GetDatumsEventHandler _handler => (GetDatumsEventHandler)Handler;

        public override string CommandName => "get_datums";

        public GetDatumsCommand(UIApplication uiApp)
            : base(new GetDatumsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            lock (_executionLock)
            {
                try
                {
                    _handler.Name = parameters?["name"]?.Value<string>();
                    _handler.DatumType = parameters?["datumType"]?.Value<string>() ?? "all";
                    _handler.ActiveViewOnly = parameters?["activeViewOnly"]?.Value<bool>() ?? false;

                    if (RaiseAndWaitForCompletion(15000))
                    {
                        return _handler.ResultDatums;
                    }

                    throw new TimeoutException("Get datums operation timed out.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Get datums failed: {ex.Message}", ex);
                }
            }
        }
    }
}
