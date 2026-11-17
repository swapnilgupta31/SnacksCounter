namespace SnackCounter.Core.Entities;

public class ExchangeRateSnapshot
{
    public int Id { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string ReportMonth { get; set; } = string.Empty; // "YYYY-MM"
    public string Label { get; set; } = "DEMO_ILLUSTRATIVE_RATE";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
