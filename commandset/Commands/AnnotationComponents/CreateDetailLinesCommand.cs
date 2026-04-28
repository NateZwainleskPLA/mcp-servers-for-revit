using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.AnnotationComponents
{
    public class CreateDetailLinesCommand : ExternalEventCommandBase
    {
        private static readonly object _executionLock = new object();
        private CreateDetailLinesEventHandler _handler => (CreateDetailLinesEventHandler)Handler;

        public override string CommandName => "create_detail_lines";

        public CreateDetailLinesCommand(UIApplication uiApp)
            : base(new CreateDetailLinesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            lock (_executionLock)
            {
                try
                {
                    _handler.Lines = parameters?["lines"]?.ToObject<List<DetailLineInput>>() ?? new List<DetailLineInput>();
                    if (_handler.Lines.Count == 0)
                    {
                        throw new ArgumentException("lines is required.");
                    }

                    if (RaiseAndWaitForCompletion(15000))
                    {
                        return _handler.Result;
                    }

                    throw new TimeoutException("Create detail lines operation timed out.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Create detail lines failed: {ex.Message}", ex);
                }
            }
        }
    }
}
