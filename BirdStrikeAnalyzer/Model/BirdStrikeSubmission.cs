using Supabase.Postgrest.Attributes;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Row mapping for public.reporter_submissions.</summary>
[Table("reporter_submissions")]
public sealed class BirdStrikeSubmission : ObservableBaseModel
{
    [PrimaryKey("submission_id")] public Guid SubmissionId { get; set; }
    [Column("date")] public DateTime ReportDate { get; set; }
    [Column("time")] public TimeSpan ReportTime { get; set; }
    [Column("reporter_id")] public Guid? ReporterId { get; set; }
    [Column("reporter_notes")] public string? ReporterNotes { get; set; }
    [Column("building_door_id")] public Guid BuildingDoorId { get; set; }
    [Column("is_confirmed")] public bool IsConfirmed { get; set; }
}
