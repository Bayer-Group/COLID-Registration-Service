using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.RegistrationService.Common.DataModels.Resources
{
    public class DisplayTableAndColumn
    {
        public List<TableFiled> tables { get; set; }
        public List<Filed> columns { get; set; }

        
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
        public List<Filed> subColumns { get; set; }

    }

    public class TableFiled : Filed
    {
        public List<Filed> linkedTableFiled { get; set; }
        
        public TableFiled()
        {
            linkedTableFiled = new List<Filed>();
        }
    }
}
