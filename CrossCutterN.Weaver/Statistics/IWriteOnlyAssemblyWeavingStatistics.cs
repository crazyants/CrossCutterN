﻿/**
 * Description: write only assembly weaving statistics
 * Author: David Cui
 */

namespace CrossCutterN.Weaver.Statistics
{
    using System;
    using Advice.Common;

    internal interface IWriteOnlyAssemblyWeavingStatistics : ICanConvertToReadOnly<IAssemblyWeavingStatistics>
    {
        Exception Exception { set; }
        void AddModuleWeavingStatistics(IModuleWeavingStatistics statistics);
    }
}