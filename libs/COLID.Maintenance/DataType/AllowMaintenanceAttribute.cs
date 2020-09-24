using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace COLID.Maintenance.DataType
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllowMaintenanceAttribute : Attribute, IFilterMetadata
    {
    }
}
