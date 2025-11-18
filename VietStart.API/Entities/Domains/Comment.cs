using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VietStart.API.Entities.Domains
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        public int StartUpId { get; set; }
        [ForeignKey(nameof(StartUpId))]
        public StartUp StartUp { get; set; }
        public string Content { get; set; }
        public int? ParentCommentId { get; set; }
        [ForeignKey(nameof(ParentCommentId))]
        public Comment ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; }
        public ICollection<React> Reacts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }

    }
}
