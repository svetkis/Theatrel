using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Entities
{
    public class TlUser
    {
        [Key]
        public long Id { get; set; }
    }
}
