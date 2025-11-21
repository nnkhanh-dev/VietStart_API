using System.ComponentModel.DataAnnotations;

namespace VietStart.API.Entities.Domains
{
    public class Position
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
