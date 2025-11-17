namespace VietStart.API.Entities.Domains
{
    public class Share
    {
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public int StartUpId { get; set; }
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
