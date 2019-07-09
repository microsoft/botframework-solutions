using System.Reflection;
using ToDoSkill.Models;

namespace ToDoSkill.Utilities
{
    public class ToDoCommonUtil
    {
        public const int DefaultDisplaySize = 4;

        // Create a shallow copy. Maybe a deep copy in the future.
        public static ToDoSkillState CloneToDoSkillStatus(ToDoSkillState oldState)
        {
            return oldState.Clone();
        }
    }
}
