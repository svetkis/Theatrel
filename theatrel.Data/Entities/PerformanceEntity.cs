using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace theatrel.DataAccess.Entities
{
    public class PerformanceEntity
    {
        [Key]
        public int Id { get; set; }

        public string Url { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime PerformanceDateTime { get; set; }

        public List<PerformanceChangeEntity> Changes { get; set; }
    }
}
