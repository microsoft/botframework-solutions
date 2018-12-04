using Luis;

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
                if (userInput.Contains("first"))
                {
                    this.Entities.ordinal = new double[] { 1 };
                }
                else if (userInput.Contains("second"))
                {
                    this.Entities.ordinal = new double[] { 2 };
                }

                if (userInput.Contains("one"))
                {
                    this.Entities.number = new double[] { 1 };
                }
                else if (userInput.Contains("two"))
                {
                    this.Entities.number = new double[] { 2 };
                }
                else if (userInput.Contains("three"))
                {
                    this.Entities.number = new double[] { 3 };
                }

                if (userInput.Contains("all"))
                {
                    this.Entities.ContainsAll = new string[] { "all" };
                }

                if (userInput.Contains("shopping"))
                {
                    this.Entities.ListType = new string[] { "shopping" };
                }
                else if (userInput.Contains("grocery"))
                {
                    this.Entities.ListType = new string[] { "grocery" };
                }

                if (userInput.ToLower().Contains("add"))
                {
                    return (ToDo.Intent.AddToDo, 0.90);
                }
                else if (userInput.ToLower().Contains("delete") || userInput.ToLower().Contains("remove"))
                {
                    return (ToDo.Intent.DeleteToDo, 0.90);
                }
                else if (userInput.ToLower().Contains("mark"))
                {
                    return (ToDo.Intent.MarkToDo, 0.90);
                }
                else if (userInput.ToLower().Contains("show"))
                {
                    return (ToDo.Intent.ShowToDo, 0.90);
                }
            }

            return (ToDo.Intent.None, 0.0);
        }
    }
}