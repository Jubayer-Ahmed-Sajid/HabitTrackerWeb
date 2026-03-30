using Microsoft.AspNetCore.Identity;

namespace HabitTrackerWeb.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public ICollection<ExternalAccountLink> ExternalAccountLinks { get; set; } = new List<ExternalAccountLink>();
    public EloRating? EloRating { get; set; }
    public ICollection<EloRatingChange> EloRatingChanges { get; set; } = new List<EloRatingChange>();
}
