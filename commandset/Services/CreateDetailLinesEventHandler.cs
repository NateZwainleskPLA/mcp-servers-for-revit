using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class CreateDetailLinesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<DetailLineInput> Lines { get; set; } = new List<DetailLineInput>();
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
                var view = doc.ActiveView;
                var created = new List<Dictionary<string, object>>();

                using (var transaction = new Transaction(doc, "Create Detail Lines"))
                {
                    transaction.Start();

                    foreach (var input in Lines)
                    {
                        var start = input.Start.ToXyz();
                        var end = input.End.ToXyz();
                        if (start.DistanceTo(end) < 1e-9)
                        {
                            continue;
                        }

                        var detailCurve = doc.Create.NewDetailCurve(view, Line.CreateBound(start, end));
                        created.Add(new Dictionary<string, object>
                        {
                            ["id"] = ElementIdValue(detailCurve.Id),
                            ["start"] = input.Start.ToDictionary(),
                            ["end"] = input.End.ToDictionary(),
                            ["lengthMm"] = start.DistanceTo(end) * 304.8
                        });
                    }

                    transaction.Commit();
                }

                Result = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["activeViewId"] = ElementIdValue(view.Id),
                    ["activeViewName"] = view.Name,
                    ["requested"] = Lines.Count,
                    ["createdCount"] = created.Count,
                    ["created"] = created
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
            return "Create Detail Lines";
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

    public class DetailLineInput
    {
        public DetailLinePoint Start { get; set; } = new DetailLinePoint();
        public DetailLinePoint End { get; set; } = new DetailLinePoint();
    }

    public class DetailLinePoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public XYZ ToXyz()
        {
            return new XYZ(X / 304.8, Y / 304.8, Z / 304.8);
        }

        public Dictionary<string, double> ToDictionary()
        {
            return new Dictionary<string, double> { ["x"] = X, ["y"] = Y, ["z"] = Z };
        }
    }
}
