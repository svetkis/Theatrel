using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities;

public class ActorEntity
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public ICollection<ActorInRoleEntity> ActorInRole { get; set; }
}