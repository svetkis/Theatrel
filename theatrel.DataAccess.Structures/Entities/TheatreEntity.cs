using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class TheatreEntity
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
