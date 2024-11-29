using Microsoft.AspNetCore.Identity;

namespace QuizAppApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; //Default 

        //Navigation Properties
        public ICollection<Group> AdminGroups { get; set; } = new List<Group>();
        public ICollection<GroupMember> MemberGroups { get; set; } = new List<GroupMember>();
    }
}
