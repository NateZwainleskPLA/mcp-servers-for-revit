using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class MoveElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public string[] ElementIds { get; set; } = Array.Empty<string>();
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public bool CopyInsteadOfMove { get; set; }
        public Dictionary<string, object> Result { get; private set; } = new Dictionary<string, object>();
        public bool TaskCompleted { get; private set; }

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                var ids = ElementIds
                    .Select(ParseElementId)
                    .Where(id => id != null && doc.GetElement(id) != null)
                    .ToList();

                var translation = new XYZ(X / 304.8, Y / 304.8, Z / 304.8);
                var changedIds = new List<long>();

                using (var transaction = new Transaction(doc, CopyInsteadOfMove ? "Copy Elements" : "Move Elements"))
                {
                    transaction.Start();

                    if (CopyInsteadOfMove)
                    {
                        changedIds.AddRange(ElementTransformUtils.CopyElements(doc, ids, translation).Select(ElementIdValue));
                    }
                    else
                    {
                        foreach (var id in ids)
                        {
                            ElementTransformUtils.MoveElement(doc, id, translation);
                            changedIds.Add(ElementIdValue(id));
                        }
                    }

                    transaction.Commit();
                }

                Result = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["inputCount"] = ElementIds.Length,
                    ["validCount"] = ids.Count,
                    ["copyInsteadOfMove"] = CopyInsteadOfMove,
                    ["translationMm"] = new Dictionary<string, double> { ["x"] = X, ["y"] = Y, ["z"] = Z },
                    ["elementIds"] = changedIds
                };
            }
            catch (Exception ex)
            {
                Result = new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = ex.Message,
                    ["innerError"] = ex.InnerException?.Message
                };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "Move Elements";
        }

        private static ElementId ParseElementId(string value)
        {
            return int.TryParse(value, out var intValue) ? new ElementId(intValue) : null;
        }

        private static long ElementIdValue(ElementId id)
        {
#if REVIT2024_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }
    }
}
