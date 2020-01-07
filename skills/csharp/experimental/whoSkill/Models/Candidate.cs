using Microsoft.Graph;

namespace WhoSkill.Models
{
    public class Candidate
    {
        public Candidate()
        {
        }

        public Candidate(User user)
        {
            UserType = user.UserType;
            DisplayName = user.DisplayName;
            Mail = user.Mail;
            JobTitle = user.JobTitle;
            UserPrincipalName = user.UserPrincipalName;
            Id = user.Id;
            OfficeLocation = user.OfficeLocation;
            MobilePhone = user.MobilePhone;
            Department = user.Department;
        }

        public string UserType { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Mail { get; set; } = string.Empty;

        public string JobTitle { get; set; } = "Mock JobTitle";

        public string UserPrincipalName { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string OfficeLocation { get; set; } = "Mock OfficeLocation";

        public string MobilePhone { get; set; } = "Mock MobilePhone";

        public string Department { get; set; } = "Mock Department";
    }
}
