﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GridBeyond2.Models
{
    public class TableModel
    {
        // properties are not capital due to json mapping

        public string sEcho { get; set; }
        public string sSearch { get; set; }
        public int iDisplayLength { get; set; }
        public int iDisplayStart { get; set; }
        public int iColumns { get; set; }
        public int iSortCol_0 { get; set; }
        public string sSortDir_0 { get; set; }
        public int iSortingCols { get; set; }
        public string sColumns { get; set; }
    }
}