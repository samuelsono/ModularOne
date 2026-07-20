using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Expense;

public class ExpenseSettingsService(ExpenseDbContext dbContext) : IExpenseSettingsService
{
    public async Task<ExpenseSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<ExpenseSettingsDto> UpdateAsync(
        UpdateExpenseSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.KilometerRate <= 0)
        {
            throw new ArgumentException("Kilometer rate must be greater than zero.", nameof(request));
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.KilometerRate = decimal.Round(request.KilometerRate, 2);
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private async Task<ExpenseSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ExpenseSettings
            .FirstOrDefaultAsync(item => item.Id == ExpenseSettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new ExpenseSettings
        {
            Id = ExpenseSettings.SingletonId,
            KilometerRate = 0m,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.ExpenseSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }

    private static ExpenseSettingsDto ToDto(ExpenseSettings settings) =>
        new(settings.KilometerRate, settings.UpdatedAt.ToString("O"));
}