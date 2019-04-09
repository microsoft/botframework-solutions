using Microsoft.Bot.Builder.Solutions.Shared.Responses;

namespace ToDoSkill.Models
{
    public class TodoItemData : ICardData
    {
        public string CheckIconUrl { get; set; }

        public string Topic { get; set; }
    }
}