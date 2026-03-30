namespace HabitTrackerWeb.Data.Seed;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
