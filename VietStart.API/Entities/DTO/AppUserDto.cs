namespace VietStart.API.Entities.DTO
{
    public class AppUserDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string Avatar { get; set; }
        public DateTime? DOB { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateAppUserDto
    {
        public string FullName { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string Avatar { get; set; }
        public DateTime? DOB { get; set; }
    }
}
