using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VietStart.API.Entities.Domains
{
    public class Share
    {
        [Key]
        [Column(Order = 1)]
        public string UserId { get; set; }
        
        [Key]
        [Column(Order = 2)]
        public int StartUpId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        
        [ForeignKey(nameof(StartUpId))]
        public StartUp StartUp { get; set; }
        
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
    }
}
