using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GridBeyond2.Models
{
    public class MyResponse
    {
        public int? statusCode;
        public string data;
        public double max;
        public double min;
        public double average;
        public List<EWindow> mostExpensive;
    }
}