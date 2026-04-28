using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access
{
    public class InspectElementsCommand : ExternalEventCommandBase
    {
        private static readonly object _executionLock = new object();
        private InspectElementsEventHandler _handler => (InspectElementsEventHandler)Handler;

        public override string CommandName => "inspect_elements";

        public InspectElementsCommand(UIApplication uiApp)
            : base(new InspectElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            lock (_executionLock)
            {
                try
                {
                    _handler.ElementIds = parameters?["elementIds"]?.ToObject<string[]>() ?? Array.Empty<string>();
                    _handler.UseSelection = parameters?["useSelection"]?.Value<bool>() ?? false;
                    _handler.Limit = parameters?["limit"]?.Value<int>();
                    _handler.IncludeGeometry = parameters?["includeGeometry"]?.Value<bool>() ?? false;
                    _handler.IncludeBoundingBox = parameters?["includeBoundingBox"]?.Value<bool>() ?? false;
                    _handler.ParameterNames = parameters?["parameterNames"]?.ToObject<string[]>() ?? Array.Empty<string>();

                    if (!_handler.UseSelection && _handler.ElementIds.Length == 0)
                    {
                        throw new ArgumentException("Provide elementIds or set useSelection to true.");
                    }

                    if (RaiseAndWaitForCompletion(15000))
                    {
                        return _handler.ResultElements;
                    }

                    throw new TimeoutException("Inspect elements operation timed out.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Inspect elements failed: {ex.Message}", ex);
                }
            }
        }
    }
}
