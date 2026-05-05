using System.Collections.Generic;

namespace AutoRepair.CFGAnalyzer;

/// <summary>
/// Identifies built-in call resolvers that can be passed to <see cref="CallGraphConfig.DisabledResolvers"/>.
/// </summary>
public enum CallResolver
{
    Static,
    InterfaceImplementation,
    RapidTypeAnalysis,
    IntraproceduralDelegate,
    OperatorOverload,
    IntraproceduralFunctionPointer
}

public class CallGraphConfig
{
    public bool ExcludeSystemLibraryTargetsExceptConsole { get; set; }

    /// <summary>
    /// Set of <see cref="CallResolver"/> values identifying resolvers to disable.
    /// </summary>
    public ISet<CallResolver> DisabledResolvers { get; set; } = new HashSet<CallResolver>();
}
