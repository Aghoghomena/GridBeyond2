using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace GridBeyond2.Controllers
{
    //api controller
    
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public JsonResult Post()
        {
            try
            {
                HttpFileCollection files = HttpContext.Current.Request.Files;
                HttpPostedFile currentfile = files[0];
                DataTable dt = new DataTable();
                dt.Columns.Add("Date", typeof(DateTime));
                dt.Columns.Add("FormattedDate", typeof(string));
                dt.Columns.Add("Price", typeof(double));
                string Fulltext;
                using (StreamReader csvreader = new StreamReader(currentfile.InputStream))
                {
                    while (!csvreader.EndOfStream)
                    {
                        Fulltext = csvreader.ReadToEnd().ToString(); //read full file text  
                        string[] rows = Fulltext.Split('\n'); //split full file text into rows  
                        for (int i = 0; i < rows.Count() - 1; i++)
                        {
                            string[] rowValues = rows[i].Split(','); //split each row with comma to get individual values  
                            {
                                if (i == 0)
                                {
                                    //for (int j = 0; j < rowValues.Count(); j++)
                                    //{
                                    //    dt.Columns.Add(rowValues[j]); //add headers  
                                    //}
                                }
                                else
                                {
                                    DataRow dr = dt.NewRow();
                                    DateTime thedate = Convert.ToDateTime(rowValues[0]);
                                        dt.Rows.Add(thedate, thedate.ToString("dddd, dd MMMM yyyy HH:mm:ss"), Math.Round(Convert.ToDouble(rowValues[1]),2));
                                      
                                }
                            }
                        }

                    }
                }
                DataTable dtTop = dt.Rows.Cast<DataRow>().Take(100).CopyToDataTable();
                return new JsonResult()
                {
                    Data = new { status = 200, data = dtTop },
                    JsonRequestBehavior = JsonRequestBehavior.DenyGet
                };
            }
            catch(Exception ex)
            {
                return new JsonResult()
                {
                    Data = new { status = 401, error_msg= "An error occurred while reading the file. Kindly contact Administrator" },
                    JsonRequestBehavior = JsonRequestBehavior.DenyGet
                };
            }
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
