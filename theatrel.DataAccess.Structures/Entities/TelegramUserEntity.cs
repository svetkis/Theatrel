using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Structures.Entities
{
    public class TelegramUserEntity
    {
        [Key]
        public long Id { get; set; }

        public string Culture { get; set; }
    }
}
