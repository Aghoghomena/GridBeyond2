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
                List<MyData> result = new List<MyData>();
                int count = 0;
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
                                }
                                else
                                {
                                    count++;
                                    MyData md = new MyData();
                                    DateTime thedate = Convert.ToDateTime(rowValues[0]);
                                    md.Date = thedate;
                                    md.Id = count;
                                    md.FormattedDate = thedate.ToString("dddd, dd MMMM yyyy HH:mm:ss");
                                    md.Price = Math.Round(Convert.ToDouble(rowValues[1]), 2);
                                    md.Year = thedate.Year;
                                    md.month = thedate.Month;
                                    result.Add(md);
                                }
                            }
                        }

                    }
                }
             
                Session.Add("SData", result);
                MyResponse output = new MyResponse();
                output.statusCode = 200;
                output.min = result.Min(x => x.Price);
                output.max = result.Max(x => x.Price);
                output.average = result.Average(x => x.Price);
                //to get the most expensive window sort by date so they are consecutive, group where date difference is 30mins
                List<MyData> cs = result.OrderBy(x => x.Date).ToList();
                List<EWindow> ew = new List<EWindow>();
                var groups = cs.GroupBy(x =>
                {
                    var stamp = x.Date;
                    stamp = stamp.AddMinutes(-(stamp.Minute % 30));
                    stamp = stamp.AddMilliseconds(-stamp.Millisecond - 1000 * stamp.Second);
                    return stamp;
                })
                .Select(g => new { FromDate = g.Key, ToDate = g.Key.AddMinutes(30), FromFormattedDate = g.Key.ToString("dddd, dd MMMM yyyy HH:mm:ss"), ToFormattedDate = g.Key.AddMinutes(30).ToString("dddd, dd MMMM yyyy HH:mm:ss"), Price = g.Average(s => s.Price) })
                .OrderByDescending(x=> x.Price).Take(1).ToList();

                foreach (var s in groups)
                {
                    // simple remapping adding extra info to found dataset
                    ew.Add(new EWindow
                    {
                        Id = 1,
                        FromDate = s.FromDate,
                        ToDate = s.ToDate,
                        Price = s.Price,
                        FromFormattedDate = s.FromFormattedDate,
                        ToFormattedDate = s.ToFormattedDate,
                    });
                };
                output.mostExpensive = ew;
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


        public JsonResult GetData(JqueryDatatableParam  model)
        {
            try
            {
                if(Session["SData"] == null)
                {
                    return Json(new
                    {
                        model.sEcho,
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = new List<MyData>(),
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    List<MyData> existingdata = Session["SData"] as List<MyData>;
                    List<MyData> result = new List<MyData>();
                    List<MyData> tblSearched = existingdata;
                    //searching
                    if (!string.IsNullOrEmpty(model.sSearch))
                    {
                        tblSearched = existingdata.Where(x => x.FormattedDate.Contains(model.sSearch.ToLower()) || x.Price.ToString() == model.sSearch).ToList();
                    }
                    //sorting
                    var sortColumnIndex = Convert.ToInt32(HttpContext.Request.QueryString["iSortCol_0"]);
                    var sortDirection = HttpContext.Request.QueryString["sSortDir_0"];
                    if (sortColumnIndex == 0)
                    {
                        result = sortDirection == "asc" ? tblSearched.OrderBy(row => row.FormattedDate).ToList() : tblSearched.OrderByDescending(row => row.FormattedDate).ToList();
                    }
                    else if (sortColumnIndex == 3)
                    {
                        result = sortDirection == "asc" ? tblSearched.OrderBy(row => row.Price).ToList() : tblSearched.AsEnumerable().OrderByDescending(row => row.Price).ToList();
                    }
                    else
                    {
                        //Func<Employee, string> orderingFunction = e => sortColumnIndex == 0 ? e.Name : sortColumnIndex == 1 ? e.Position : e.Location;
                        //employees = sortDirection == "asc" ? employees.OrderBy(orderingFunction) : employees.OrderByDescending(orderingFunction);
                        result = sortDirection == "asc" ? tblSearched.OrderBy(row => row.Id).ToList() : tblSearched.OrderByDescending(row => row.Id).ToList();
                    }

                    //pagination
                    var displayResult = result.Skip(model.iDisplayStart)
                       .Take(model.iDisplayLength).ToList();
                    var totalRecords = result.Count();


                    return Json(new
                    {
                        model.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = totalRecords,
                        aaData = displayResult
                    }, JsonRequestBehavior.AllowGet);

                }

            }
            catch(Exception ex)
            {
                return Json(new
                {
                    model.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData  = new List<MyData>(),
                }, JsonRequestBehavior.AllowGet);

            }
           
        }

        public JsonResult FirstChat(int? year)
        {
            try
            {
                if (Session["SData"] == null)
                {
                    return Json(new
                    {

                        aaData = new List<MyData>(),
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    List<MyData> existingdata = Session["SData"] as List<MyData>;
                    year = year is null ? existingdata.Max(x => x.Year) : year;
                    var data = existingdata.Where(x => x.Year == year).GroupBy(d => d.month)
                            .Select(
                                g => new
                                {
                                    Key = g.Key,
                                    Value = Math.Round(g.Sum(s => s.Price)),
                                    month = g.First().Date.Month,
                                    monthname = g.First().Date.ToString("MMM"),
                                });




                    return Json(new
                    {
                        aaData = data,
                    }, JsonRequestBehavior.AllowGet);

                }

            }
            catch (Exception ex)
            {
                return Json(new
                {
                   
                    aaData = new List<MyData>(),
                }, JsonRequestBehavior.AllowGet);

            }

        }

    }
}

