using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class ActorInRoleEntity
    {
        [Key]
        public int Id { get; set; }

        public int ActorId { get; set; }
        public ActorEntity Actor { get; set; }

        public int RoleId { get; set; }
        public RoleEntity Role { get; set; }
    }
}
