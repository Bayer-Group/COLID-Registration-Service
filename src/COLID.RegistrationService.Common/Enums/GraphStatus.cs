using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace COLID.RegistrationService.Common.Enums.Graph
{
    /// <summary>
    /// Describes the status of the named graph in the database
    /// </summary>
    public enum GraphStatus
    {
        /// <summary>
        /// Active means that the graph is referenced in the current metadata graph config 
        /// and is actively used by the system to create new entries. 
        /// </summary>
        Active = 1,

        /// <summary>
        /// Historic means that the graph is referenced in the historized metadata graph config 
        /// and is therefore only used by the system to display historized entries. 
        /// </summary>
        Historic = 2,

        /// <summary>
        /// Unreferenced means that the graph is not used by the system or referenced in any form. 
        /// It can be a new or old graph. 
        /// </summary>
        Unreferenced = 3
    }
}
