using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class PerformanceEntity
    {
        [Key]
        public int Id { get; set; }

        public int LocationId { get; set; }
        public LocationsEntity Location { get; set; }

        public string Name { get; set; }

        public int TypeId { get; set; }
        public PerformanceTypeEntity Type { get; set; }
    }
}
