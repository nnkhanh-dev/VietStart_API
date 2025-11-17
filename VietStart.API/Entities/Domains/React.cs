using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VietStart.API.Enums;

namespace VietStart.API.Entities.Domains
{
    public class React
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        public int? CommentId { get; set; }
        [ForeignKey(nameof(CommentId))]
        public Comment Comment { get; set; }
        public int? StartUpId { get; set; }
        [ForeignKey(nameof(StartUpId))]
        public StartUp StartUp { get; set; }
        public ReactType  Type { get; set; }
    }
}
