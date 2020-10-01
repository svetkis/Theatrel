using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class RoleEntity
    {
        [Key]
        public int Id { get; set; }

        public string CharacterName { get; set; }

        public int PerformanceId { get; set; }
        public PerformanceEntity Performance { get; set; }

        public ICollection<ActorInRoleEntity> ActorInRole { get; set; }
    }
}
