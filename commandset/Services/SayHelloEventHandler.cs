using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class SayHelloEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string Message { get; set; } = "Hello MCP!";

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            // Signal completion before showing the modal dialog so the MCP caller
            // does not time out waiting for the user to dismiss it.
            _resetEvent.Set();
            TaskDialog.Show("Revit MCP", Message);
        }

        public string GetName()
        {
            return "Say Hello";
        }
    }
}
