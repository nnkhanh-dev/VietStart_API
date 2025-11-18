using VietStart.API.Enums;

namespace VietStart.API.Entities.DTO
{
    public class StartUpDto
    {
        public int Id { get; set; }
        public string Team { get; set; }
        public string Idea { get; set; }
        public string Prototype { get; set; }
        public string Plan { get; set; }
        public string Relationship { get; set; }
        public Privacy Privacy { get; set; }
        public int Point { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StartUpDetailDto
    {
        public int Id { get; set; }
        public string Team { get; set; }
        public string Idea { get; set; }
        public string Prototype { get; set; }
        public string Plan { get; set; }
        public string Relationship { get; set; }
        public Privacy Privacy { get; set; }
        public int Point { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // User Information
        public AppUserDto User { get; set; }

        // Category Information
        public CategoryDto Category { get; set; }

        // Related Data
        public IEnumerable<StartUpMediaDto> Medias { get; set; }
        public IEnumerable<CommentDto> Comments { get; set; }
        public IEnumerable<ShareDto> Shares { get; set; }
        public IEnumerable<ReactDto> Reacts { get; set; }
    }

    public class CreateStartUpDto
    {
        public string Team { get; set; }
        public string Idea { get; set; }
        public string Prototype { get; set; }
        public string Traction { get; set; }
        public string Relationship { get; set; }
        public int Point { get; set; }
        public Privacy Privacy { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateStartUpDto
    {
        public string Team { get; set; }
        public string Idea { get; set; }
        public string Prototype { get; set; }
        public string Traction { get; set; }
        public string Relationship { get; set; }
        public Privacy Privacy { get; set; }
        public int CategoryId { get; set; }
    }
}
