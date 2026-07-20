namespace CarTrack.Modules.Expense;

public class ExpenseSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public decimal KilometerRate { get; set; } = 0m;

    public DateTimeOffset UpdatedAt { get; set; }
}