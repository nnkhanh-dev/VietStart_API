namespace VietStart.API.Entities.DTO
{
    public class ShareDto
    {
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public int StartUpId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateShareDto
    {
        public int StartUpId { get; set; }
        public string Content { get; set; }
    }

    public class UpdateShareDto
    {
        public string Content { get; set; }
    }
}
