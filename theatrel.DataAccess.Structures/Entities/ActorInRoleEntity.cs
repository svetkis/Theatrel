namespace theatrel.DataAccess.Structures.Entities
{
    public class ActorInRoleEntity
    {
        public int RoleId { get; set; }

        public RoleEntity Role { get; set; }

        public int ActorId { get; set; }

        public ActorEntity Actor { get; set; }

        public int PlaybillId { get; set; }
        public PlaybillEntity Playbill { get; set; }
    }
}
