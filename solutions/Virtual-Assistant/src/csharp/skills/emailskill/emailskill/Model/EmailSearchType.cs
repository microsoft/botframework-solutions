using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailSkill.Model
{
    /// <summary>
    /// Source of event.
    /// </summary>
    public enum EmailSearchType
    {
        /// <summary>
        /// None.
        /// </summary>
        None,

        /// <summary>
        /// Search By Contact.
        /// </summary>
        SearchByContact,

        /// <summary>
        /// Search By Subject.
        /// </summary>
        SearchBySubject
    }
}
