using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using theatrel.Interfaces;

namespace theatrel.DataAccess.Entities
{
    public class PerformanceEntity : IPerformanceData
    {
        [Key]
        public int Id { get; set; }

        public string Location { get; set; }
        public string Name { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DateTime { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public int MinPrice { get; set; }

        public List<PerformanceChangeEntity> Changes { get; set; }
    }
}
