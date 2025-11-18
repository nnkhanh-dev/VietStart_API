using VietStart.API.Enums;

namespace VietStart.API.Entities.DTO
{
    public class StartUpMediaDto
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public MediaType Type { get; set; }
        public int StartUpId { get; set; }
    }

    public class CreateStartUpMediaDto
    {
        public string Path { get; set; }
        public MediaType Type { get; set; }
        public int StartUpId { get; set; }
    }

    public class UpdateStartUpMediaDto
    {
        public string Path { get; set; }
        public MediaType Type { get; set; }
    }
}
