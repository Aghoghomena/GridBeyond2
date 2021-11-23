using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GridBeyond2.Models
{
    public class EWindow
    {
        public int Id { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromFormattedDate { get; set; }
        public string ToFormattedDate { get; set; }
        public double Price { get; set; }
        public double FromPrice { get; set; }
        public double ToPrice { get; set; }
    }
}