using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CallCenter.Models
{
    public class Call
    {
        public int Id { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; }
    }
}
