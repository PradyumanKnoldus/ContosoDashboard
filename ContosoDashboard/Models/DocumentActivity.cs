using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;
    public DateTime RetainUntil { get; set; } = DateTime.UtcNow.AddYears(7);

    [MaxLength(1000)]
    public string? Details { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;
}