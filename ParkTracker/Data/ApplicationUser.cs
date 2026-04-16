using Microsoft.AspNetCore.Identity;
using ParkTracker.Data.Models;

namespace ParkTracker.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public ICollection<Visit> Visits { get; set; } = [];
}

