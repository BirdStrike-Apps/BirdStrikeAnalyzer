using Supabase.Postgrest.Attributes;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Row mapping for public.bird_strike_confirmations.</summary>
[Table("bird_strike_confirmations")]
public sealed class BirdStrikeConfirmation : ObservableBaseModel
{
    [PrimaryKey("submission_id")]
    [Column("submission_id")]
    public Guid SubmissionId { get; set; }

    [Column("entry_date")] public DateTime EntryDate { get; set; }
    [Column("entry_time")] public TimeSpan? EntryTime { get; set; }
    [Column("syst_opp")] public string? SystematicOrOpportunistic { get; set; }
    [Column("entry_confirmed")] public bool? EntryConfirmed { get; set; }
    [Column("entry_confirmed_by")] public string? EntryConfirmedBy { get; set; }
    [Column("confirmation_notes")] public string? ConfirmationNotes { get; set; }
    [Column("species")] public string? Species { get; set; }
}
