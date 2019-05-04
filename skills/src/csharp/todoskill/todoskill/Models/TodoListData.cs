using Microsoft.Bot.Builder.Solutions.Responses;

namespace ToDoSkill.Models
{
    public class TodoListData : ICardData
    {
        public string Title { get; set; }

        public string TotalNumber { get; set; }
    }
}