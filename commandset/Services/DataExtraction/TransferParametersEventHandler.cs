using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using RevitElementIdExtensions = RevitMCPCommandSet.Utils.ElementIdExtensions;

namespace RevitMCPCommandSet.Services.DataExtraction
{
    public class TransferParametersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public long SourceElementId { get; set; }
        public List<long> TargetElementIds { get; set; } = new List<long>();
        public List<string> ParameterNames { get; set; } = new List<string>(); // empty = all writable
        public bool IncludeType { get; set; } = false;
        public bool DryRun { get; set; } = false;

        public AIResult<object> Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters() { TaskCompleted = false; _resetEvent.Reset(); }
        public bool WaitForCompletion(int timeoutMilliseconds = 30000) { _resetEvent.Reset(); return _resetEvent.WaitOne(timeoutMilliseconds); }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;

                var source = doc.GetElement(RevitElementIdExtensions.FromLong(SourceElementId));
                if (source == null)
                    throw new ArgumentException($"Source element {SourceElementId} not found");

                // Collect source parameter values
                var sourceValues = new Dictionary<string, (StorageType type, object value, bool isType)>();
                foreach (Parameter p in source.Parameters)
                {
                    if (ParameterNames.Count > 0 && !ParameterNames.Contains(p.Definition.Name)) continue;

                    if (ParameterValueUtils.TryGetWritableParameterValue(p, out var value))
                        sourceValues[p.Definition.Name] = (value.Type, value.Value, false);
                }

                // Include type parameters if requested
                if (IncludeType)
                {
                    var sourceType = doc.GetElement(source.GetTypeId());
                    if (sourceType != null)
                    {
                        foreach (Parameter p in sourceType.Parameters)
                        {
                            if (ParameterNames.Count > 0 && !ParameterNames.Contains(p.Definition.Name)) continue;
                            if (sourceValues.ContainsKey(p.Definition.Name)) continue;

                            if (ParameterValueUtils.TryGetWritableParameterValue(p, out var value))
                                sourceValues[p.Definition.Name] = (value.Type, value.Value, true);
                        }
                    }
                }

                int totalTransferred = 0;
                int targetCount = 0;
                var targetResults = new List<object>();

                using (var transaction = DryRun ? null : new Transaction(doc, "Transfer Parameters"))
                {
                    if (!DryRun) transaction.Start();

                    foreach (var targetId in TargetElementIds)
                    {
                        var target = doc.GetElement(RevitElementIdExtensions.FromLong(targetId));
                        if (target == null) continue;

                        int transferred = 0;
                        int skipped = 0;
                        var paramResults = new List<object>();

                        foreach (var kvp in sourceValues)
                        {
                            string paramName = kvp.Key;
                            var (storageType, value, isType) = kvp.Value;

                            Element targetElem = target;
                            if (isType)
                            {
                                targetElem = doc.GetElement(target.GetTypeId());
                                if (targetElem == null) { skipped++; continue; }
                            }

                            var targetParam = targetElem.LookupParameter(paramName);
                            if (targetParam == null || targetParam.IsReadOnly)
                            {
                                skipped++;
                                continue;
                            }

                            try
                            {
                                if (!DryRun)
                                    ParameterValueUtils.SetParameterValue(targetParam, storageType, value);

                                transferred++;
                                paramResults.Add(new { parameterName = paramName, success = true, value = value?.ToString() ?? "" });
                            }
                            catch
                            {
                                skipped++;
                            }
                        }

                        totalTransferred += transferred;
                        targetCount++;
                        targetResults.Add(new
                        {
                            targetId,
                            targetName = target.Name,
                            transferred,
                            skipped,
                            parameters = DryRun ? paramResults : null
                        });
                    }

                    if (!DryRun) transaction.Commit();
                }

                Result = new AIResult<object>
                {
                    Success = true,
                    Message = DryRun
                        ? $"Dry run: would transfer {sourceValues.Count} parameters to {targetCount} elements"
                        : $"Transferred {totalTransferred} parameter values to {targetCount} elements",
                    Response = new
                    {
                        sourceElementId = SourceElementId,
                        sourceParameterCount = sourceValues.Count,
                        targetCount,
                        totalTransferred,
                        dryRun = DryRun,
                        targets = targetResults
                    }
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<object> { Success = false, Message = $"Transfer parameters failed: {ex.Message}" };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName() => "Transfer Parameters";
    }
}
