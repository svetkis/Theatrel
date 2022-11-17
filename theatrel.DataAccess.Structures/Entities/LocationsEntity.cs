using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities;

public class LocationsEntity
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string ShortDescription { get; set; }

    public TheatreEntity Theatre { get; set; }
}