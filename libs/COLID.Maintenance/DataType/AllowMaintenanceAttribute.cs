using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace COLID.Maintenance.DataType
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowMaintenanceAttribute : Attribute, IFilterMetadata
    {
    }
}
