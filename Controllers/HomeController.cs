using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using GridBeyond2.Models;

namespace GridBeyond2.Controllers
{
    //mvc controller
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Session.Abandon();
            Session.Clear();
            ViewBag.Title = "Home Page";

            return View();
        }

        [HttpPost]
        // POST api/values
        public JsonResult ReadFile()
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                HttpFileCollectionBase files = Request.Files;
                HttpPostedFileBase currentfile = files[0];
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
                                    dt.Rows.Add(thedate, thedate.ToString("dddd, dd MMMM yyyy HH:mm:ss"), Math.Round(Convert.ToDouble(rowValues[1]), 2));

                                }
                            }
                        }

                    }
                }
                Session["Data"] = dt;
                DataTable dtTop = dt.Rows.Cast<DataRow>().Take(100).CopyToDataTable();
                MyResponse output = new MyResponse();
                output.statusCode = 200;
                output.data = JsonConvert.SerializeObject(dtTop);
                //var deserializedResult = JsonConvert.SerializeObject({ status = 200, data = dtTop });
                return new System.Web.Mvc.JsonResult()
                {
                    Data = output,
                    JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                MyResponse output = new MyResponse();
                output.statusCode = 400;
                return new JsonResult()
                {
                    Data = output ,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
        }
    }
}
