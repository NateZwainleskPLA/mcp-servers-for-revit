using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands
{
    public class MoveElementsCommand : ExternalEventCommandBase
    {
        private static readonly object _executionLock = new object();
        private MoveElementsEventHandler _handler => (MoveElementsEventHandler)Handler;

        public override string CommandName => "move_elements";

        public MoveElementsCommand(UIApplication uiApp)
            : base(new MoveElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            lock (_executionLock)
            {
                try
                {
                    _handler.ElementIds = parameters?["elementIds"]?.ToObject<string[]>() ?? Array.Empty<string>();
                    _handler.X = parameters?["translation"]?["x"]?.Value<double>() ?? 0;
                    _handler.Y = parameters?["translation"]?["y"]?.Value<double>() ?? 0;
                    _handler.Z = parameters?["translation"]?["z"]?.Value<double>() ?? 0;
                    _handler.CopyInsteadOfMove = parameters?["copyInsteadOfMove"]?.Value<bool>() ?? false;

                    if (_handler.ElementIds.Length == 0)
                    {
                        throw new ArgumentException("elementIds is required.");
                    }

                    if (RaiseAndWaitForCompletion(15000))
                    {
                        return _handler.Result;
                    }

                    throw new TimeoutException("Move elements operation timed out.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Move elements failed: {ex.Message}", ex);
                }
            }
        }
    }
}
