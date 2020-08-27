using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class LocationsEntity
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public LocationsEntity()
        {
        }

        public LocationsEntity(string name)
        {
            Name = name;
        }
    }
}
