using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VietStart.API.Enums;

namespace VietStart.API.Entities.Domains
{
    public class StartUpMedia
    {
        [Key]
        public int Id { get; set; }
        public string Path { get; set; }
        public MediaType Type { get; set; } 
        public int StartUpId { get; set; }
        [ForeignKey("StartUpId")]
        public StartUp StartUp { get; set; }
    }
}
