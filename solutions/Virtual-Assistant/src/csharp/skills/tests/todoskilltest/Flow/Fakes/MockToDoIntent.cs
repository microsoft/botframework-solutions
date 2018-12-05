using Luis;
using System;

namespace ToDoSkillTest.Flow.Fakes
{
    public class MockToDoIntent : ToDo
    {
        private string userInput;
        private Intent intent;
        private double score;

        public MockToDoIntent(string userInput)
        {
            this.Entities = new ToDo._Entities();
            this.userInput = userInput;

            this.intent = ToDo.Intent.None;
            this.score = 0;

            (intent, score) = LuisResultMock();
        }

        public override _Entities Entities { get; set; }

        public override (Intent intent, double score) TopIntent()
        {
            return (intent, score);
        }

        private (Intent intent, double score) LuisResultMock()
        {
            if (userInput != null)
            {
                if (userInput.Contains("first", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.ordinal = new double[] { 1 };
                }
                else if (userInput.Contains("second", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.ordinal = new double[] { 2 };
                }

                if (userInput.Contains("one", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.number = new double[] { 1 };
                }
                else if (userInput.Contains("two", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.number = new double[] { 2 };
                }
                else if (userInput.Contains("three", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.number = new double[] { 3 };
                }

                if (userInput.Contains("all", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.ContainsAll = new string[] { "all" };
                }

                if (userInput.Contains("shopping", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.ListType = new string[] { "shopping" };
                }
                else if (userInput.Contains("grocery", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Entities.ListType = new string[] { "grocery" };
                }

                if (userInput.ToLower().Contains("add", StringComparison.InvariantCultureIgnoreCase))
                {
                    return (ToDo.Intent.AddToDo, 0.90);
                }
                else if (userInput.ToLower().Contains("delete", StringComparison.InvariantCultureIgnoreCase)
                    || userInput.ToLower().Contains("remove", StringComparison.InvariantCultureIgnoreCase))
                {
                    return (ToDo.Intent.DeleteToDo, 0.90);
                }
                else if (userInput.ToLower().Contains("mark", StringComparison.InvariantCultureIgnoreCase))
                {
                    return (ToDo.Intent.MarkToDo, 0.90);
                }
                else if (userInput.ToLower().Contains("show", StringComparison.InvariantCultureIgnoreCase))
                {
                    return (ToDo.Intent.ShowToDo, 0.90);
                }
            }

            return (ToDo.Intent.None, 0.0);
        }
    }
}