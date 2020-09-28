using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class RoleEntity
    {
        [Key]
        public int Id { get; set; }

        public string СharacterName { get; set; }

        public int PerformanceId { get; set; }
        public PerformanceEntity Performance { get; set; }
    }
}
