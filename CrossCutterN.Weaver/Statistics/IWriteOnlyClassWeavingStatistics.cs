﻿/**
* Description: write only class weaving statistics
* Author: David Cui
*/

namespace CrossCutterN.Weaver.Statistics
{
    using Advice.Common;

    internal interface IWriteOnlyClassWeavingStatistics : ICanConvertToReadOnly<IClassWeavingStatistics>
    {
        void AddMethodWeavingStatistics(IMethodWeavingStatistics statistics);
        void AddPropertyWeavingStatistics(IPropertyWeavingStatistics statistics);
    }
}