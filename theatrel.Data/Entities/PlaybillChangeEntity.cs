using System;
using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Entities
{
    public class PlaybillChangeEntity
    {
        [Key]
        public int Id { get; set; }

        public int MinPrice { get; set; }

        public int ReasonOfChanges { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime LastUpdate { get; set; }

        public int PlaybillEntityId { get; set; }
        public PlaybillEntity PlaybillEntity { get; set; }
    }
}
