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
            //clear session on reload
            Session.Abandon();
            Session.Clear();
            ViewBag.Title = "Home Page";

            return View();
        }

        [HttpPost]
        // POST api/values to read the excel file
        public JsonResult ReadFile()
        {
            try
            {
                //get the request files sent
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
                                    //save to the list of MyData Class
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
             
                //store in session to get later another way to achieve this is store in dictionary
                Session.Add("SData", result);
                MyResponse output = new MyResponse();
                output.statusCode = 200;
                //call the aggregate function to get the values
                Aggregates ag = GetAggregates(result, null, null);
                output.min =ag.min ;
                output.max = ag.max;
                output.average = ag.average;
                //to get the most expensive window sort by date so they are consecutive, group where date difference is 30mins;
                List<EWindow> ew = GetConsecutiveWindow(result, null, null);
                //check if there are consecutive values
                output.mostExpensive = ew.OrderByDescending(x=>x.Price).Take(1).ToList();
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
            //this function is expecting the dataTable parameter
            try
            {
                //check if session variable exists
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
                    //give place for the filter date
                    List<MyData> existingdata = Session["SData"] as List<MyData>;
                    List<MyData> result = new List<MyData>();
                    List<MyData> tblSearched = existingdata;
                    //searching
                    if (!string.IsNullOrEmpty(model.sSearch))
                    {
                        tblSearched = existingdata.Where(x => x.FormattedDate.ToLower().Contains(model.sSearch.ToLower()) || x.Price.ToString().Contains(model.sSearch.ToString())).ToList();
                    }
                    //sorting with columns clicked
                    var sortColumnIndex = Convert.ToInt32(HttpContext.Request.QueryString["iSortCol_0"]);
                    var sortDirection = HttpContext.Request.QueryString["sSortDir_0"];
                    if (sortColumnIndex == 1)
                    {
                        result = sortDirection == "asc" ? tblSearched.OrderBy(row => row.FormattedDate).ToList() : tblSearched.OrderByDescending(row => row.FormattedDate).ToList();
                    }
                    else if (sortColumnIndex == 2)
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

        //load chart data sending the date range 
        public JsonResult FirstChat(int? year, DateTime? from , DateTime? to)
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
                    //get the max year because for chart data can only load yearly
                    if(from != null && to != null)
                    {
                        year = year is null ? existingdata.Where(x=>x.Date >= from && x.Date <= to).Max(x => x.Year) : year;
                    }
                    else
                    {
                        year = year is null ? existingdata.Max(x => x.Year) : year;
                    }
                    //get the data grouped by year
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

        //Get Most expensive hour window
        public List<EWindow> GetConsecutiveWindow(List<MyData> result,DateTime? from, DateTime?to)
        {
            //check if it has value
            List<EWindow> ew = new List<EWindow>();
            try
            {
                List<MyData> data = new List<MyData>();
                if (from != null && to != null)
                {
                  data = result.Where(x => x.Date >= from && x.Date <= to).ToList();
                }
                else
                {
                    data = result.ToList();
                }
                //loop through each value to check if date and date consecutive 30minutes exists which is a window
                foreach (MyData md in data)
                {
                    MyData firstwindow = new MyData();
                    MyData secondWindow = new MyData();
                    //check if the consecutive window exits
                    DateTime extratime = md.Date.AddMinutes(30);
                    firstwindow = result.Where(x => x.Date == md.Date).FirstOrDefault();
                    if (firstwindow != null)
                    {
                        secondWindow = result.Where(x => x.Date == extratime).FirstOrDefault();
                        if (secondWindow != null)
                        {
                            //if it exits push to the ew and return
                            ew.Add(new EWindow
                            {
                                Id = 0,
                                FromDate = firstwindow.Date,
                                ToDate = secondWindow.Date,
                                FromFormattedDate = firstwindow.FormattedDate,
                                ToFormattedDate = secondWindow.FormattedDate,
                                Price = Math.Round(((firstwindow.Price + secondWindow.Price) / 2), 2),
                                FromPrice = firstwindow.Price,
                                ToPrice = secondWindow.Price
                            });
                        }
                    }

                }
                return ew;
            }
            catch(Exception ex)
            {
                return ew;
            }
           
        }

        //Get the min, max and average price
        public Aggregates GetAggregates(List<MyData> result, DateTime? from , DateTime? to)
        {
            Aggregates aggregates = new Aggregates();
            try
            {
                //if it has date range filter
                if(from != null && to != null)
                {
                    aggregates.min = result.Where(x=> x.Date >= from && x.Date <= to).Min(x => x.Price);
                    aggregates.max = result.Where(x => x.Date >= from && x.Date <= to).Max(x => x.Price);
                    aggregates.average = result.Where(x => x.Date >= from && x.Date <= to).Average(x => x.Price);
                }
                else
                {
                    //load default
                    aggregates.min = result.Min(x => x.Price);
                    aggregates.max = result.Max(x => x.Price);
                    aggregates.average = result.Average(x => x.Price);
                }


                return aggregates;
            }
            catch(Exception ex)
            {
                return aggregates;

            }
        }

        //function to filter the data by daterange
        public JsonResult filterData(DateTime ? from, DateTime? to)
        {
            try
            {


                if (Session["SData"] == null)
                {
                    return Json(new
                    {
                        iTotalRecords = 0,
                        iTotalDisplayRecords = 0,
                        aaData = new List<MyData>(),
                    }, JsonRequestBehavior.AllowGet);
                }
                //get data from session
                List<MyData> existingdata = Session["SData"] as List<MyData>;

                MyResponse output = new MyResponse();
                output.statusCode = 200;
                //call the aggregate function to get the values
                Aggregates ag = GetAggregates(existingdata, from, to);
                output.min = ag.min;
                output.max = ag.max;
                output.average = ag.average;
                //to get the most expensive window sort by date so they are consecutive, group where date difference is 30mins;
                List<EWindow> ew = GetConsecutiveWindow(existingdata, from, to);
                //check if there are consecutive values
                output.mostExpensive = ew.OrderByDescending(x => x.Price).Take(1).ToList();
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
                    Data = output,
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
        }
    }
}

