using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using scfs_erp.Context;
using scfs_erp.Helper;
using scfs_erp.Models;
using scfs.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace scfs_erp.Controllers.Export
{
    [SessionExpire]
    public class SealController : Controller
    {
        // GET: Seal
        #region Context Declaration
        SCFSERPContext context = new SCFSERPContext();
        #endregion

        #region Index Page
        [Authorize(Roles = "ExportSealIndex")]
        public ActionResult Index()
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            if (string.IsNullOrEmpty(Session["SDATE"] as string))
            {
                Session["SDATE"] = DateTime.Now.ToString("dd-MM-yyyy");
                Session["EDATE"] = DateTime.Now.ToString("dd-MM-yyyy");
            }
            else
            {
                if (Request.Form.Get("from") != null)
                {
                    Session["SDATE"] = Request.Form.Get("from");
                    Session["EDATE"] = Request.Form.Get("to");
                }
            }

            DateTime fromdate = Convert.ToDateTime(System.Web.HttpContext.Current.Session["SDATE"]).Date;
            DateTime todate = Convert.ToDateTime(System.Web.HttpContext.Current.Session["EDATE"]).Date;
                       
            TotalContainerDetails(fromdate, todate);

            return View();

            //return View(context.stuffingmasters.Where(x => x.STFMTIME >= sd).Where(x => x.STFMDATE <= ed).Where(x => x.STFTID == 1).ToList());
        }
        #endregion

        #region Getting Data for Grid Or IndexTable
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            using (var e = new CFSExportEntities())
            {

                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                int trc = Convert.ToInt32(totalRowsCount.Value);
                int frc = Convert.ToInt32(filteredRowsCount.Value);
                int srn = Convert.ToInt32(param.iDisplayStart);
                int ern = Convert.ToInt32(param.iDisplayStart + param.iDisplayLength);
                int CompyId = Convert.ToInt32(Session["compyid"]);

                DateTime sdate = Convert.ToDateTime(Session["SDATE"]).Date;
                DateTime edate = Convert.ToDateTime(Session["EDATE"]).Date;

                string squery = "exec [pr_Search_Export_Stuffing_Master] @FilterTerm='" + param.sSearch + "',";
                squery += "@SortIndex=" + Convert.ToInt32(Request["iSortCol_0"]) + ",@SortDirection='" + Request["sSortDir_0"] + "',";
                squery += "@StartRowNum=" + srn + ",@EndRowNum=" + ern + ",";
                squery += "@TotalRowsCount=" + trc + ",@FilteredRowsCount=" + frc + ",@PCompyId=" + CompyId + ",@PSTFTId=1,";
                squery += "@PSDate='" + sdate.ToString("yyyy-MM-dd") + "',@PEDate='" + edate.ToString("yyyy-MM-dd") + "'";

                var data = context.Database.SqlQuery<pr_Search_Export_Stuffing_Master_Result>(squery).ToList();

                //var data = e.pr_Search_Export_Stuffing_Master(param.sSearch, Convert.ToInt32(Request["iSortCol_0"]), Request["sSortDir_0"], param.iDisplayStart, param.iDisplayStart + param.iDisplayLength,
                //   totalRowsCount, filteredRowsCount, Convert.ToInt32(Session["compyid"]), 1, Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]));

                if (data.Count > 0)
                {
                    var aaData = data.Select(d => new string[] { d.STFMDATE.Value.ToString("dd/MM/yyyy"), d.STFMDNO.ToString(), d.STFMNAME, d.STFDSBDNO, d.STFCORDNO, d.CONTNRNO, d.STFDNOP.ToString(), d.DISPSTATUS.ToString(), d.STFMID.ToString(), d.STFDID.ToString() }).ToArray();

                    return Json(new
                    {
                        sEcho = param.sEcho,
                        aaData = aaData,
                        iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                        iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var aaData = "";

                    return Json(new
                    {
                        sEcho = param.sEcho,
                        aaData = aaData,
                        iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                        iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                    }, JsonRequestBehavior.AllowGet);
                }                
            }
        }
        #endregion

        #region TotalContainerDetails
        public JsonResult TotalContainerDetails(DateTime fromdate, DateTime todate)
        {
            string fdate = ""; string tdate = ""; int sdptid = 1;
            if (fromdate == null)
            {
                fromdate = DateTime.Now.Date;
                fdate = Convert.ToString(fromdate);
            }
            else
            {
                string infdate = Convert.ToString(fromdate);
                var in_date = infdate.Split(' ');
                var in_date1 = in_date[0].Split('/');
                fdate = Convert.ToString(in_date1[2] + "-" + in_date1[1] + "-" + in_date1[0]);
            }
            if (todate == null)
            {
                todate = DateTime.Now.Date;
                tdate = Convert.ToString(todate);
            }
            else
            {
                string intdate = Convert.ToString(todate);

                var in_date1 = intdate.Split(' ');
                var in_date2 = in_date1[0].Split('/');
                tdate = Convert.ToString(in_date2[2] + "-" + in_date2[1] + "-" + in_date2[0]);

            }


            var result = context.Database.SqlQuery<PR_EXPORT_SHIPPINGBILLCOUNT_DETAILS_Result>("EXEC PR_EXPORT_SHIPPINGBILLCOUNT_DETAILS @PFDT='" + fdate + "',@PTDT='" + tdate + "'").ToList();

            foreach (var rslt in result)
            {
                if ((rslt.Sno == 2) && (rslt.Descriptn == "EXPORT SEAL"))
                {
                    @ViewBag.TotalEST = rslt.seal_sb;
                    Session["ESBSEAL"] = rslt.seal_sb;

                }

            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        #endregion

        #region Edit Page Id
        [Authorize(Roles = "ExportSealEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/Seal/NForm/" + id);
        }
        #endregion

        #region insert/update form design
        [Authorize(Roles = "ExportSealCreate")]
        public ActionResult NForm(int id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            context.Database.ExecuteSqlCommand("DELETE FROM TMP_STUFFING_SBDID WHERE KUSRID='" + Session["CUSRID"] + "'");
            StuffingMaster tab = new StuffingMaster();
            StuffingMD vm = new StuffingMD();
            ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6 && m.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            //ViewBag.EOPTID = new SelectList(context.Export_OperationTypeMaster.OrderBy(m => m.EOPTDESC), "EOPTID", "EOPTDESC");
            ViewBag.EOPTID = new SelectList(context.Export_OperationTypeMaster.Where(m => (m.EOPTID == 6 || m.EOPTID == 7 || m.EOPTID == 8 || m.EOPTID == 9) && m.DISPSTATUS == 0).OrderBy(m => m.EOPTDESC), "EOPTID", "EOPTDESC");
            ViewBag.SLABTID = 0;
            ViewBag.STFCATEAID = new SelectList("");
            ViewBag.STFBCATEAID = new SelectList("");
            ViewBag.STFCORDID = new SelectList("");
            ViewBag.SBDID = new SelectList("");
            ViewBag.GIDID = new SelectList("");

            ViewBag.STFTYPE = new SelectList(context.Database.SqlQuery<Export_Seal_Type_Master>("SELECT * FROM EXPORT_SEAL_TYPE_MASTER").ToList(), "ESLTID", "ESLTDESC");

            //BILLED TO
            List<SelectListItem> selectedtaxlst1 = new List<SelectListItem>();
            SelectListItem selectedItemtax1 = new SelectListItem { Text = "EXPORTER", Value = "1", Selected = false };
            selectedtaxlst1.Add(selectedItemtax1);
            selectedItemtax1 = new SelectListItem { Text = "CHA", Value = "0", Selected = true };
            selectedtaxlst1.Add(selectedItemtax1);
            ViewBag.BILLEDTO = selectedtaxlst1;


            if (id != 0)
            {
                tab = context.stuffingmasters.Find(id);//find selected record
                var Cont_result = context.Database.SqlQuery<SP_STUFFING_SEAL_CONTAINER_NO_CBX_ASSGN_Result>("SP_STUFFING_SEAL_CONTAINER_NO_CBX_ASSGN @PCHAID= " + tab.CHAID + ",@PSTFMID=" + id + "").ToList();
                //ViewBag.EOPTID = new SelectList(context.Export_OperationTypeMaster.OrderBy(m => m.EOPTDESC), "EOPTID", "EOPTDESC", tab.EOPTID);
                ViewBag.EOPTID = new SelectList(context.Export_OperationTypeMaster.Where(m => (m.EOPTID == 6 || m.EOPTID == 7 || m.EOPTID == 8 || m.EOPTID == 9) && m.DISPSTATUS == 0).OrderBy(m => m.EOPTDESC), "EOPTID", "EOPTDESC", tab.EOPTID);
                ViewBag.SLABTID = 0;

                //.......................................Dropdown data........................................//

                ViewBag.GIDID = new SelectList(Cont_result, "GIDID", "CONTNRNO");

                ViewBag.STFCATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == tab.CHAID).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", tab.STFCATEAID);
                ViewBag.STFBCATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == tab.STFBILLREFID).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", tab.STFBCATEAID);

                var sql5 = context.Database.SqlQuery<CategoryMaster>("select * from CategoryMaster where CATEID=" + tab.STFBILLREFID).ToList();
                ViewBag.STFBILLREFNAME = sql5[0].CATENAME;

                var sql6 = context.Database.SqlQuery<Category_Address_Details>("select *from CATEGORY_ADDRESS_DETAIL WHERE CATEID=" + tab.STFBILLREFID).ToList();
                ViewBag.STFBILLGSTNO = sql6[0].CATEAGSTNO;

                List<SelectListItem> selectedcontainertype01 = new List<SelectListItem>();
                if (tab.STFBILLEDTO == 1)
                {
                    SelectListItem selectedcontainer01 = new SelectListItem { Text = "EXPPORTER", Value = "1", Selected = true };
                    selectedcontainertype01.Add(selectedcontainer01);
                    selectedcontainer01 = new SelectListItem { Text = "CHA", Value = "0", Selected = false };
                    selectedcontainertype01.Add(selectedcontainer01);
                    ViewBag.BILLEDTO = selectedcontainertype01;
                }


                //.........End
                vm.masterdata = context.stuffingmasters.Where(det => det.STFMID == id).ToList();
                vm.detaildata = context.stuffingdetails.Where(det => det.STFMID == id).ToList();
                vm.stuffsealdetail = context.Database.SqlQuery<PR_STUFFINGDETAIL_FLX_ASSGN_Result>("PR_STUFFINGDETAIL_FLX_ASSGN @PSTFMID=" + id + "").ToList();//........procedure  for edit mode details data
            }

            return View(vm);

        }
        #endregion

        #region Insert and Update data
        public void savedata(FormCollection F_Form)
        {
            using (SCFSERPContext context = new SCFSERPContext())
            {
                using (var trans = context.Database.BeginTransaction())
                {
                    try
                    {
                        StuffingMaster stuffingmaster = new StuffingMaster();
                        StuffingDetail stuffingdetail = new StuffingDetail();
                        StuffingProductDetail stuffingproductdetail = new StuffingProductDetail();

                        //.................Getting Primarykey field..................//

                        int STFMID = 0;
                        string mid = Convert.ToString(F_Form["masterdata[0].STFMID"]);
                        if (mid == "0" || mid == "" || mid == null)
                        {
                            STFMID = 0;
                        }
                        else { STFMID = Convert.ToInt32(mid); }

                        Int32 STFDID = 0;
                        Int32 STFPID = 0;
                        string DELIDS = "";
                        string P_DELIDS = "";//.....End

                        // Capture before state for edit logging
                        StuffingMaster before = null;
                        if (STFMID != 0)//Getting Primary id in Edit mode
                        {
                            stuffingmaster = context.stuffingmasters.Find(STFMID);
                            try
                            {
                                before = context.stuffingmasters.AsNoTracking().FirstOrDefault(x => x.STFMID == STFMID);
                                if (before != null)
                                {
                                    EnsureBaselineVersionZero(before, Session["CUSRID"]?.ToString() ?? "");
                                }
                            }
                            catch { /* ignore if baseline creation fails */ }
                        }
                        //........................Stuffing Master..............//

                        string todaydt = Convert.ToString(DateTime.Now);
                        string todayd = Convert.ToString(DateTime.Now.Date);

                        string indate = Convert.ToString(F_Form["masterdata[0].STFMDATE"]);
                        string intime = Convert.ToString(F_Form["masterdata[0].STFMTIME"]);

                        stuffingmaster.COMPYID = Convert.ToInt32(Session["compyid"]);
                        stuffingmaster.STFTID = 1;

                        if (indate != null || indate != "")
                        {
                            stuffingmaster.STFMDATE = Convert.ToDateTime(indate).Date;
                        }
                        else { stuffingmaster.STFMDATE = DateTime.Now.Date; }

                        if (stuffingmaster.STFMDATE > Convert.ToDateTime(todayd))
                        {
                            stuffingmaster.STFMDATE = Convert.ToDateTime(todayd);
                        }

                        if ((intime != null || intime != "") && ((indate != null || indate != "")))
                        {
                            if ((intime.Contains(' ')) && (indate.Contains(' ')))
                            {
                                var in_time = intime.Split(' ');
                                var in_date = indate.Split(' ');

                                if ((in_time[1].Contains(':')) && (in_date[0].Contains('/')))
                                {

                                    var in_time1 = in_time[1].Split(':');
                                    var in_date1 = in_date[0].Split('/');

                                    string in_datetime = in_date1[2] + "-" + in_date1[1] + "-" + in_date1[0] + "  " + in_time1[0] + ":" + in_time1[1] + ":" + in_time1[2];

                                    stuffingmaster.STFMTIME = Convert.ToDateTime(in_datetime);
                                }
                                else { stuffingmaster.STFMTIME = DateTime.Now; }
                            }
                            else {
                                var in_time = intime;
                                var in_date = indate;

                                if ((in_time.Contains(':')) && (in_date.Contains('/')))
                                {

                                    var in_time1 = in_time.Split(':');
                                    var in_date1 = in_date.Split('/');

                                    string in_datetime = in_date1[2] + "-" + in_date1[1] + "-" + in_date1[0] + "  " + in_time1[0] + ":" + in_time1[1] + ":" + in_time1[2];

                                    stuffingmaster.STFMTIME = Convert.ToDateTime(in_datetime);
                                }
                                else { stuffingmaster.STFMTIME = DateTime.Now; }
                                                                
                            }
                        }
                        else { stuffingmaster.STFMTIME = DateTime.Now; }

                        if (stuffingmaster.STFMTIME > Convert.ToDateTime(todaydt))
                        {
                            stuffingmaster.STFMTIME = Convert.ToDateTime(todaydt);
                        }

                        //stuffingmaster.STFMDATE = Convert.ToDateTime(F_Form["masterdata[0].STFMTIME"]).Date;
                        //stuffingmaster.STFMTIME = Convert.ToDateTime(F_Form["masterdata[0].STFMTIME"]);

                        stuffingmaster.CHAID = Convert.ToInt32(F_Form["masterdata[0].CHAID"]);
                        stuffingmaster.STFMNAME = F_Form["masterdata[0].STFMNAME"].ToString();
                        stuffingmaster.LCATEID = 0;
                        stuffingmaster.LCATENAME = "";
                        stuffingmaster.EOPTID = Convert.ToInt32(F_Form["EOPTID"]);                        
                        stuffingmaster.TGID = 0;
                        stuffingmaster.SLABTID = Convert.ToInt32(F_Form["EOPTID"]);

                        if (STFMID == 0 || stuffingmaster.CUSRID == null)
                        {
                            stuffingmaster.CUSRID = Session["CUSRID"].ToString();
                        }
                        stuffingmaster.LMUSRID = Session["CUSRID"].ToString();
                        //stuffingmaster.LMUSRID = 1;
                        stuffingmaster.DISPSTATUS = 0;
                        stuffingmaster.PRCSDATE = DateTime.Now;



                        stuffingmaster.STFBILLEDTO = Convert.ToInt16(F_Form["BILLEDTO"]);

                        if (Convert.ToString(F_Form["masterdata[0].STFBILLREFID"]) != "" || Convert.ToString(F_Form["masterdata[0].STFBILLREFID"]) != "0")
                        {
                            stuffingmaster.STFBILLREFID = Convert.ToInt32(F_Form["masterdata[0].STFBILLREFID"]);
                        }
                        else { stuffingmaster.STFBILLREFID = 0; }

                        stuffingmaster.STFBILLREFNAME = Convert.ToString(F_Form["masterdata[0].STFBILLREFNAME"]);

                        string STFCATEAID = Convert.ToString(F_Form["STFCATEAID"]);
                        if (STFCATEAID != "" || STFCATEAID != null || STFCATEAID != "0")
                        {
                            stuffingmaster.STFCATEAID = Convert.ToInt32(STFCATEAID);
                        }
                        else { stuffingmaster.STFCATEAID = 0; }

                        string STATEID = Convert.ToString(F_Form["masterdata[0].STATEID"]);
                        if (STATEID != "" || STATEID != null || STATEID != "0")
                        {
                            stuffingmaster.STATEID = Convert.ToInt32(STATEID);
                        }
                        else { stuffingmaster.STATEID = 0; }

                        stuffingmaster.CATEAGSTNO = Convert.ToString(F_Form["masterdata[0].CATEAGSTNO"]);
                        stuffingmaster.TRANIMPADDR1 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR1"]);
                        stuffingmaster.TRANIMPADDR2 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR2"]);
                        stuffingmaster.TRANIMPADDR3 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR3"]);
                        stuffingmaster.TRANIMPADDR4 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR4"]);

                        string STFBCATEAID = Convert.ToString(F_Form["STFBCATEAID"]);
                        if (STFBCATEAID != "" || STFBCATEAID != null)
                        {
                            stuffingmaster.STFBCATEAID = Convert.ToInt32(STFBCATEAID);
                        }
                        else { stuffingmaster.STFBCATEAID = 0; }

                        string STFBCHASTATEID = Convert.ToString(F_Form["masterdata[0].STFBCHASTATEID"]);
                        if (STFBCHASTATEID != "" || STFBCHASTATEID != null)
                        {
                            stuffingmaster.STFBCHASTATEID = Convert.ToInt32(STFBCHASTATEID);
                        }
                        else { stuffingmaster.STFBCHASTATEID = 0; }

                        stuffingmaster.STFBCHAGSTNO = Convert.ToString(F_Form["masterdata[0].STFBCHAGSTNO"]);
                        stuffingmaster.STFBCHAADDR1 = Convert.ToString(F_Form["masterdata[0].STFBCHAADDR1"]);
                        stuffingmaster.STFBCHAADDR2 = Convert.ToString(F_Form["masterdata[0].STFBCHAADDR2"]);
                        stuffingmaster.STFBCHAADDR3 = Convert.ToString(F_Form["masterdata[0].STFBCHAADDR3"]);
                        stuffingmaster.STFBCHAADDR4 = Convert.ToString(F_Form["masterdata[0].STFBCHAADDR4"]);

                        string STF_SBILL_RNO = Convert.ToString(F_Form["masterdata[0].STF_SBILL_RNO"]);
                        if (STF_SBILL_RNO == "" || STF_SBILL_RNO == null)
                        {
                            stuffingmaster.STF_SBILL_RNO = "-";
                        }
                        else { stuffingmaster.STF_SBILL_RNO = Convert.ToString(F_Form["masterdata[0].STF_SBILL_RNO"]);  }

                        string STF_FORM13_RNO = Convert.ToString(F_Form["masterdata[0].STF_FORM13_RNO"]);
                        if (STF_FORM13_RNO == "" || STF_FORM13_RNO == null)
                        {
                            stuffingmaster.STF_FORM13_RNO = "-";
                        }
                        else { stuffingmaster.STF_FORM13_RNO = Convert.ToString(F_Form["masterdata[0].STF_FORM13_RNO"]); }

                        //stuffingmaster.STF_SBILL_RNO = Convert.ToString(F_Form["masterdata[0].STF_SBILL_RNO"]);
                        //stuffingmaster.STF_FORM13_RNO = Convert.ToString(F_Form["masterdata[0].STF_FORM13_RNO"]);


                        if (STFMID == 0)
                        {
                            stuffingmaster.STFMNO = Convert.ToInt32(Autonumber.autonum("stuffingmaster", "STFMNO", "STFTID = 1 AND COMPYID = " + Convert.ToInt32(Session["compyid"]) + "").ToString());
                            int ano = stuffingmaster.STFMNO;
                            string prfx = string.Format("{0:D5}", ano);
                            stuffingmaster.STFMDNO = prfx.ToString();
                            context.stuffingmasters.Add(stuffingmaster);
                            context.SaveChanges();
                        }
                        else
                        {
                            context.Entry(stuffingmaster).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        } //.........End of stuffing Master 


                        //....................stuffing Detail---------------------------
                        string[] A_STFDID = F_Form.GetValues("STFDID");
                        string[] A_STFPID = F_Form.GetValues("STFPID");
                        string[] GIDID = F_Form.GetValues("GIDID");
                        string[] STFTYPE = F_Form.GetValues("detaildata[0].STFTYPE");
                        string[] STFREFNO = F_Form.GetValues("STFREFNO");
                        string[] booltype = F_Form.GetValues("booltype");
                        string[] STFCORDNO = F_Form.GetValues("STFCORDNO");
                        string[] STFDSBDATE = F_Form.GetValues("STFDSBDATE");
                        string[] STFDNOP = F_Form.GetValues("STFDNOP");
                        string[] STFDQTY = F_Form.GetValues("STFDQTY");
                        string[] STFDSBDDATE = F_Form.GetValues("STFDSBDDATE");
                        string[] STFDSBNO = F_Form.GetValues("STFDSBNO");
                        string[] STFDSBDNO = F_Form.GetValues("STFDSBDNO");
                        string[] SBDID = F_Form.GetValues("SBDID");
                        string[] desc = F_Form.GetValues("STFCORDDESC");

                        string[] ESBDID = F_Form.GetValues("ESBDID");
                        string[] ESBD_SBILLDATE = F_Form.GetValues("ESBD_SBILLDATE");
                        string[] ESBD_SBILLNO = F_Form.GetValues("ESBD_SBILLNO");

                        HashSet<int> hash = new HashSet<int>(context.stuffingdetails.Select(m => m.GIDID));

                        for (int count = 0; count < A_STFDID.Count(); count++)
                        {
                            //if (booltype[count] == "true")
                            //{
                            STFDID = Convert.ToInt32(A_STFDID[count]);
                            STFPID = Convert.ToInt32(A_STFPID[count]);
                            // var bools = Convert.ToString(booltype[count]);
                            if (STFDID != 0 /*&& bools == "true"*/)
                            {
                                stuffingdetail = context.stuffingdetails.Find(STFDID);
                                stuffingproductdetail = context.stuffingproductdetails.Find(STFPID);
                            }

                            var f_gidid = Convert.ToInt32(GIDID[count]);
                            if (Convert.ToInt32(STFDID) == 0)
                            {

                                //......... insert condition for Container No.....................
                                if (!hash.Contains(f_gidid))
                                {
                                    stuffingdetail.STFMID = stuffingmaster.STFMID;
                                    stuffingdetail.GIDID = Convert.ToInt32(GIDID[count]);
                                    stuffingdetail.STFTYPE = Convert.ToInt16(STFTYPE[count]);
                                    if (stuffingdetail.CUSRID == null)
                                        stuffingdetail.CUSRID = Session["CUSRID"].ToString();
                                    stuffingdetail.LMUSRID = Session["CUSRID"].ToString();
                                    //stuffingdetail.LMUSRID = 1;
                                    stuffingdetail.DISPSTATUS = 0;
                                    stuffingdetail.PRCSDATE = DateTime.Now;
                                    context.stuffingdetails.Add(stuffingdetail);
                                    context.SaveChanges();
                                    STFDID = stuffingdetail.STFDID;
                                    hash.Add(f_gidid);
                                }
                                else
                                {
                                    hash.Add(f_gidid);
                                    var sql = context.Database.SqlQuery<int>("SELECT STFDID from stuffingdetail where GIDID=" + Convert.ToInt32(f_gidid)).ToList();
                                    var ids = (sql[0]).ToString();
                                    stuffingdetail = context.stuffingdetails.Find(Convert.ToInt32(ids));
                                    context.Entry(stuffingdetail).Entity.STFTYPE = Convert.ToInt16(STFTYPE[count]);

                                }
                                STFDID = stuffingdetail.STFDID;

                            }
                            else
                            {
                                hash.Add(f_gidid);
                                stuffingdetail.STFMID = stuffingmaster.STFMID;
                                stuffingdetail.GIDID = Convert.ToInt32(GIDID[count]);
                                stuffingdetail.STFTYPE = Convert.ToInt16(STFTYPE[count]);
                                if (stuffingdetail.CUSRID == null)
                                    stuffingdetail.CUSRID = Session["CUSRID"].ToString();
                                stuffingdetail.LMUSRID = Session["CUSRID"].ToString();
                                //stuffingdetail.LMUSRID = 1;
                                stuffingdetail.DISPSTATUS = 0;
                                stuffingdetail.PRCSDATE = DateTime.Now;
                                // stuffingdetail.STFDID = STFDID;
                                context.Entry(stuffingdetail).State = System.Data.Entity.EntityState.Modified;
                                context.SaveChanges();
                            }
                            //.................Stuffing Product detail..............//
                            stuffingproductdetail.STFDID = stuffingdetail.STFDID;
                            stuffingproductdetail.STFCORDNO = STFCORDNO[count].ToString();
                            stuffingproductdetail.STFDSBNO = STFDSBNO[count];
                            stuffingproductdetail.SBDID = Convert.ToInt32(SBDID[count]);
                            stuffingproductdetail.STFDSBDNO = STFDSBDNO[count];
                            stuffingproductdetail.STFDSBDATE = Convert.ToDateTime(STFDSBDATE[count]).Date;
                            stuffingproductdetail.STFDSBDDATE = Convert.ToDateTime(STFDSBDDATE[count]).Date;
                            stuffingproductdetail.STFDNOP = Convert.ToDecimal(STFDNOP[count]);
                            stuffingproductdetail.STFDQTY = Convert.ToDecimal(STFDQTY[count]);


                            stuffingproductdetail.ESBDID = Convert.ToInt32(ESBDID[count]);
                            stuffingproductdetail.ESBD_SBILLDATE = Convert.ToDateTime(ESBD_SBILLDATE[count]).Date;
                            stuffingproductdetail.ESBD_SBILLNO = ESBD_SBILLNO[count].ToString();
                            if (Session["CUSRID"] != null)
                            {
                                stuffingproductdetail.CUSRID = Session["CUSRID"].ToString();

                            }
                            stuffingproductdetail.LMUSRID = Session["CUSRID"].ToString();

                            stuffingproductdetail.CUSRID = "admin";
                            stuffingproductdetail.LMUSRID = Session["CUSRID"].ToString();

                            stuffingproductdetail.DISPSTATUS = 0;
                            stuffingproductdetail.PRCSDATE = DateTime.Now;
                            if (Convert.ToInt32(STFPID) == 0)
                            {
                                context.stuffingproductdetails.Add(stuffingproductdetail);
                                context.SaveChanges();
                                STFPID = stuffingproductdetail.STFPID;
                            }
                            else
                            {
                                stuffingproductdetail.STFPID = STFPID;
                                context.Entry(stuffingproductdetail).State = System.Data.Entity.EntityState.Modified;
                                context.SaveChanges();
                            }
                            DELIDS = DELIDS + "," + STFDID.ToString();
                            P_DELIDS = P_DELIDS + "," + STFPID.ToString();
                            //count++;
                        }

                        context.Database.ExecuteSqlCommand("DELETE FROM stuffingproductdetail  WHERE STFDID IN(" + DELIDS.Substring(1) + ") and  STFPID NOT IN(" + P_DELIDS.Substring(1) + ")");
                        context.Database.ExecuteSqlCommand("DELETE FROM stuffingdetail  WHERE STFMID=" + stuffingmaster.STFMID + " and  STFDID NOT IN(" + DELIDS.Substring(1) + ")");
                        context.Database.ExecuteSqlCommand("DELETE FROM TMP_STUFFING_SBDID WHERE KUSRID='" + Session["CUSRID"] + "'");
                        
                        // Log changes after successful save (before commit)
                        if (before != null && stuffingmaster.STFMID != 0)
                        {
                            try
                            {
                                var after = context.stuffingmasters.AsNoTracking().FirstOrDefault(x => x.STFMID == stuffingmaster.STFMID);
                                if (after != null)
                                {
                                    LogSealEdits(before, after, Session["CUSRID"]?.ToString() ?? "");
                                }
                            }
                            catch { /* ignore logging errors */ }
                        }
                        else if (STFMID == 0 && stuffingmaster.STFMID != 0)
                        {
                            // Create baseline for new record
                            try
                            {
                                var newRecord = context.stuffingmasters.AsNoTracking().FirstOrDefault(x => x.STFMID == stuffingmaster.STFMID);
                                if (newRecord != null)
                                {
                                    EnsureBaselineVersionZero(newRecord, Session["CUSRID"]?.ToString() ?? "");
                                }
                            }
                            catch { /* ignore baseline creation errors */ }
                        }
                        
                        trans.Commit(); Response.Redirect("Index");
                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message.ToString();
                        trans.Rollback();
                        Response.Redirect("/Error/AccessDenied");
                    }
                }
            }
        }
        #endregion

        #region Autocomplete CHA Name
        public JsonResult AutoCha(string term)
        {
            var result = (from r in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult NewAutoCha(string term)
        {

            var result = (from r in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region cha and Exporter
        public JsonResult NewAutoExporter(string term)
        {
            var result = (from r in context.categorymasters.Where(m => m.CATETID == 2).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region NEW FORM DATA
        public JsonResult GetCartingNo(int id)
        {
            var data = context.Database.SqlQuery<pr_Export_Stuffing_COrder_No_Assgn_Result>("pr_Export_Stuffing_COrder_No_Assgn @PCHAID=" + id + ",@PKUSRID='" + Session["CUSRID"] + "'").ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region GetContNo
        public JsonResult GetContNo(string id)
        {
            var param = id.Split(';');
            var CHAID = 0;
            var STFMID = 0;
            if (param[0] != "") { CHAID = Convert.ToInt32(param[0]); } else { CHAID = 0; }
            if (param[1] != "") { STFMID = Convert.ToInt32(param[1]); } else { STFMID = 0; }
            var data = context.Database.SqlQuery<SP_STUFFING_SEAL_CONTAINER_NO_CBX_ASSGN_Result>("SP_STUFFING_SEAL_CONTAINER_NO_CBX_ASSGN @PCHAID=" + CHAID + ",@PSTFMID=" + STFMID + "").ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region GetRefNo
        //public JsonResult GetRefNo(string id)
        //{
        //    var param = id.Split(';');
        //    var CHAID = Convert.ToInt32(param[0]); var STFCORDID = Convert.ToInt32(param[1]);
        //    var data = context.Database.SqlQuery<VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN>("select * from VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN where SBMID=" + STFCORDID + " AND (KUSRID='" + Session["CUSRID"] + "' OR KUSRID IS NULL) order by SBMID,SBDID").ToList();            
            
        //    return Json(data, JsonRequestBehavior.AllowGet);
        //}
        public JsonResult GetRefNo(string id)
        {
            var param = id.Split(';');
            var CHAID = Convert.ToInt32(param[0]); var STFCORDID = Convert.ToInt32(param[1]);
            //var data = context.Database.SqlQuery<VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN>("select * from VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN where SBMID=" + STFCORDID + " AND (KUSRID='" + Session["CUSRID"] + "' OR KUSRID IS NULL) order by SBMID,SBDID").ToList();
            //var qry = "select * from VW_STUFFING_ORGSBILLNO_REFNO_CBX_ASSGN where SBMID=" + STFCORDID + " AND (KUSRID='" + Session["CUSRID"] + "' OR KUSRID IS NULL) order by SBMID,SBDID";
            var qry = "select * from VW_STUFFING_ORGSBILLNO_REFNO_CBX_ASSGN where SBMID=" + STFCORDID + " AND (KUSRID='" + Session["CUSRID"] + "' OR KUSRID='') order by SBMID,SBDID";
            var data = context.Database.SqlQuery<VW_STUFFING_ORGSBILLNO_REFNO_CBX_ASSGN>(qry).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region GetRefDetails
        //public JsonResult GetRefDetails(string id)
        //{
        //    var param = id.Split(';');
        //    var CHAID = Convert.ToInt32(param[0]); var STFCORDID = Convert.ToInt32(param[1]); var SBDID = Convert.ToInt32(param[2]);
        //    var data = context.Database.SqlQuery<VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN>("select * from VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN where CHAID=" + CHAID + " and SBMID=" + STFCORDID + " and SBDID=" + SBDID + "order by SBMID,SBDID").ToList();
        //    return Json(data, JsonRequestBehavior.AllowGet);
        //}


        public JsonResult GetRefDetails(string id)
        {
            var param = id.Split(';');
            var CHAID = Convert.ToInt32(param[0]); var STFCORDID = Convert.ToInt32(param[1]);
            var SBDID = Convert.ToInt32(param[2]); var ORGSBNO = Convert.ToInt64(param[3]);
            //var data = context.Database.SqlQuery<VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN>("select * from VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN where CHAID=" + CHAID + " and SBMID=" + STFCORDID + " and SBDID=" + SBDID + "order by SBMID,SBDID").ToList();
            var data = context.Database.SqlQuery<VW_STUFFING_ORGSBILLNO_REFNO_CBX_ASSGN>("select * from VW_STUFFING_ORGSBILLNO_REFNO_CBX_ASSGN where CHAID=" + CHAID + " and SBMID=" + STFCORDID + " and SBDID=" + SBDID + " and ESBD_SBILLNO ='" + ORGSBNO + "' order by SBMID,SBDID").ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region seal auto detail
        public string Detail(string id)
        {
            var param = id.Split('-');
            var CHAID = Convert.ToInt32(param[0]);
            var STFMID = Convert.ToInt32(param[1]);

            var Cont_result = context.Database.SqlQuery<SP_STUFFING_SEAL_CONTAINER_NO_CBX_ASSGN_Result>("SP_STUFFING_SEAL_CONTAINER_NO_CBX_ASSGN @PCHAID= " + CHAID + ",@PSTFMID=" + STFMID + "").ToList();
            var data = context.Database.SqlQuery<VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN>("select * from VW_STUFFING_SBILLNO_REFNO_CBX_ASSGN where CHAID=" + CHAID + "order by SBMID,SBDID").ToList();
            var SealType = context.Database.SqlQuery<Export_Seal_Type_Master>("SELECT * FROM EXPORT_SEAL_TYPE_MASTER").ToList();
            var html = "";

            int count = 0;
            foreach (var cost in data)
            {
                int i = 0;

                html = html + "<tr><td><input type='text' value='0' id='STFDID' class='hide STFDID' name='STFDID'/><input type='text' value='0' id='STFPID' class='hide STFPID' name='STFPID'/>" + cost.SBMDNO + "<input type='text' value='" + cost.SBMDNO + "' id='STFCORDNO' class='hide form-control STFCORDNO' name='STFCORDNO' style='width:165px'><input type='text' value=" + cost.SBMID + " id='STFCORDID' class='hide form-control STFCORDID' name='STFCORDID' ><div class='stk'>C.O.Date:<input type='text' value='" + cost.SBMDATE.ToString("dd/MM/yyyy") + "' id='STFDSBDDATE' class='STFDSBDDATE medium' name='STFDSBDDATE' style='width:95px' readonly=readonly /></div></td><td>";

                html = html + "<input type=checkbox value='0' text=" + cost.SBDDNO + "  name=STFREFNO id=STFREFNO class=STFREFNO onchange='OnChangeCheckbox(this)'></td><td><input type='text' value='" + cost.SBDDNO + "' id='STFDSBDNO' class='hide form-control STFDSBDNO' name='STFDSBDNO' style='width:145px'/>" + cost.SBDDNO + "<input type='text' class='hide SBDID' id='SBDID' name='SBDID' value='" + cost.SBDID + "'><input type='text' class='hide booltype' id='booltype' name='booltype'> ";

                html = html + "<div class='stk'>R.Date:<input type='text' value='" + cost.SBDDATE.ToString("dd/MM/yyyy") + "' id='STFDSBDATE' class='STFDSBDATE medium' name='STFDSBDATE' style='width:125px' readonly=readonly/></div></td><td>" + cost.PRDTDESC + "<input type='text' value='" + cost.PRDTDESC + "' id='PRDTDESC' class='hide form-control PRDTDESC' name='PRDTDESC' readonly='readonly' style='width:185px'/><div class='stk'> Qty : <input type='text' value='" + cost.SBDNOP + "' id='QTY' class='QTY small' name='QTY' readonly='readonly' width='20px'>Bal :  <input type='text' value='" + cost.BNOP + "' id='BQTY' class='BQTY small' name='BQTY' readonly='readonly'></div></td><td>" + cost.ESBMDNO + "<input type='text' id='STFDSBNO' class='hide STFDSBNO form-control' name='STFDSBNO' value='" + cost.ESBMDNO + "' style='width:105px' readonly='readonly' /></br></br>" + cost.ESBMDATE.Value.ToString("dd/MM/yyyy") + "<input type='text' id='SBDDATE' value='" + cost.ESBMDATE.Value.ToString("dd/MM/yyyy") + "' class='hide SBDDATE form-control' name='SBDDATE' style='width:105px' readonly='readonly'/></div></td><td><select id='STFTYPE' class='form-control STFTYPE' name='detaildata[0].STFTYPE' style='width:145px'><option value=''>Select</option>";
                foreach (var stf in SealType)
                {
                    html = html + "<option value=" + stf.ESLTID + ">" + stf.ESLTDESC + "</option>";
                }

                html = html + "</select></td><td><select id='GIDID' class='GIDID' name='GIDID' onchange='sel_contno(this,&quot;CONTNRNO&quot;);'><option value=''>Select</option>";


                foreach (var cnt in Cont_result)
                {
                    html = html + "<option value=" + cnt.GIDID + ">" + cnt.CONTNRNO + "</option>";
                }

                html = html + "</select><input id='CONTNRNO' class='hide CONTNRNO' value='0' name='CONTNRNO'><div class='stk'>Size:<input type='text' id='CONTSIZE' class='CONTSIZE medium' name='CONTSIZE' style='width:145px' readonly='readonly' /></div> </td><td><input type='text' id='STFDNOP' class='form-control number STFDNOP' name='STFDNOP' style='width:80px' onchange='Check(this);' onkeypress='return isNumerDecimalOnly(event,this)'/></td><td><input type='text' id='STFDQTY' class='form-control wgtnumber STFDQTY' name='STFDQTY' style='width:80px' onkeypress='return isNumerDecimalOnly(event,this)'/></td></tr>";
                i++;
                count++;

            }
            html = html + "</table>";

            return html;
        }
        #endregion

        #region getting container size using(jquery code in form view) container id
        public JsonResult Cont_size(int id)
        {
            var GIDID = id;
            // var result = context.Database.SqlQuery<Int32>("select CONTNRSID from GATEINDETAIL where GIDID=" + GIDID).ToList();
            var result = context.Database.SqlQuery<string>("select CONTNRSDESC from GATEINDETAIL INNER JOIN CONTAINERSIZEMASTER ON GATEINDETAIL.CONTNRSID=CONTAINERSIZEMASTER.CONTNRSID where GIDID=" + GIDID).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region TEMP TABLE INSERT AND DELETE        
        public void TMP_INSERT(string ids)
        {
            var param = ids.Split(';');

            if (param[0] == "Select" || param[0] == "")
                param[0] = "0";
            if (param[1] == "Select" || param[1] == "")
                param[1] = "0";
            if (param[2] == "Select" || param[2] == "")
                param[2] = "0";

            context.Database.ExecuteSqlCommand("INSERT INTO TMP_STUFFING_SBDID(KUSRID,SBMID,SBDID,NOP) VALUES('" + Session["CUSRID"] + "'," + Convert.ToInt32(param[0]) + "," + Convert.ToInt32(param[1]) + "," + Convert.ToDecimal(param[2]) + ")");
            Response.Write("SAV");
        }
        public void TMP_INSERT_DEL()
        {
            context.Database.ExecuteSqlCommand("DELETE FROM TMP_STUFFING_SBDID WHERE KUSRID='" + Session["CUSRID"] + "'");
            Response.Write("SAV");
        }
        #endregion

        #region Print View
        [Authorize(Roles = "ExportSealPrint")]
        public void PrintView(int? id = 0)
        {
            String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
            SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);

            //  ........delete TMPRPT...//
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "SEALDETAIL", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;

                cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "Export_Stuffing.RPT");
                cryRpt.RecordSelectionFormula = "{VW_STUFFING_DETAIL_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_STUFFING_DETAIL_PRINT_ASSGN.STFMID} = " + id;

                crConnectionInfo.ServerName = stringbuilder.DataSource;
                crConnectionInfo.DatabaseName = stringbuilder.InitialCatalog;
                crConnectionInfo.UserID = stringbuilder.UserID;
                crConnectionInfo.Password = stringbuilder.Password;

                CrTables = cryRpt.Database.Tables;
                foreach (CrystalDecisions.CrystalReports.Engine.Table CrTable in CrTables)
                {
                    crtableLogoninfo = CrTable.LogOnInfo;
                    crtableLogoninfo.ConnectionInfo = crConnectionInfo;
                    CrTable.ApplyLogOnInfo(crtableLogoninfo);
                }

                cryRpt.ExportToHttpResponse(ExportFormatType.PortableDocFormat, System.Web.HttpContext.Current.Response, false, "");
                cryRpt.Dispose();
                cryRpt.Close();
            }

        }
        #endregion

        #region delete record
        [Authorize(Roles = "ExportSealDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");
            var param = id.Split('-');

            String temp = Delete_fun.delete_check1(fld, param[1]);

            if (temp.Equals("PROCEED"))
            {
                StuffingMaster stuffingmaster = context.stuffingmasters.Find(Convert.ToInt32(param[0]));
                context.stuffingmasters.Remove(stuffingmaster);
                context.SaveChanges();
                Response.Write("Deleted successfully...");
            }
            else
                Response.Write(temp);
        }
        #endregion

        // ========================= Edit Log Pages =========================
        public ActionResult EditLog()
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            return View();
        }

        public ActionResult EditLogSeal(int? stfmid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var list = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                string gidnoParam = stfmid.HasValue ? stfmid.Value.ToString() : null;
                
                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"SELECT TOP 2000 [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [Modules] = 'Seal'
                                                  AND (@GIDNO IS NULL OR [GIDNO] = @GIDNO)
                                                  AND (@FROM IS NULL OR [ChangedOn] >= @FROM)
                                                  AND (@TO   IS NULL OR [ChangedOn] <  DATEADD(day, 1, @TO))
                                                  AND (@USER IS NULL OR [ChangedBy] LIKE @USERPAT)
                                                  AND (@FIELD IS NULL OR [FieldName] LIKE @FIELDPAT)
                                                  AND (@VERSION IS NULL OR [Version] LIKE @VERPAT)
                                                  AND NOT (RTRIM(LTRIM([Version])) IN ('0','V0') OR LEFT(RTRIM(LTRIM([Version])),3) IN ('v0-','V0-'))
                                                ORDER BY [ChangedOn] DESC, [GIDNO] DESC", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", string.IsNullOrEmpty(gidnoParam) ? (object)DBNull.Value : gidnoParam);
                    cmd.Parameters.AddWithValue("@FROM", (object)from ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TO", (object)to ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@USER", string.IsNullOrWhiteSpace(user) ? (object)DBNull.Value : user);
                    cmd.Parameters.AddWithValue("@USERPAT", string.IsNullOrWhiteSpace(user) ? (object)DBNull.Value : (object)("%" + user + "%"));
                    cmd.Parameters.AddWithValue("@FIELD", string.IsNullOrWhiteSpace(fieldName) ? (object)DBNull.Value : fieldName);
                    cmd.Parameters.AddWithValue("@FIELDPAT", string.IsNullOrWhiteSpace(fieldName) ? (object)DBNull.Value : (object)("%" + fieldName + "%"));
                    cmd.Parameters.AddWithValue("@VERSION", string.IsNullOrWhiteSpace(version) ? (object)DBNull.Value : version);
                    cmd.Parameters.AddWithValue("@VERPAT", string.IsNullOrWhiteSpace(version) ? (object)DBNull.Value : (object)("%" + version + "%"));
                    sql.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new scfs_erp.Models.GateInDetailEditLogRow
                            {
                                GIDNO = Convert.ToString(r["GIDNO"]),
                                FieldName = Convert.ToString(r["FieldName"]),
                                OldValue = r["OldValue"] == DBNull.Value ? null : Convert.ToString(r["OldValue"]),
                                NewValue = r["NewValue"] == DBNull.Value ? null : Convert.ToString(r["NewValue"]),
                                ChangedBy = Convert.ToString(r["ChangedBy"]),
                                ChangedOn = r["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r["ChangedOn"]) : DateTime.MinValue,
                                Version = r["Version"] == DBNull.Value ? null : Convert.ToString(r["Version"]),
                                Modules = r["Modules"] == DBNull.Value ? null : Convert.ToString(r["Modules"])
                            });
                        }
                    }
                }
            }

            // Map raw DB codes to form-friendly display values
            try
            {
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictEOPT = context.Export_OperationTypeMaster.ToDictionary(x => x.EOPTID, x => x.EOPTDESC);
                var dictSlab = context.exportslabtypemaster.ToDictionary(x => x.SLABTID, x => x.SLABTDESC);

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrWhiteSpace(val)) return val;
                    try
                    {
                        int id;
                        if (field == "CHAID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "LCATEID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "STFBILLREFID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "EOPTID" && int.TryParse(val, out id) && dictEOPT.ContainsKey(id))
                            return dictEOPT[id];
                        if (field == "SLABTID" && int.TryParse(val, out id) && dictSlab.ContainsKey(id))
                            return dictSlab[id];
                    }
                    catch { }
                    return val;
                };

                Func<string, string> Friendly = field =>
                {
                    var fieldNameMap = GetSealFieldDisplayNames();
                    if (fieldNameMap.ContainsKey(field)) return fieldNameMap[field];
                    return field.Replace("_", " ").Trim();
                };

                foreach (var row in list)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
            }
            catch { /* Best-effort mapping */ }

            ViewBag.Module = "Seal";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        // Compare two versions for a given STFMID
        public ActionResult EditLogSealCompare(int? stfmid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            if (stfmid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide STFMID, Version A and Version B to compare.";
                return RedirectToAction("EditLogSeal", new { stfmid = stfmid });
            }

            versionA = (versionA ?? string.Empty).Trim();
            versionB = (versionB ?? string.Empty).Trim();
            string gidnoString = stfmid.HasValue ? stfmid.Value.ToString() : "";

            var baseLabel = "v0-" + gidnoString;
            if (string.Equals(versionA, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(versionA, "V0", StringComparison.OrdinalIgnoreCase) || string.Equals(versionA, "v0", StringComparison.OrdinalIgnoreCase))
                versionA = baseLabel;
            if (string.Equals(versionB, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(versionB, "V0", StringComparison.OrdinalIgnoreCase) || string.Equals(versionB, "v0", StringComparison.OrdinalIgnoreCase))
                versionB = baseLabel;

            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            var a = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var b = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [GIDNO]=@GIDNO AND [Modules]='Seal' AND RTRIM(LTRIM([Version]))=@V", sql))
                {
                    cmd.Parameters.Add("@GIDNO", System.Data.SqlDbType.NVarChar, 50);
                    cmd.Parameters.Add("@V", System.Data.SqlDbType.NVarChar, 100);

                    sql.Open();
                    cmd.Parameters["@GIDNO"].Value = gidnoString;
                    cmd.Parameters["@V"].Value = versionA.Trim();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            a.Add(new scfs_erp.Models.GateInDetailEditLogRow
                            {
                                GIDNO = gidnoString,
                                FieldName = Convert.ToString(r["FieldName"]),
                                OldValue = r["OldValue"] == DBNull.Value ? null : Convert.ToString(r["OldValue"]),
                                NewValue = r["NewValue"] == DBNull.Value ? null : Convert.ToString(r["NewValue"]),
                                ChangedBy = Convert.ToString(r["ChangedBy"]),
                                ChangedOn = r["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r["ChangedOn"]) : DateTime.MinValue,
                                Version = versionA,
                                Modules = Convert.ToString(r["Modules"])
                            });
                        }
                    }

                    cmd.Parameters["@V"].Value = versionB.Trim();
                    using (var r2 = cmd.ExecuteReader())
                    {
                        while (r2.Read())
                        {
                            b.Add(new scfs_erp.Models.GateInDetailEditLogRow
                            {
                                GIDNO = gidnoString,
                                FieldName = Convert.ToString(r2["FieldName"]),
                                OldValue = r2["OldValue"] == DBNull.Value ? null : Convert.ToString(r2["OldValue"]),
                                NewValue = r2["NewValue"] == DBNull.Value ? null : Convert.ToString(r2["NewValue"]),
                                ChangedBy = Convert.ToString(r2["ChangedBy"]),
                                ChangedOn = r2["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r2["ChangedOn"]) : DateTime.MinValue,
                                Version = versionB,
                                Modules = Convert.ToString(r2["Modules"])
                            });
                        }
                    }
                }
            }

            // Map values
            try
            {
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictEOPT = context.Export_OperationTypeMaster.ToDictionary(x => x.EOPTID, x => x.EOPTDESC);
                var dictSlab = context.exportslabtypemaster.ToDictionary(x => x.SLABTID, x => x.SLABTDESC);

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrWhiteSpace(val)) return val;
                    try
                    {
                        int id;
                        if (field == "CHAID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "LCATEID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "STFBILLREFID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "EOPTID" && int.TryParse(val, out id) && dictEOPT.ContainsKey(id))
                            return dictEOPT[id];
                        if (field == "SLABTID" && int.TryParse(val, out id) && dictSlab.ContainsKey(id))
                            return dictSlab[id];
                    }
                    catch { }
                    return val;
                };

                Func<string, string> Friendly = field =>
                {
                    var fieldNameMap = GetSealFieldDisplayNames();
                    if (fieldNameMap.ContainsKey(field)) return fieldNameMap[field];
                    return field.Replace("_", " ").Trim();
                };

                foreach (var row in a)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
                foreach (var row in b)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
            }
            catch { /* Best-effort mapping */ }

            ViewBag.GIDNO = gidnoString;
            ViewBag.VersionA = versionA;
            ViewBag.VersionB = versionB;
            ViewBag.RowsA = a;
            ViewBag.RowsB = b;
            ViewBag.Module = "Seal";
            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        private static Dictionary<string, string> GetSealFieldDisplayNames()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"STFMDATE", "Date"}, {"STFMTIME", "Date/Time"}, {"STFMDNO", "Seal Number"}, {"STFMNO", "No"},
                {"CHAID", "CHA"}, {"STFMNAME", "Name"}, {"LCATEID", "Labour Category"}, {"LCATENAME", "Labour Category Name"},
                {"EOPTID", "Operation Type"}, {"SLABTID", "Slab Type"}, {"STFBILLEDTO", "Billed To"},
                {"STFBILLREFID", "Bill Reference"}, {"STFBILLREFNAME", "Bill Reference Name"},
                {"STFCATEAID", "Location"}, {"STATEID", "State"}, {"CATEAGSTNO", "GST NO"},
                {"STF_SBILL_RNO", "Shipping Bill RNO"}, {"STF_FORM13_RNO", "Form 13 RNO"},
                {"TRANIMPADDR1", "Address 1"}, {"TRANIMPADDR2", "Address 2"}, {"TRANIMPADDR3", "Address 3"}, {"TRANIMPADDR4", "Address 4"},
                {"STFBCATEAID", "Billed CHA Location"}, {"STFBCHAGSTNO", "Billed CHA GST NO"}, {"STFBCHASTATEID", "Billed CHA State"},
                {"STFBCHAADDR1", "Billed CHA Address 1"}, {"STFBCHAADDR2", "Billed CHA Address 2"}, {"STFBCHAADDR3", "Billed CHA Address 3"}, {"STFBCHAADDR4", "Billed CHA Address 4"},
                {"DISPSTATUS", "Status"}, {"PRCSDATE", "Process Date"}, {"CUSRID", "Created By"}, {"LMUSRID", "Last Modified By"}
            };
        }

        // ========================= Edit Logging Helper Methods =========================
        private void LogSealEdits(StuffingMaster before, StuffingMaster after, string userId)
        {
            if (before == null || after == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "STFMID", "COMPYID", "PRCSDATE", "LMUSRID", "CUSRID", "STFTID", "TGID"
            };

            var gidno = after.STFMID.ToString();
            int nextVersion = 1;
            try
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(
                        MAX(TRY_CAST(
                            SUBSTRING([Version], 2, 
                                CASE WHEN CHARINDEX('-', [Version]) > 0 
                                     THEN CHARINDEX('-', [Version]) - 2 
                                     ELSE LEN([Version]) - 1
                                END
                            ) AS INT)
                        ), 0) + 1
                    FROM [dbo].[GateInDetailEditLog]
                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'Seal'", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", gidno);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        nextVersion = Convert.ToInt32(obj);
                }
            }
            catch { /* ignore logging version errors */ }

            var versionLabel = $"V{nextVersion}-{gidno}";
            
            var props = typeof(StuffingMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                var ov = p.GetValue(before, null);
                var nv = p.GetValue(after, null);

                if (BothNull(ov, nv)) continue;

                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                bool changed;

                if (type == typeof(decimal))
                {
                    var d1 = ToNullableDecimal(ov) ?? 0m;
                    var d2 = ToNullableDecimal(nv) ?? 0m;
                    changed = d1 != d2;
                }
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                {
                    var i1 = Convert.ToInt64(ov ?? 0);
                    var i2 = Convert.ToInt64(nv ?? 0);
                    changed = i1 != i2;
                }
                else if (type == typeof(DateTime))
                {
                    var t1 = ov != null && ov != DBNull.Value ? Convert.ToDateTime(ov) : DateTime.MinValue;
                    var t2 = nv != null && nv != DBNull.Value ? Convert.ToDateTime(nv) : DateTime.MinValue;
                    changed = t1 != t2;
                }
                else if (type == typeof(bool))
                {
                    var b1 = ov != null && ov != DBNull.Value && Convert.ToBoolean(ov);
                    var b2 = nv != null && nv != DBNull.Value && Convert.ToBoolean(nv);
                    changed = b1 != b2;
                }
                else if (type == typeof(string))
                {
                    var s1 = (Convert.ToString(ov) ?? string.Empty).Trim();
                    var s2 = (Convert.ToString(nv) ?? string.Empty).Trim();
                    bool def1 = string.IsNullOrEmpty(s1) || s1 == "-" || s1 == "0" || s1 == "0.0" || s1 == "0.00" || s1 == "0.000" || s1 == "0.0000";
                    bool def2 = string.IsNullOrEmpty(s2) || s2 == "-" || s2 == "0" || s2 == "0.0" || s2 == "0.00" || s2 == "0.000" || s2 == "0.0000";
                    if (def1 && def2) continue;
                    changed = !string.Equals(s1, s2, StringComparison.Ordinal);
                }
                else
                {
                    var s1 = FormatVal(ov);
                    var s2 = FormatVal(nv);
                    changed = !string.Equals(s1, s2, StringComparison.Ordinal);
                }

                if (!changed) continue;

                var os = FormatValForLogging(p.Name, ov);
                var ns = FormatValForLogging(p.Name, nv);

                InsertEditLogRow(cs.ConnectionString, gidno, p.Name, os, ns, userId, versionLabel, "Seal");
            }
        }

        private string FormatValForLogging(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            if (string.IsNullOrEmpty(formattedValue)) return formattedValue;

            try
            {
                int lookupId;
                if (fieldName == "CHAID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cate = context.categorymasters.FirstOrDefault(x => x.CATEID == lookupId);
                    if (cate != null) return cate.CATENAME;
                }
                else if (fieldName == "LCATEID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cate = context.categorymasters.FirstOrDefault(x => x.CATEID == lookupId);
                    if (cate != null) return cate.CATENAME;
                }
                else if (fieldName == "STFBILLREFID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cate = context.categorymasters.FirstOrDefault(x => x.CATEID == lookupId);
                    if (cate != null) return cate.CATENAME;
                }
                else if (fieldName == "EOPTID" && int.TryParse(formattedValue, out lookupId))
                {
                    var eopt = context.Export_OperationTypeMaster.FirstOrDefault(x => x.EOPTID == lookupId);
                    if (eopt != null) return eopt.EOPTDESC;
                }
                else if (fieldName == "SLABTID" && int.TryParse(formattedValue, out lookupId))
                {
                    var slab = context.exportslabtypemaster.FirstOrDefault(x => x.SLABTID == lookupId);
                    if (slab != null) return slab.SLABTDESC;
                }
            }
            catch { }

            return formattedValue;
        }

        private static string FormatVal(object v)
        {
            if (v == null || v == DBNull.Value) return string.Empty;
            if (v is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
            return Convert.ToString(v);
        }

        private static bool BothNull(object a, object b)
        {
            return (a == null || a == DBNull.Value) && (b == null || b == DBNull.Value);
        }

        private static decimal? ToNullableDecimal(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            if (decimal.TryParse(Convert.ToString(v), out decimal d)) return d;
            return null;
        }

        private static void InsertEditLogRow(string connectionString, string gidno, string fieldName, string oldValue, string newValue, string changedBy, string versionLabel, string modules)
        {
            try
            {
                using (var sql = new SqlConnection(connectionString))
                {
                    sql.Open();
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO [dbo].[GateInDetailEditLog] ([GIDNO], [FieldName], [OldValue], [NewValue], [ChangedBy], [ChangedOn], [Version], [Modules])
                        VALUES (@GIDNO, @FieldName, @OldValue, @NewValue, @ChangedBy, GETDATE(), @Version, @Modules)", sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO", gidno ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FieldName", fieldName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@OldValue", string.IsNullOrEmpty(oldValue) ? (object)DBNull.Value : oldValue);
                        cmd.Parameters.AddWithValue("@NewValue", string.IsNullOrEmpty(newValue) ? (object)DBNull.Value : newValue);
                        cmd.Parameters.AddWithValue("@ChangedBy", string.IsNullOrEmpty(changedBy) ? (object)DBNull.Value : changedBy);
                        cmd.Parameters.AddWithValue("@Version", versionLabel ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Modules", modules ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InsertEditLogRow failed: {ex.Message}");
            }
        }

        private void EnsureBaselineVersionZero(StuffingMaster snapshot, string userId)
        {
            if (snapshot == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            var gidno = snapshot.STFMID.ToString();
            var baselineVer = "v0-" + gidno;

            try
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    using (var cmd = new SqlCommand(@"
                        IF NOT EXISTS (
                            SELECT 1 FROM [dbo].[GateInDetailEditLog]
                            WHERE [GIDNO] = @GIDNO AND [Modules] = 'Seal' AND RTRIM(LTRIM([Version])) = @VERSION
                        )
                        BEGIN
                            INSERT INTO [dbo].[GateInDetailEditLog] ([GIDNO], [FieldName], [OldValue], [NewValue], [ChangedBy], [ChangedOn], [Version], [Modules])
                            SELECT @GIDNO, 'INITIAL', NULL, 'Initial State', @USER, GETDATE(), @VERSION, 'Seal'
                        END", sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO", gidno);
                        cmd.Parameters.AddWithValue("@VERSION", baselineVer);
                        cmd.Parameters.AddWithValue("@USER", userId ?? string.Empty);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* ignore baseline creation errors */ }
        }
    }
}