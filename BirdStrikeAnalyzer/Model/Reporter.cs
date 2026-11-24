using Supabase.Postgrest.Attributes;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Row mapping for public.reporters.</summary>
[Table("reporters")]
public sealed class Reporter : ObservableBaseModel
{
    [PrimaryKey("reporter_id")]
    [Column("reporter_id")]
    public Guid ReporterId { get; set; }

    [Column("first_name")] public string FirstName { get; set; } = string.Empty;
    [Column("last_name")] public string? LastName { get; set; }

    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
}
