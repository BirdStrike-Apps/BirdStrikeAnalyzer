using Supabase.Postgrest.Attributes;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Row mapping for public.building_doors, shared with the reporter app.</summary>
[Table("building_doors")]
public sealed class BuildingDoors : ObservableBaseModel
{
    [PrimaryKey("building_door_id")]
    [Column("building_door_id")]
    public Guid BuildingDoorId { get; set; }

    [Column("building_name")] public string BuildingName { get; set; } = string.Empty;
    [Column("door")] public string Door { get; set; } = string.Empty;
    [Column("latitude")] public double? Latitude { get; set; }
    [Column("longitude")] public double? Longitude { get; set; }
}
