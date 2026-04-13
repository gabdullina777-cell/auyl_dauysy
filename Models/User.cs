using Microsoft.AspNetCore.Identity;

namespace ayul_dayusy.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public string Village { get; set; } = "";
        public string Region { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}