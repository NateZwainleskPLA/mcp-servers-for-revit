using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class InspectElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public string[] ElementIds { get; set; } = Array.Empty<string>();
        public bool UseSelection { get; set; }
        public int? Limit { get; set; }
        public bool IncludeGeometry { get; set; }
        public bool IncludeBoundingBox { get; set; }
        public string[] ParameterNames { get; set; } = Array.Empty<string>();
        public List<Dictionary<string, object>> ResultElements { get; private set; } = new List<Dictionary<string, object>>();
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
                var uiDoc = app.ActiveUIDocument;
                var doc = uiDoc.Document;
                var view = doc.ActiveView;
                var ids = ResolveElementIds(uiDoc, doc);

                if (Limit.HasValue && Limit.Value > 0)
                {
                    ids = ids.Take(Limit.Value).ToList();
                }

                ResultElements = ids
                    .Select(id => doc.GetElement(id))
                    .Where(e => e != null)
                    .Select(e => InspectElement(e, view))
                    .ToList();
            }
            catch
            {
                ResultElements = new List<Dictionary<string, object>>();
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "Inspect Elements";
        }

        private List<ElementId> ResolveElementIds(UIDocument uiDoc, Document doc)
        {
            if (UseSelection)
            {
                return uiDoc.Selection.GetElementIds().ToList();
            }

            return ElementIds
                .Select(ParseElementId)
                .Where(id => id != null && doc.GetElement(id) != null)
                .ToList();
        }

        private Dictionary<string, object> InspectElement(Element element, View view)
        {
            var info = new Dictionary<string, object>
            {
                ["id"] = ElementIdValue(element.Id),
                ["uniqueId"] = element.UniqueId,
                ["name"] = element.Name,
                ["category"] = element.Category?.Name,
                ["className"] = element.GetType().Name
            };

            var typeId = element.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                info["typeId"] = ElementIdValue(typeId);
            }

            var location = InspectLocation(element.Location);
            if (location != null)
            {
                info["location"] = location;
            }

            if (IncludeBoundingBox)
            {
                var box = element.get_BoundingBox(view) ?? element.get_BoundingBox(null);
                if (box != null)
                {
                    info["boundingBox"] = new Dictionary<string, object>
                    {
                        ["min"] = Point(box.Min),
                        ["max"] = Point(box.Max)
                    };
                }
            }

            if (IncludeGeometry)
            {
                var geometry = InspectElementGeometry(element);
                if (geometry != null)
                {
                    info["geometry"] = geometry;
                }
            }

            if (ParameterNames.Length > 0)
            {
                info["parameters"] = ParameterNames.ToDictionary(name => name, name => ParameterValue(element.LookupParameter(name)));
            }

            return info;
        }

        private static Dictionary<string, object> InspectLocation(Location location)
        {
            if (location is LocationPoint point)
            {
                return new Dictionary<string, object>
                {
                    ["type"] = "point",
                    ["point"] = Point(point.Point),
                    ["rotation"] = point.Rotation
                };
            }

            if (location is LocationCurve curveLocation)
            {
                return new Dictionary<string, object>
                {
                    ["type"] = "curve",
                    ["curve"] = InspectCurve(curveLocation.Curve)
                };
            }

            return null;
        }

        private static Dictionary<string, object> InspectElementGeometry(Element element)
        {
            if (element is CurveElement curveElement)
            {
                return new Dictionary<string, object>
                {
                    ["curve"] = InspectCurve(curveElement.GeometryCurve)
                };
            }

            return null;
        }

        private static Dictionary<string, object> InspectCurve(Curve curve)
        {
            var result = new Dictionary<string, object>
            {
                ["curveType"] = curve.GetType().Name,
                ["isBound"] = curve.IsBound,
                ["length"] = curve.IsBound ? curve.Length : 0
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

        private static object ParameterValue(Parameter parameter)
        {
            if (parameter == null)
            {
                return null;
            }

            return parameter.StorageType switch
            {
                StorageType.Double => new Dictionary<string, object>
                {
                    ["value"] = parameter.AsDouble(),
                    ["displayValue"] = parameter.AsValueString()
                },
                StorageType.Integer => parameter.AsInteger(),
                StorageType.String => parameter.AsString(),
                StorageType.ElementId => ElementIdValue(parameter.AsElementId()),
                _ => null
            };
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
