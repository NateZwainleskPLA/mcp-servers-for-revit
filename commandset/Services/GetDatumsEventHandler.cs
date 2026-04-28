using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class GetDatumsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public string Name { get; set; }
        public string DatumType { get; set; } = "all";
        public bool ActiveViewOnly { get; set; }
        public List<Dictionary<string, object>> ResultDatums { get; private set; } = new List<Dictionary<string, object>>();
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
                var results = new List<Dictionary<string, object>>();
                var includeAll = string.Equals(DatumType, "all", StringComparison.OrdinalIgnoreCase);

                if (includeAll || string.Equals(DatumType, "grid", StringComparison.OrdinalIgnoreCase))
                {
                    results.AddRange(Collect<Grid>(doc, view).Select(InspectGrid));
                }

                if (includeAll || string.Equals(DatumType, "level", StringComparison.OrdinalIgnoreCase))
                {
                    results.AddRange(Collect<Level>(doc, view).Select(InspectLevel));
                }

                if (includeAll || string.Equals(DatumType, "referencePlane", StringComparison.OrdinalIgnoreCase))
                {
                    results.AddRange(Collect<ReferencePlane>(doc, view).Select(InspectReferencePlane));
                }

                if (!string.IsNullOrWhiteSpace(Name))
                {
                    results = results
                        .Where(d => string.Equals(Convert.ToString(d["name"]), Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                ResultDatums = results.OrderBy(d => Convert.ToString(d["datumType"])).ThenBy(d => Convert.ToString(d["name"])).ToList();
            }
            catch
            {
                ResultDatums = new List<Dictionary<string, object>>();
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "Get Datums";
        }

        private IEnumerable<T> Collect<T>(Document doc, View view) where T : Element
        {
            var collector = ActiveViewOnly
                ? new FilteredElementCollector(doc, view.Id)
                : new FilteredElementCollector(doc);

            return collector.OfClass(typeof(T)).Cast<T>();
        }

        private static Dictionary<string, object> InspectGrid(Grid grid)
        {
            var info = BaseDatum(grid, "grid");
            info["curve"] = InspectCurve(grid.Curve);
            return info;
        }

        private static Dictionary<string, object> InspectLevel(Level level)
        {
            var info = BaseDatum(level, "level");
            info["elevation"] = level.Elevation;
            info["elevationMm"] = level.Elevation * 304.8;
            return info;
        }

        private static Dictionary<string, object> InspectReferencePlane(ReferencePlane plane)
        {
            var info = BaseDatum(plane, "referencePlane");
            info["bubbleEnd"] = Point(plane.BubbleEnd);
            info["freeEnd"] = Point(plane.FreeEnd);
            info["direction"] = Point(plane.Direction);
            return info;
        }

        private static Dictionary<string, object> BaseDatum(Element element, string datumType)
        {
            return new Dictionary<string, object>
            {
                ["id"] = ElementIdValue(element.Id),
                ["uniqueId"] = element.UniqueId,
                ["name"] = element.Name,
                ["datumType"] = datumType
            };
        }

        private static Dictionary<string, object> InspectCurve(Curve curve)
        {
            var result = new Dictionary<string, object>
            {
                ["curveType"] = curve.GetType().Name,
                ["isBound"] = curve.IsBound
            };

            if (curve.IsBound)
            {
                result["start"] = Point(curve.GetEndPoint(0));
                result["end"] = Point(curve.GetEndPoint(1));
            }

            if (curve is Line line)
            {
                result["direction"] = Point(line.Direction);
            }
            else if (curve is Arc arc)
            {
                result["center"] = Point(arc.Center);
                result["radius"] = arc.Radius;
                result["normal"] = Point(arc.Normal);
            }

            return result;
        }

        private static Dictionary<string, double> Point(XYZ point)
        {
            return new Dictionary<string, double>
            {
                ["x"] = point.X,
                ["y"] = point.Y,
                ["z"] = point.Z
            };
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
