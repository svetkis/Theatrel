using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Entities
{
    public class TelegramUserEntity
    {
        [Key]
        public long Id { get; set; }
    }
}
