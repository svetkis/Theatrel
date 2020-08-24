using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Entities
{
    public class PerformanceTypeEntity
    {
        [Key]
        public int Id { get; set; }

        public string TypeName { get; set; }

        public PerformanceTypeEntity()
        {
        }

        public PerformanceTypeEntity(string name)
        {
            TypeName = name;
        }
    }
}
