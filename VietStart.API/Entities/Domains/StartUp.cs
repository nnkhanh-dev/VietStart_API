using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VietStart.API.Enums;

namespace VietStart.API.Entities.Domains
{
    public class StartUp
    {
        [Key]
        public int Id { get; set; }
        public string Team { get; set; }
        public string Idea { get; set; }
        public string Prototype { get; set; }
        public string Plan { get; set; }
        public string Relationship { get; set; }
        public Privacy Privacy { get; set; }
        public int Point { get; set; }
        public int IdeaPoint { get; set; }
        public int TeamPoint { get; set; }
        public int PrototypePoint { get; set; }
        public int PlanPoint { get; set; }
        public int RelationshipPoint { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser AppUser { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }

        public ICollection<StartUpMedia> StartUpMedias { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Share> Shares { get; set; }
        public ICollection<React> Reacts { get; set; }
    }
}
