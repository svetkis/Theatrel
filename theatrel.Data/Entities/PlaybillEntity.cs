using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Entities
{
    public class PlaybillEntity
    {
        [Key]
        public int Id { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime When { get; set; }
        public string Url { get; set; }

        public int PerformanceId { get; set; }
        public PerformanceEntity Performance { get; set; }

        public ICollection<PlaybillChangeEntity> Changes { get; set; }
    }
}
