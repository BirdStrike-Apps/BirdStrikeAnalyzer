using Supabase.Postgrest.Attributes;

namespace BirdStrikeAnalyzer.Model;

[Table("sample_items")]
public sealed class SampleItem : ObservableBaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
}
