namespace VietStart.API.Entities.DTO
{
    public class TeamStartUpDto
    {
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string StartUpIdea { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public string UserAvatar { get; set; }
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public string? Experience { get; set; }
        public string? Motivation { get; set; }
        public string Status { get; set; }
    }

    public class TeamStartUpDetailDto
    {
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string UserId { get; set; }
        public int PositionId { get; set; }
        public string? Experience { get; set; }
        public string? Motivation { get; set; }
        public string Status { get; set; }

        // Related entities
        public AppUserDto User { get; set; }
        public StartUpDto StartUp { get; set; }
        public PositionDto Position { get; set; }
    }

    public class CreateTeamStartUpDto
    {
        public int StartUpId { get; set; }
        public string UserId { get; set; }
        public int PositionId { get; set; }
        public string? Experience { get; set; }
        public string? Motivation { get; set; }
        public string Status { get; set; } = "?ang ch?"; // Default status
    }

    public class UpdateTeamStartUpDto
    {
        public int PositionId { get; set; }
        public string? Experience { get; set; }
        public string? Motivation { get; set; }
        public string Status { get; set; }
    }

    public class PositionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreatePositionDto
    {
        public string Name { get; set; }
    }

    public class UpdatePositionDto
    {
        public string Name { get; set; }
    }
}
