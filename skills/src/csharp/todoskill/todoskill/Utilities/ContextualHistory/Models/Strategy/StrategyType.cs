using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToDoSkill.Utilities.ContextualHistory.Models.Strategy
{
    public enum ReplacementStrategy
    {
        /// <summary>
        /// First in first out.
        /// </summary>
        FIFO,

        /// <summary>
        /// Least recently used.
        /// </summary>
        LRU,

        /// <summary>
        /// Random.
        /// </summary>
        Random
    }
}
