using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BirdStrikeAnalyzer.Model;

/// <summary>Link from a confirmed bird-strike record to its original reporter submission.</summary>
[Table("bird_strike_confirmations")]
public sealed class BirdStrikeConfirmation : BaseModel
{
    [Column("submission_id")] public Guid SubmissionId { get; set; }
}
