namespace ToDoSkillTest.Flow.Fakes
{
    public class DataOperationType
    {
        public enum OperationType
        {
            /// <summary>
            /// Remove all the items in the list.
            /// </summary>
            KeepZeroItem,

            /// <summary>
            /// Remove all the items in the list except the first one.
            /// </summary>
            KeepOneItem,
        }
    }
}
