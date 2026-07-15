using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Expense;

public static class ExpenseSeeder
{
    public static readonly Guid DailySubsistenceId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid TravelId = Guid.Parse("22222222-2222-2222-2222-222222222202");
    public static readonly Guid AccommodationId = Guid.Parse("22222222-2222-2222-2222-222222222203");
    public static readonly Guid FuelId = Guid.Parse("22222222-2222-2222-2222-222222222204");
    public static readonly Guid PettyCashId = Guid.Parse("22222222-2222-2222-2222-222222222205");
    public static readonly Guid OtherId = Guid.Parse("22222222-2222-2222-2222-222222222206");

    private static readonly (Guid Id, string Name, string Code, string Description, int SortOrder)[] DefaultCategories =
    [
        (DailySubsistenceId, "Daily subsistence", "DAILY_SUBS", "Meals and incidentals while travelling on business", 1),
        (TravelId, "Travel", "TRAVEL", "Flights, rail, taxis, and other transport", 2),
        (AccommodationId, "Accommodation", "ACCOMMODATION", "Hotels and lodging for business travel", 3),
        (FuelId, "Fuel", "FUEL", "Vehicle fuel for business travel", 4),
        (PettyCashId, "Petty cash", "PETTY_CASH", "Small cash purchases for business purposes", 5),
        (OtherId, "Other", "OTHER", "Other business-related expenses", 6),
    ];

    public static async Task SeedAsync(ExpenseDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.ExpenseCategories.AnyAsync(cancellationToken))
        {
            foreach (var category in DefaultCategories)
            {
                dbContext.ExpenseCategories.Add(new ExpenseCategory
                {
                    Id = category.Id,
                    Name = category.Name,
                    Code = category.Code,
                    Description = category.Description,
                    IsActive = true,
                    SortOrder = category.SortOrder,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var claimsWithoutCategory = await dbContext.ExpenseClaims
            .Where(claim => claim.CategoryId == Guid.Empty)
            .ToListAsync(cancellationToken);

        if (claimsWithoutCategory.Count > 0)
        {
            foreach (var claim in claimsWithoutCategory)
            {
                claim.CategoryId = OtherId;
                if (claim.ExpenseDate == default)
                {
                    claim.ExpenseDate = DateOnly.FromDateTime(claim.CreatedAt.UtcDateTime);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
