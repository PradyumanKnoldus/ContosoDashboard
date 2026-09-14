using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Tags { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1024)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedDate { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(TaskId))]
    public virtual TaskItem? Task { get; set; }

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
}