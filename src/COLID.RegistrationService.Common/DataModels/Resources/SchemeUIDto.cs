using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Resources
{
    public class DisplayTableAndColumnByPidUri
    {
        public string pidURI { get; set; }
        public DisplayTableAndColumn TableAndColumn { get; set; }
    }

    public class DisplayTableAndColumn
    {
        public IList<TableFiled> tables { get; set; }
        public IList<Filed> columns { get; set; }

        
        public DisplayTableAndColumn()
        {
            tables = new List<TableFiled>();
            columns = new List<Filed>();
        }
    }

    public class Filed
    {
        public string resourceId { get; set; }
        public string pidURI { get; set; }
        public string label { get; set; }
        public IList<Filed> subColumns { get; set; }

    }

    public class TableFiled : Filed
    {
        public IList<Filed> linkedTableFiled { get; set; }
        
        public TableFiled()
        {
            linkedTableFiled = new List<Filed>();
        }
    }
}
