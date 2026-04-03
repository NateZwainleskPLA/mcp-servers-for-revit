using Autodesk.Revit.DB;
using System;

namespace RevitMCPCommandSet.Utils
{
    internal static class ParameterValueUtils
    {
        public static bool TryGetWritableParameterValue(Parameter parameter, out (StorageType Type, object Value) value)
        {
            value = default;

            if (parameter == null || parameter.IsReadOnly || !parameter.HasValue)
            {
                return false;
            }

            object rawValue = parameter.StorageType switch
            {
                StorageType.String => parameter.AsString(),
                StorageType.Integer => parameter.AsInteger(),
                StorageType.Double => parameter.AsDouble(),
                StorageType.ElementId => parameter.AsElementId().GetValue(),
                _ => null
            };

            if (rawValue == null)
            {
                return false;
            }

            value = (parameter.StorageType, rawValue);
            return true;
        }

        public static void SetParameterValue(Parameter parameter, StorageType type, object value)
        {
            switch (type)
            {
                case StorageType.String:
                    parameter.Set(value as string ?? "");
                    break;
                case StorageType.Integer:
                    parameter.Set((int)value);
                    break;
                case StorageType.Double:
                    parameter.Set((double)value);
                    break;
                case StorageType.ElementId:
                    parameter.Set(value is ElementId elementId ? elementId : ElementIdExtensions.FromLong(Convert.ToInt64(value)));
                    break;
            }
        }
    }
}
