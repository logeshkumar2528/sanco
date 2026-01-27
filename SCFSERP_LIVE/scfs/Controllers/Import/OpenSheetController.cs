using scfs.Data;
using scfs_erp.Context;
using scfs_erp.Helper;
using scfs_erp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.SqlClient;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Configuration;
using System.Data.Entity;
using System.Reflection;

namespace scfs_erp.Controllers
{
    [SessionExpire]
    public class OpenSheetController : Controller
    {
        SCFSERPContext context = new SCFSERPContext();
        public static String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
        SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);
        [Authorize(Roles = "OpenSheetIndex")]
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
            DateTime sd = Convert.ToDateTime(Session["SDATE"]).Date;
            DateTime ed = Convert.ToDateTime(Session["EDATE"]).Date;
                  
                
            return View(context.opensheetmasters.Where(x => x.OSMDATE >= sd).Where(x => x.OSMDATE <= ed).ToList());
        }//........end of index grid
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {

            using (var e = new CFSImportEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                var data = e.pr_SearchOpenSheetGridAssgn(param.sSearch,
                                                Convert.ToInt32(Request["iSortCol_0"]),
                                                Request["sSortDir_0"],
                                                param.iDisplayStart,
                                                param.iDisplayStart + param.iDisplayLength,
                                                totalRowsCount,
                                                filteredRowsCount, Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]), Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]), Convert.ToString(System.Web.HttpContext.Current.Session["CUSRID"]),0);

                var aaData = data.Select(d => new string[] { d.OSMDATE.Value.ToString("dd/MM/yyyy"), d.OSMDNO, d.OSMNAME, d.OSMVSLNAME, d.OSMIGMNO, d.OSMLNO, d.BOENO, d.BOEDATE.Value.ToString("dd/MM/yyyy"), d.DISPSTATUS, d.OSMID.ToString(), d.OOCNOSTS.ToString(), d.DOSTS.ToString() }).ToArray();

                return Json(new
                {
                    sEcho = param.sEcho,
                    aaData = aaData,
                    iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                    iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                }, JsonRequestBehavior.AllowGet);
            }
        }

        //[Authorize(Roles = "OpenSheetEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/OpenSheet/Form/" + id);
        }

        //[Authorize(Roles = "OpenSheetEdit")]
        public void SEdit(int id)
        {
            Response.Redirect("/OpenSheet/SForm/" + id);
        }

        //[Authorize(Roles = "OpenSheetEdit")]
        public ActionResult SForm(int id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            OpenSheetMD vm = new OpenSheetMD();
            OpenSheetMaster master = new OpenSheetMaster();

            ViewBag.OSMLCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            ViewBag.OSMUNITID = new SelectList(context.unitmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.UNITDESC), "UNITID", "UNITDESC", 2);
            //  ViewBag.OSMLNO = new SelectList(context.gateindetails, "GPLNO", "GPLNO");
            ViewBag.OSMTYPE = new SelectList(context.opensheetviadetails, "OSMTYPE", "OSMTYPEDESC");

            ViewBag.OSMLNO = new SelectList("");

            ViewBag.OSBCHACATEAID = new SelectList("");

            ViewBag.OSBBCHACATEAID = new SelectList("");
            //------------------------------DOTYPE-------------------------
            List<SelectListItem> selectedDOTYPE = new List<SelectListItem>();
            SelectListItem selectedItem1 = new SelectListItem { Text = "No", Value = "0", Selected = false };
            selectedDOTYPE.Add(selectedItem1);
            selectedItem1 = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
            selectedDOTYPE.Add(selectedItem1);
            ViewBag.DOTYPE = selectedDOTYPE;

            //------------------------------SealCut-------------------------
            List<SelectListItem> selectedScut = new List<SelectListItem>();
            SelectListItem selectedItem = new SelectListItem { Text = "No", Value = "0", Selected = true };
            selectedScut.Add(selectedItem);
            selectedItem = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
            selectedScut.Add(selectedItem);
            ViewBag.SCTYPE = selectedScut;

            //------------------------------INSPTYPE-------------------------
            List<SelectListItem> selectedINSPTYPE = new List<SelectListItem>();
            SelectListItem selectedItemINS = new SelectListItem { Text = "No", Value = "0", Selected = false };
            selectedINSPTYPE.Add(selectedItemINS);
            selectedItemINS = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
            selectedINSPTYPE.Add(selectedItemINS);
            ViewBag.INSP_TYPE = selectedINSPTYPE;


            //-------------------------------DISPSTATUS----

            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "INBOOKS", Value = "0", Selected = false };
            selectedDISPSTATUS.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "CANCELLED", Value = "1", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDSP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;


            //-------------------------------container type----

            List<SelectListItem> selectedcontainertype = new List<SelectListItem>();
            SelectListItem selectedcontainer = new SelectListItem { Text = "FCL", Value = "0", Selected = true };
            selectedcontainertype.Add(selectedcontainer);
            // selectedcontainer = new SelectListItem { Text = "LCL", Value = "1", Selected = false };
            //  selectedcontainertype.Add(selectedcontainer);
            ViewBag.OSMLDTYPE = selectedcontainertype;



            //BILLED TO
            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst1 = new List<SelectListItem>();
            SelectListItem selectedItemtax1 = new SelectListItem { Text = "IMPORTER", Value = "1", Selected = false };
            selectedtaxlst1.Add(selectedItemtax1);
            selectedItemtax1 = new SelectListItem { Text = "CHA", Value = "0", Selected = true };
            selectedtaxlst1.Add(selectedItemtax1);
            ViewBag.BILLEDTO = selectedtaxlst1;



            //-------------opensheetdetails---------------

            if (id != 0)
            {
                master = context.opensheetmasters.Find(id);//find selected record

                vm.masterdata = context.opensheetmasters.Where(det => det.OSMID == id).ToList();
                vm.detaildata = context.opensheetdetails.Where(det => det.OSMID == id).ToList();



                //---------Dropdown lists-------------------
                ViewBag.OSMLCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME", master.OSMLCATEID);
                ViewBag.OSMUNITID = new SelectList(context.unitmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.UNITDESC), "UNITID", "UNITDESC", master.OSMUNITID);
                ViewBag.OSMLNO = new SelectList(context.gateindetails, "GPLNO", "GPLNO", master.OSMLNO);
                ViewBag.OSMTYPE = new SelectList(context.opensheetviadetails, "OSMTYPE", "OSMTYPEDESC", master.OSMTYPE);
                int chaid = Convert.ToInt32(vm.masterdata[0].CHAID);
                int chaaid = Convert.ToInt32(vm.masterdata[0].OSBCHACATEAID);
                ViewBag.OSBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", chaaid).ToList();
                chaid = Convert.ToInt32(vm.masterdata[0].OSBILLREFID);
                chaaid = Convert.ToInt32(vm.masterdata[0].OSBBCHACATEAID);
                ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", chaaid).ToList();


                var sql5 = context.Database.SqlQuery<CategoryMaster>("select * from CategoryMaster where CATEID=" + master.OSBILLREFID).ToList();
                ViewBag.OSBILLREFNAME = sql5[0].CATENAME;
                ViewBag.OSBILLGSTNO = sql5[0].CATEBGSTNO;


                //--------End of dropdown


                //------------------------------DOTYPE-------------------------
                List<SelectListItem> selectedDOTYPE1 = new List<SelectListItem>();
                if (Convert.ToInt16(master.DOTYPE) == 0)
                {
                    SelectListItem selectedItemDO = new SelectListItem { Text = "No", Value = "0", Selected = true };
                    selectedDOTYPE1.Add(selectedItemDO);
                    selectedItemDO = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
                    selectedDOTYPE1.Add(selectedItemDO);
                    ViewBag.DOTYPE = selectedDOTYPE1;
                }
                else
                {
                    SelectListItem selectedItemDO = new SelectListItem { Text = "No", Value = "0", Selected = false };
                    selectedDOTYPE1.Add(selectedItemDO);
                    selectedItemDO = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
                    selectedDOTYPE1.Add(selectedItemDO);
                    ViewBag.DOTYPE = selectedDOTYPE1;

                }

                //------------------------------SealCut-------------------------
                List<SelectListItem> selectedScut1 = new List<SelectListItem>();
                if (Convert.ToInt16(master.SCTYPE) == 0)
                {
                    SelectListItem selectedItemSC = new SelectListItem { Text = "No", Value = "0", Selected = true };
                    selectedScut1.Add(selectedItemSC);
                    selectedItemSC = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
                    selectedScut1.Add(selectedItemSC);
                    ViewBag.SCTYPE = selectedScut1;
                }
                else
                {
                    SelectListItem selectedItemSC = new SelectListItem { Text = "No", Value = "0", Selected = false };
                    selectedScut1.Add(selectedItemSC);
                    selectedItemSC = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
                    selectedScut1.Add(selectedItemSC);
                    ViewBag.SCTYPE = selectedScut1;

                }

                if (vm.detaildata.Count == 0)
                {
                    ViewBag.INSP_TYPE = selectedINSPTYPE;
                }
                else
                {
                    List<SelectListItem> selectedINSPTYPE1 = new List<SelectListItem>();
                    if (Convert.ToInt16(vm.detaildata[0].INSPTYPE) == 0)
                    {
                        SelectListItem selectedItemINS1 = new SelectListItem { Text = "No", Value = "0", Selected = true };
                        selectedINSPTYPE1.Add(selectedItemINS1);
                        //selectedItemINS1 = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
                        //selectedINSPTYPE1.Add(selectedItemINS1);
                        ViewBag.INSP_TYPE = selectedINSPTYPE1;
                    }
                    else
                    {
                        SelectListItem selectedItemINS1 = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
                        selectedINSPTYPE1.Add(selectedItemINS1);
                        //selectedItemINS1 = new SelectListItem { Text = "No", Value = "0", Selected = false };
                        //selectedINSPTYPE1.Add(selectedItemINS1);
                        ViewBag.INSP_TYPE = selectedINSPTYPE1;
                    }
                }

                //------------------------------INSPTYPE-------------------------
                

                //-------------------------------container type----

                List<SelectListItem> selectedcontainertype_ = new List<SelectListItem>();
                if (master.OSMLDTYPE == 1)
                {
                    SelectListItem selectedcontainer_ = new SelectListItem { Text = "FCL", Value = "0", Selected = false };
                    selectedcontainertype_.Add(selectedcontainer_);
                    selectedcontainer_ = new SelectListItem { Text = "LCL", Value = "1", Selected = true };
                    selectedcontainertype_.Add(selectedcontainer_);
                    ViewBag.OSMLDTYPE = selectedcontainertype_;
                }


                //BILLED TO 

                List<SelectListItem> selectedcontainertype01 = new List<SelectListItem>();
                if (master.OSBILLEDTO == 1)
                {
                    SelectListItem selectedcontainer01 = new SelectListItem { Text = "IMPORTER", Value = "1", Selected = true };
                    selectedcontainertype01.Add(selectedcontainer01);
                    selectedcontainer01 = new SelectListItem { Text = "CHA", Value = "0", Selected = false };
                    selectedcontainertype01.Add(selectedcontainer01);
                    ViewBag.BILLEDTO = selectedcontainertype01;
                }


                //----------------To Display GateInDetails-----------------------

                //GateInDetail gdet = context.gateindetails.Find(Convert.ToInt32(vm.detaildata[0].GIDID));
                //if (vm.detaildata[0].GIDID == gdet.GIDID)
                //{
                //    ViewBag.VOYNO = gdet.VOYNO;
                //    ViewBag.IMPRTNAME = gdet.IMPRTNAME;
                //    ViewBag.STMRNAME = gdet.STMRNAME;

                //}//------End


            }//---End Of IF


            return View(vm);

        }

        [Authorize(Roles = "ImportInvoiceNameUpdate")]
        public ActionResult TCHForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var ids = Convert.ToInt32(id);
            //var gstamt = Convert.ToInt32(param[1]);

            

            ViewBag.TRANMID = 0;
            ViewBag.FGSTAMT = 0;//gstamt;

            ViewBag.OSMID = ids;
            ViewBag.OSMNAME = "";
            ViewBag.CHAID = 0;
            ViewBag.OSBCHACATEAGSTNO = "";
            ViewBag.OSBCHASTATEID = 0;
            ViewBag.OSBCHAADDR1 = "";
            ViewBag.OSBCHAADDR2 = "";
            ViewBag.OSBCHAADDR3 = "";
            ViewBag.OSBCHAADDR4 = "";
            ViewBag.OSBCHA_CATEID = 0;

            var squer = context.Database.SqlQuery<OpenSheetMaster>("select *from  OPENSHEETMASTER  where OSMID=" + ids).ToList();

            if (squer.Count > 0)
            {

                ViewBag.OSMID = squer[0].OSMID;
                ViewBag.OSMNAME = squer[0].OSMNAME;
                ViewBag.CHAID = squer[0].CHAID;

                int chaid = Convert.ToInt32(squer[0].CHAID);
                int chaaid = Convert.ToInt32(squer[0].OSBCHACATEAID);

                if (chaaid > 0)
                {
                    var adds = context.Database.SqlQuery<Category_Address_Details>("Select *From CATEGORY_ADDRESS_DETAIL Where CATEAID  = " + chaaid + " ORDER By  CATEAID DESC").ToList();
                    ViewBag.OSBCHACATEAGSTNO = adds[0].CATEAGSTNO;
                    ViewBag.OSBCHASTATEID = adds[0].STATEID;
                    ViewBag.OSBCHAADDR1 = adds[0].CATEAADDR1;
                    ViewBag.OSBCHAADDR2 = adds[0].CATEAADDR2;
                    ViewBag.OSBCHAADDR3 = adds[0].CATEAADDR3;
                    ViewBag.OSBCHAADDR4 = adds[0].CATEAADDR4;
                    ViewBag.OSBCHA_CATEAID = adds[0].CATEAID;

                    var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + adds[0].STATEID).ToList();
                    if (starqy.Count > 0)
                    {
                        ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
                        ViewBag.STATETYPE = starqy[0].STATETYPE;
                    }

                    ViewBag.OSBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", adds[0].CATEAID).ToList();
                }
                else
                {
                    var adds = context.Database.SqlQuery<Category_Address_Details>("Select Top 1 *From CATEGORY_ADDRESS_DETAIL Where CATEID  = " + chaid + " ORDER By  CATEAID DESC").ToList();
                    ViewBag.OSBCHACATEAGSTNO = adds[0].CATEAGSTNO;
                    ViewBag.OSBCHASTATEID = adds[0].STATEID;
                    ViewBag.OSBCHAADDR1 = adds[0].CATEAADDR1;
                    ViewBag.OSBCHAADDR2 = adds[0].CATEAADDR2;
                    ViewBag.OSBCHAADDR3 = adds[0].CATEAADDR3;
                    ViewBag.OSBCHAADDR4 = adds[0].CATEAADDR4;
                    ViewBag.OSBCHA_CATEAID = adds[0].CATEAID;

                    var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + adds[0].STATEID).ToList();
                    if (starqy.Count > 0)
                    {
                        ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
                        ViewBag.STATETYPE = starqy[0].STATETYPE;
                    }

                    ViewBag.OSBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC").ToList();
                }

            }

            return View();
        }

        //[Authorize(Roles = "ImportOpsheetBoenoUpdate")] 
        public ActionResult OSBOEForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var ids = Convert.ToInt32(id);
            //var gstamt = Convert.ToInt32(param[1]);



            ViewBag.TRANMID = 0;
            ViewBag.FGSTAMT = 0;//gstamt;

            ViewBag.OSMID = ids;
            ViewBag.OSMNAME = "";
            ViewBag.CHAID = 0;
            ViewBag.OSMDATE = "";
            ViewBag.OSMDNO = "";
            ViewBag.OSP_BOENO = "";
            ViewBag.BOEDATE = "";

            var squer = context.Database.SqlQuery<OpenSheetMaster>("select *from  OPENSHEETMASTER  where OSMID=" + ids).ToList();

            if (squer.Count > 0)
            {

                ViewBag.OSMID = squer[0].OSMID;
                ViewBag.OSMNAME = squer[0].OSMNAME;
                ViewBag.CHAID = squer[0].CHAID;
                ViewBag.OSMDATE = squer[0].OSMDATE;
                ViewBag.OSMDNO = squer[0].OSMDNO;
                ViewBag.OSP_BOENO = squer[0].BOENO;
                ViewBag.BOEDATE = Convert.ToDateTime(squer[0].BOEDATE).Date.ToString("dd/MM/yyyy");

            }

            return View();
        }

        public ActionResult OSBLForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var ids = Convert.ToInt32(id);
            //var gstamt = Convert.ToInt32(param[1]);



            ViewBag.TRANMID = 0;
            ViewBag.FGSTAMT = 0;//gstamt;

            ViewBag.OSMID = ids;
            ViewBag.OSMNAME = "";
            ViewBag.CHAID = 0;
            ViewBag.OSMDATE = "";
            ViewBag.OSMDNO = "";
            ViewBag.OSP_BOENO = "";

            var squer = context.Database.SqlQuery<OpenSheetMaster>("select *from  OPENSHEETMASTER  where OSMID=" + ids).ToList();

            if (squer.Count > 0)
            {

                ViewBag.OSMID = squer[0].OSMID;
                ViewBag.OSMNAME = squer[0].OSMNAME;
                ViewBag.CHAID = squer[0].CHAID;
                ViewBag.OSMDATE = squer[0].OSMDATE;
                ViewBag.OSMDNO = squer[0].OSMDNO;
                ViewBag.OSP_BLNO = squer[0].OSMBLNO;
                
            }

            return View();
        }


        [Authorize(Roles = "OpenSheetIndex")]        
        public ActionResult OOCForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var ids = Convert.ToInt32(id);
            //var gstamt = Convert.ToInt32(param[1]);



            ViewBag.TRANMID = 0;
            ViewBag.FGSTAMT = 0;//gstamt;

            ViewBag.OSMID = ids;
            ViewBag.OSMNAME = "";
            ViewBag.CHAID = 0;
            ViewBag.OSMDATE = "";
            ViewBag.OSMDNO = "";
            ViewBag.OOCNO = "";
            ViewBag.OOCDATE = "";

            var squer = context.Database.SqlQuery<OpenSheetMaster>("select *from  OPENSHEETMASTER  where OSMID=" + ids).ToList();

            if (squer.Count > 0)
            {

                ViewBag.OSMID = squer[0].OSMID;
                ViewBag.OSMNAME = squer[0].OSMNAME;
                ViewBag.CHAID = squer[0].CHAID;
                ViewBag.OSMDATE = squer[0].OSMDATE;
                ViewBag.OSMDNO = squer[0].OSMDNO;
                ViewBag.OOCNO = squer[0].OOCNO;
                ViewBag.OOCDATE = "";
                var aa = Session["Group"].ToString();
                if (Session["Group"].ToString() == "Imports" || Session["Group"].ToString()=="SuperAdmin" || Session["Group"].ToString() == "GroupAdmin" || Session["Group"].ToString() == "Admin")
                {
                    if (squer[0].OOCDATE != null)
                    {
                        ViewBag.OOCDATE = Convert.ToDateTime(squer[0].OOCDATE).ToString("dd/MM/yyyy");
                    }
                }
                                 

            }

            return View();
        }
        [HttpPost]
        public ActionResult oocsavedata(FormCollection tab)
        {
            string status = "";
            int osmid = Convert.ToInt32(tab["OSMID"]);
            
            // Load BEFORE state
            var before = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
            
            using (var trans = context.Database.BeginTransaction())
            {
                try
                {
                    string OOCNO = Convert.ToString(tab["OOCNO"]);
                    string OOCDATE = Convert.ToString(tab["OOCDATE"]);

                    string upquery = " UPDATE OPENSHEETMASTER SET OOCNO='" + OOCNO + "', OOCDATE = '" + Convert.ToDateTime(OOCDATE).ToString("yyyy-MM-dd") + "'  WHERE OSMID = " + Convert.ToInt32(osmid) + "";
                    context.Database.ExecuteSqlCommand(upquery);

                    status = "saved";
                    trans.Commit();
                    
                    // Load AFTER state and log changes
                    if (before != null)
                    {
                        var after = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
                        if (after != null)
                        {
                            int nextVersion = CalculateNextVersion(after.OSMDNO);
                            LogOpenSheetEdits(before, after, Session["CUSRID"].ToString(), nextVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }
        
        [Authorize(Roles = "OpenSheetIndex")]
        public ActionResult DOVForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var ids = Convert.ToInt32(id);
            //var gstamt = Convert.ToInt32(param[1]);



            ViewBag.TRANMID = 0;
            ViewBag.FGSTAMT = 0;//gstamt;

            ViewBag.OSMID = ids;
            ViewBag.OSMNAME = "";
            ViewBag.CHAID = 0;
            ViewBag.OSMDATE = "";
            ViewBag.OSMDNO = "";            
            ViewBag.DODATE = "";

            var squer = context.Database.SqlQuery<OpenSheetMaster>("select *from  OPENSHEETMASTER  where OSMID=" + ids).ToList();

            if (squer.Count > 0)
            {

                ViewBag.OSMID = squer[0].OSMID;
                ViewBag.OSMNAME = squer[0].OSMNAME;
                ViewBag.CHAID = squer[0].CHAID;
                ViewBag.OSMDATE = squer[0].OSMDATE;
                ViewBag.OSMDNO = squer[0].OSMDNO;
                if (Session["Group"].ToString() == "Imports" || Session["Group"].ToString() == "SuperAdmin" || Session["Group"].ToString() == "GroupAdmin" || Session["Group"].ToString() == "Admin")
                    ViewBag.DODATE = Convert.ToDateTime(squer[0].DODATE).ToString("dd/MM/yyyy");

            }

            return View();
        }

        [HttpPost]
        public ActionResult dovdsavedata(FormCollection tab)
        {
            string status = "";
            int osmid = Convert.ToInt32(tab["OSMID"]);
            
            // Load BEFORE state
            var before = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
            
            using (var trans = context.Database.BeginTransaction())
            {
                try
                {
                    string DODATE = Convert.ToString(tab["DODATE"]);

                    string upquery = " UPDATE OPENSHEETMASTER SET DODATE = '" + Convert.ToDateTime(DODATE).ToString("yyyy-MM-dd") + "'  WHERE OSMID = " + Convert.ToInt32(osmid) + "";
                    context.Database.ExecuteSqlCommand(upquery);

                    status = "saved";
                    trans.Commit();
                    
                    // Load AFTER state and log changes
                    if (before != null)
                    {
                        var after = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
                        if (after != null)
                        {
                            int nextVersion = CalculateNextVersion(after.OSMDNO);
                            LogOpenSheetEdits(before, after, Session["CUSRID"].ToString(), nextVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        //..............form data..............//
        //[Authorize(Roles = "OpenSheetCreate")]
        public ActionResult Form(int id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            OpenSheetMD vm = new OpenSheetMD();
            OpenSheetMaster master = new OpenSheetMaster();

            ViewBag.OSMLCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            //ViewBag.OSMUNITID = new SelectList(context.unitmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.UNITDESC), "UNITID", "UNITDESC",2);
            ViewBag.OSMUNITID = new SelectList(context.unitmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.UNITCODE), "UNITID", "UNITCODE", 2);
            //  ViewBag.OSMLNO = new SelectList(context.gateindetails, "GPLNO", "GPLNO");
            ViewBag.OSMTYPE = new SelectList(context.opensheetviadetails, "OSMTYPE", "OSMTYPEDESC");
            ViewBag.T_PRDTGID = new SelectList(context.productgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.PRDTGDESC), "PRDTGID", "PRDTGDESC");
            ViewBag.OSMLNO = new SelectList("");

            ViewBag.OSBCHACATEAID = new SelectList("");

            ViewBag.OSBBCHACATEAID = new SelectList("");
            //------------------------------DOTYPE-------------------------
            List<SelectListItem> selectedDOTYPE = new List<SelectListItem>();
            SelectListItem selectedItem1 = new SelectListItem { Text = "No", Value = "0", Selected = false };
            selectedDOTYPE.Add(selectedItem1);
            selectedItem1 = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
            selectedDOTYPE.Add(selectedItem1);
            ViewBag.DOTYPE = selectedDOTYPE;

            //------------------------------SealCut-------------------------
            List<SelectListItem> selectedScut = new List<SelectListItem>();
            SelectListItem selectedItem = new SelectListItem { Text = "No", Value = "0", Selected = true };
            selectedScut.Add(selectedItem);
            selectedItem = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
            selectedScut.Add(selectedItem);
            ViewBag.SCTYPE = selectedScut;

            //------------------------------INSPTYPE-------------------------
            List<SelectListItem> selectedINSPTYPE = new List<SelectListItem>();
            SelectListItem selectedItemINS = new SelectListItem { Text = "No", Value = "0", Selected = false };
            selectedINSPTYPE.Add(selectedItemINS);
            selectedItemINS = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
            selectedINSPTYPE.Add(selectedItemINS);
            ViewBag.INSP_TYPE = selectedINSPTYPE;


            //-------------------------------DISPSTATUS----

            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "INBOOKS", Value = "0", Selected = false };
            selectedDISPSTATUS.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "CANCELLED", Value = "1", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDSP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;


            //-------------------------------container type----

            List<SelectListItem> selectedcontainertype = new List<SelectListItem>();
            SelectListItem selectedcontainer = new SelectListItem { Text = "FCL", Value = "0", Selected = true };
            selectedcontainertype.Add(selectedcontainer);
            // selectedcontainer = new SelectListItem { Text = "LCL", Value = "1", Selected = false };
            //  selectedcontainertype.Add(selectedcontainer);
            ViewBag.OSMLDTYPE = selectedcontainertype;



            //BILLED TO
            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst1 = new List<SelectListItem>();
            SelectListItem selectedItemtax1 = new SelectListItem { Text = "IMPORTER", Value = "1", Selected = false };
            selectedtaxlst1.Add(selectedItemtax1);
            selectedItemtax1 = new SelectListItem { Text = "CHA", Value = "0", Selected = true };
            selectedtaxlst1.Add(selectedItemtax1);
            ViewBag.BILLEDTO = selectedtaxlst1;



            //-------------opensheetdetails---------------

            if (id != 0)
            {
                master = context.opensheetmasters.Find(id);//find selected record

                vm.masterdata = context.opensheetmasters.Where(det => det.OSMID == id).ToList();
                vm.detaildata = context.opensheetdetails.Where(det => det.OSMID == id).ToList();
                int OSMID = Convert.ToInt32(id);

                vm.boedetaildata = context.VW_OPENSHEET_BILL_ENTRY_MID_ASSGNs.Where(det => det.OSMID == OSMID).ToList();


                //---------Dropdown lists-------------------
                ViewBag.OSMLCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME", master.OSMLCATEID);
                ViewBag.OSMUNITID = new SelectList(context.unitmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.UNITCODE), "UNITID", "UNITCODE", master.OSMUNITID);
                //ViewBag.OSMUNITID = new SelectList(context.unitmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.UNITDESC), "UNITID", "UNITDESC", master.OSMUNITID);
                ViewBag.OSMLNO = new SelectList(context.gateindetails, "GPLNO", "GPLNO", master.OSMLNO);
                ViewBag.OSMTYPE = new SelectList(context.opensheetviadetails, "OSMTYPE", "OSMTYPEDESC", master.OSMTYPE);
                int chaid = Convert.ToInt32(vm.masterdata[0].CHAID);
                int chaaid = Convert.ToInt32(vm.masterdata[0].OSBCHACATEAID);
                ViewBag.OSBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", chaaid).ToList();
                int bchaid = Convert.ToInt32(vm.masterdata[0].OSBILLREFID);
                int bchaaid = Convert.ToInt32(vm.masterdata[0].OSBBCHACATEAID);
                ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == bchaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", bchaaid).ToList();
                

                var sql5 = context.Database.SqlQuery<CategoryMaster>("select * from CategoryMaster where CATEID=" + master.OSBILLREFID).ToList();
                ViewBag.OSBILLREFNAME = sql5[0].CATENAME;
                ViewBag.OSBILLGSTNO = sql5[0].CATEBGSTNO;

                ViewBag.T_PRDTGID = new SelectList(context.productgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.PRDTGDESC), "PRDTGID", "PRDTGDESC");
                //--------End of dropdown


                //------------------------------DOTYPE-------------------------
                List<SelectListItem> selectedDOTYPE1 = new List<SelectListItem>();
                if (Convert.ToInt16(master.DOTYPE) == 0)
                {
                    SelectListItem selectedItemDO = new SelectListItem { Text = "No", Value = "0", Selected = true };
                    selectedDOTYPE1.Add(selectedItemDO);
                    selectedItemDO = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
                    selectedDOTYPE1.Add(selectedItemDO);
                    ViewBag.DOTYPE = selectedDOTYPE1;
                }

                //------------------------------SealCut-------------------------
                List<SelectListItem> selectedScut1 = new List<SelectListItem>();
                if (Convert.ToInt16(master.SCTYPE) == 0)
                {
                    SelectListItem selectedItemSC = new SelectListItem { Text = "No", Value = "0", Selected = true };
                    selectedScut1.Add(selectedItemSC);
                    selectedItemSC = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
                    selectedScut1.Add(selectedItemSC);
                    ViewBag.SCTYPE = selectedScut1;
                }


                //------------------------------INSPTYPE-------------------------
                List<SelectListItem> selectedINSPTYPE1 = new List<SelectListItem>();
                if (vm.detaildata != null)
                {
                    if (Convert.ToInt16(vm.detaildata[0].INSPTYPE) == 0)
                    {
                        SelectListItem selectedItemINS1 = new SelectListItem { Text = "No", Value = "0", Selected = true };
                        selectedINSPTYPE1.Add(selectedItemINS1);
                        selectedItemINS1 = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
                        selectedINSPTYPE1.Add(selectedItemINS1);
                        ViewBag.INSP_TYPE = selectedINSPTYPE1;
                    }
                    else {
                        
                            SelectListItem selectedItemINS1 = new SelectListItem { Text = "No", Value = "0", Selected = false };
                            selectedINSPTYPE1.Add(selectedItemINS1);
                            selectedItemINS1 = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
                            selectedINSPTYPE1.Add(selectedItemINS1);
                            ViewBag.INSP_TYPE = selectedINSPTYPE1;
                        
                    }
                }
                
                //-------------------------------container type----

                List<SelectListItem> selectedcontainertype_ = new List<SelectListItem>();
                if (master.OSMLDTYPE == 1)
                {
                    SelectListItem selectedcontainer_ = new SelectListItem { Text = "FCL", Value = "0", Selected = false };
                    selectedcontainertype_.Add(selectedcontainer_);
                    selectedcontainer_ = new SelectListItem { Text = "LCL", Value = "1", Selected = true };
                    selectedcontainertype_.Add(selectedcontainer_);
                    ViewBag.OSMLDTYPE = selectedcontainertype_;
                }


                //BILLED TO 

                List<SelectListItem> selectedcontainertype01 = new List<SelectListItem>();
                if (master.OSBILLEDTO == 1)
                {
                    SelectListItem selectedcontainer01 = new SelectListItem { Text = "IMPORTER", Value = "1", Selected = true };
                    selectedcontainertype01.Add(selectedcontainer01);
                    selectedcontainer01 = new SelectListItem { Text = "CHA", Value = "0", Selected = false };
                    selectedcontainertype01.Add(selectedcontainer01);
                    ViewBag.BILLEDTO = selectedcontainertype01;
                }


                //----------------To Display GateInDetails-----------------------

                //GateInDetail gdet = context.gateindetails.Find(Convert.ToInt32(vm.detaildata[0].GIDID));
                //if (vm.detaildata[0].GIDID == gdet.GIDID)
                //{
                //    ViewBag.VOYNO = gdet.VOYNO;
                //    ViewBag.IMPRTNAME = gdet.IMPRTNAME;
                //    ViewBag.STMRNAME = gdet.STMRNAME;

                //}//------End


            }//---End Of IF


            return View(vm);
        }//----End of Form

        public void ssavedata(FormCollection F_Form)
        {

            OpenSheetDetail opensheetdetail = new OpenSheetDetail();
            //-------Getting Primarykey field--------
            Int32 OSMID = Convert.ToInt32(F_Form["masterdata[0].OSMID"]);

            // Get OSMDNO for logging
            var master = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == OSMID);
            string osmdno = master != null ? master.OSMDNO : OSMID.ToString();

            //-----End

            //--------------------Bill Entry Detail and Open Sheet Detail---------------------------
            string[] OSDID = F_Form.GetValues("OSDID");
            string[] FBILLEDID = F_Form.GetValues("BILLEDID");
            string[] F_GIDID = F_Form.GetValues("F_GIDID");
            string[] LSEALNO = F_Form.GetValues("LSEALNO");
            string[] SSEALNO = F_Form.GetValues("SSEALNO");
            string[] GIDATE = F_Form.GetValues("GIDATE");
            string[] CSEALNO = F_Form.GetValues("CSEALNO");
            string[] BLOCK = F_Form.GetValues("BLOCK");
            string[] INSPTYPE = F_Form.GetValues("INSPTYPE"); 


            for (int count = 0; count < OSDID.Count(); count++)
            {


                var OSDID_ = Convert.ToInt32(OSDID[count]);
                
                // Load BEFORE state
                OpenSheetDetail beforeDetail = null;
                if (OSDID_ != 0)
                {
                    opensheetdetail = context.opensheetdetails.Find(OSDID_);
                    beforeDetail = context.opensheetdetails.AsNoTracking().FirstOrDefault(x => x.OSDID == OSDID_);
                    
                    //----------OpenSheet Detail -----------------//
                    opensheetdetail.OSMID = Convert.ToInt32(F_Form["masterdata[0].OSMID"]);
                    opensheetdetail.GIDID = Convert.ToInt32(F_GIDID[count]);
                    opensheetdetail.LSEALNO = LSEALNO[count].ToString();
                    opensheetdetail.SSEALNO = SSEALNO[count].ToString();
                    opensheetdetail.CSEALNO = CSEALNO[count].ToString();
                    opensheetdetail.GIDATE = Convert.ToDateTime(GIDATE[count]).Date;
                    opensheetdetail.BILLEDID = Convert.ToInt32(FBILLEDID[count]);                    

                    string instype = Convert.ToString(INSPTYPE[count]);
                    if (instype == "Yes")
                    {
                        opensheetdetail.INSPTYPE = 1;
                    }
                    else
                    {
                        opensheetdetail.INSPTYPE = 0;
                    }
                    opensheetdetail.CUSRID = Session["CUSRID"].ToString();
                    opensheetdetail.LMUSRID = Session["CUSRID"].ToString();
                    opensheetdetail.DISPSTATUS = 0;
                    opensheetdetail.PRCSDATE = DateTime.Now;
                    //----------End of OpenSheet Detail

                    context.Entry(opensheetdetail).State = System.Data.Entity.EntityState.Modified;
                    context.SaveChanges();

                    // Log detail changes after save
                    if (beforeDetail != null)
                    {
                        var afterDetail = context.opensheetdetails.AsNoTracking().FirstOrDefault(x => x.OSDID == OSDID_);
                        if (afterDetail != null)
                        {
                            int nextVersion = CalculateNextVersion(osmdno);
                            LogOpenSheetDetailEdits(beforeDetail, afterDetail, osmdno, Session["CUSRID"].ToString(), nextVersion);
                        }
                    }

                    string osuquery = "EXEC sp_opensheet_seal_history_update @compyid = " + Convert.ToInt32(Session["compyid"]) + ",";
                    osuquery += " @OSMID = " + Convert.ToInt32(opensheetdetail.OSMID) + ",";
                    osuquery += " @OSDID = " + Convert.ToInt32(opensheetdetail.OSDID) + ",";
                    osuquery += " @CSEALNO = '" + Convert.ToString(opensheetdetail.CSEALNO) + "'";
                    context.Database.ExecuteSqlCommand(osuquery);

                }
            }
            Response.Redirect("Index");

        }//---End of Savedata


        [HttpPost]
        public ActionResult tchasavedata(FormCollection tab)
        {
            string status = "";
            int osmid = Convert.ToInt32(tab["OSMID"]);
            
            // Load BEFORE state
            var before = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
            
            using (var trans = context.Database.BeginTransaction())
            {
                try
                {
                    string TRANMID = Convert.ToString(tab["TRANMID"]);
                    string OSMNAME = Convert.ToString(tab["OSMNAME"]);
                    string CHAID = Convert.ToString(tab["CHAID"]);

                    string OSBCHACATEAID = Convert.ToString(tab["OSBCHACATEAID"]);
                    string OSBCHACATEAGSTNO = Convert.ToString(tab["OSBCHACATEAGSTNO"]);
                    string OSBCHASTATEID = Convert.ToString(tab["OSBCHASTATEID"]);
                    string OSBCHAADDR1 = Convert.ToString(tab["OSBCHAADDR1"]);
                    string OSBCHAADDR2 = Convert.ToString(tab["OSBCHAADDR2"]);
                    string OSBCHAADDR3 = Convert.ToString(tab["OSBBCHAADDR3"]);
                    string OSBCHAADDR4 = Convert.ToString(tab["OSBBCHAADDR4"]);

                    var sq = context.Database.SqlQuery<VW_TALLYCHANAME_UPDATE_VIEW_ASSGN>(" SELECT *FROM  VW_TALLYCHANAME_UPDATE_VIEW_ASSGN  WHERE OSMID = " + Convert.ToInt32(osmid) + "  ").ToList();

                    if (sq.Count > 0)
                    {
                        status = "Invoice";
                    }
                    else
                    {
                        if (osmid > 0)
                        {
                            string osuquery = " UPDATE OPENSHEETMASTER SET OSMNAME = '" + Convert.ToString(OSMNAME) + "',";
                            osuquery += " CHAID = " + Convert.ToInt32(CHAID) + ",";
                            osuquery += " OSBBCHACATEAID = " + Convert.ToInt32(OSBCHACATEAID) + ",";
                            osuquery += " OSBCHACATEAGSTNO = '" + Convert.ToString(OSBCHACATEAGSTNO) + "', OSBCHASTATEID = " + Convert.ToInt32(OSBCHASTATEID) + ",";
                            osuquery += " OSBCHAADDR1 = '" + Convert.ToString(OSBCHAADDR1) + "', OSBCHAADDR2 = '" + Convert.ToString(OSBCHAADDR2) + "',";
                            osuquery += " OSBCHAADDR3 = '" + Convert.ToString(OSBCHAADDR3) + "', OSBCHAADDR4 = '" + Convert.ToString(OSBCHAADDR4) + "' WHERE OSMID =" + Convert.ToInt32(osmid) + " ";
                            context.Database.ExecuteSqlCommand(osuquery);
                        }
                        status = "saved";
                    }
                    trans.Commit();
                    
                    // Load AFTER state and log changes
                    if (before != null && status == "saved")
                    {
                        var after = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
                        if (after != null)
                        {
                            int nextVersion = CalculateNextVersion(after.OSMDNO);
                            LogOpenSheetEdits(before, after, Session["CUSRID"].ToString(), nextVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult boenosavedata(FormCollection tab)
        {
            string status = "";
            int osmid = Convert.ToInt32(tab["OSMID"]);
            
            // Load BEFORE state
            var before = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
            
            using (var trans = context.Database.BeginTransaction())
            {
                try
                {
                    string BOENO = Convert.ToString(tab["BOENO"]);
                    string BOEDATE = Convert.ToString(tab["BOEDATE"]);

                    //string upquery = " exec SP_UPDATE_OSBETRAN_BOENO @OSMID = " + Convert.ToInt32(OSMID) + ", @OSBOENO = '" + Convert.ToString(BOENO) + "'" ;
                    string upquery = " exec SP_UPDATE_OSBETRAN_BOENO_N01 @OSMID = " + Convert.ToInt32(osmid) + ", @OSBOENO = '" + Convert.ToString(BOENO) + "', @OSBOEDATE = '" + Convert.ToDateTime(BOEDATE).Date.ToString("dd-MMM-yyyy") + "'";
                    context.Database.ExecuteSqlCommand(upquery);

                    status = "saved";                    
                    trans.Commit();
                    
                    // Load AFTER state and log changes
                    if (before != null)
                    {
                        var after = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
                        if (after != null)
                        {
                            int nextVersion = CalculateNextVersion(after.OSMDNO);
                            LogOpenSheetEdits(before, after, Session["CUSRID"].ToString(), nextVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult blnosavedata(FormCollection tab)
        {
            string status = "";
            int osmid = Convert.ToInt32(tab["OSMID"]);
            
            // Load BEFORE state
            var before = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
            
            using (var trans = context.Database.BeginTransaction())
            {
                try
                {
                    string BLNO = Convert.ToString(tab["BLNO"]);

                    string upquery = " exec SP_UPDATE_OS_BL_TRAN_BLNO @OSMID = " + Convert.ToInt32(osmid) + ", @OSBLNO = '" + Convert.ToString(BLNO) + " '";
                    context.Database.ExecuteSqlCommand(upquery);

                    status = "saved";
                    trans.Commit();
                    
                    // Load AFTER state and log changes
                    if (before != null)
                    {
                        var after = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid);
                        if (after != null)
                        {
                            int nextVersion = CalculateNextVersion(after.OSMDNO);
                            LogOpenSheetEdits(before, after, Session["CUSRID"].ToString(), nextVersion);
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCatAddressDetail(string CATEAID)
        {
            int cateaid = 0;
            cateaid = Convert.ToInt32(CATEAID);

            if (cateaid > 0)
            {
                var result = (from r in context.categoryaddressdetails
                              where r.CATEAID == cateaid
                              select new { r.STATEID, r.CATEAGSTNO, r.CATEAADDR1, r.CATEAADDR2, r.CATEAADDR3, r.CATEAADDR4 }).Distinct();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }


        }

        public JsonResult GetCatLocationDetail(int CATEID)
        {
            if (CATEID > 0)
            {

                var result = (from r in context.categoryaddressdetails
                              where r.CATEID == CATEID
                              select new { r.CATEAID, r.CATEATYPEDESC }).Distinct();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }


        }

        //------------------------Insert and update data---------------------------
        public void savedata(FormCollection F_Form)
        {
            OpenSheetMaster opensheetmaster = new OpenSheetMaster();
            OpenSheetDetail opensheetdetail = new OpenSheetDetail();
            BillEntryMaster billentrymaster = new BillEntryMaster();
            BillEntryDetail billentrydetail = new BillEntryDetail();
            //-------Getting Primarykey field--------
            Int32 OSMID = Convert.ToInt32(F_Form["masterdata[0].OSMID"]);
            string BILLEMID1 = Convert.ToString(F_Form["BILLEMID"]);
            //if(BILLEMID1 ==null && BILLEMID1 == "")
            //    BILLEMID1 = Convert.ToString(F_Form["boedetaildata[0].BILLEMID"]);
            Int32 BILLEMID = 0;
            if(BILLEMID1 != null && BILLEMID1 != "")
                BILLEMID = Convert.ToInt32(BILLEMID1);
            Int32 TMPBOEID = Convert.ToInt32(F_Form["TMPBOEID"]);
            //-----End

            string todaydt = Convert.ToString(DateTime.Now);
            string todayd = Convert.ToString(DateTime.Now.Date);

            string userId = Session["CUSRID"]?.ToString() ?? "";
            OpenSheetMaster before = null;

            if (OSMID != 0)//Getting Primary id in Edit mode
            {
                opensheetmaster = context.opensheetmasters.Find(OSMID);
                // Capture before state for edit logging AFTER loading the entity
                try
                {
                    before = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == OSMID);
                    if (before != null)
                    {
                        EnsureBaselineVersionZero(before, userId);
                    }
                }
                catch { /* ignore if baseline creation fails */ }
               // billentrymaster = context.billentrymasters.Find(BILLEMID);
            }
            if(BILLEMID != 0)
            {
                billentrymaster = context.billentrymasters.Find(BILLEMID);
            }

            //--------------------------OpenSheet Master---------//
            opensheetmaster.SDPTID = 1;
            opensheetmaster.COMPYID = Convert.ToInt32(Session["compyid"]);
            opensheetmaster.OSMDATE = Convert.ToDateTime(F_Form["masterdata[0].OSMDATE"]).Date;
            if (opensheetmaster.OOCNO == null)
                opensheetmaster.OOCNO = "";
            //if (opensheetmaster.OOCDATE == null || opensheetmaster.OOCDATE == Convert.ToDateTime("01/01/0001 00:00:00"))
            //    opensheetmaster.OOCDATE = Convert.ToDateTime(todayd);
            string indate = Convert.ToString(F_Form["masterdata[0].OSMDATE"]);
            if (indate != null || indate != "")
            {
                opensheetmaster.OSMDATE = Convert.ToDateTime(indate).Date;
            }
            else { opensheetmaster.OSMDATE = DateTime.Now.Date; }

            if (opensheetmaster.OSMDATE > Convert.ToDateTime(todayd))
            {
                opensheetmaster.OSMDATE = Convert.ToDateTime(todayd);
            }

            string intime = Convert.ToString(F_Form["masterdata[0].OSMTIME"]);
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

                        opensheetmaster.OSMTIME = Convert.ToDateTime(in_datetime);
                    }
                    else { opensheetmaster.OSMTIME = DateTime.Now; }
                }
                else { opensheetmaster.OSMTIME = DateTime.Now; }
            }
            else { opensheetmaster.OSMTIME = DateTime.Now; }

            if (opensheetmaster.OSMTIME > Convert.ToDateTime(todaydt))
            {
                opensheetmaster.OSMTIME = Convert.ToDateTime(todaydt);
            }


            //opensheetmaster.OSMTIME = Convert.ToDateTime(F_Form["masterdata[0].OSMDATE"]);

            opensheetmaster.CHAID = Convert.ToInt32(F_Form["masterdata[0].CHAID"]);
            opensheetmaster.OSMNAME = F_Form["masterdata[0].OSMNAME"].ToString();
            opensheetmaster.OSMLNAME = F_Form["masterdata[0].OSMLNAME"].ToString();
            opensheetmaster.OSMCNAME = F_Form["masterdata[0].OSMCNAME"].ToString();
            opensheetmaster.BOENO = Convert.ToString(F_Form["masterdata[0].BOENO"]);
            opensheetmaster.BOEDATE = Convert.ToDateTime(F_Form["masterdata[0].BOEDATE"]).Date;
            opensheetmaster.DOTYPE = Convert.ToInt16(F_Form["DOTYPE"]);
            opensheetmaster.DODATE = Convert.ToDateTime(F_Form["masterdata[0].DODATE"]).Date;
            opensheetmaster.DOIDATE = Convert.ToDateTime(F_Form["masterdata[0].DOIDATE"]).Date;
            opensheetmaster.DONO = Convert.ToString(F_Form["masterdata[0].DONO"]);
            opensheetmaster.SCTYPE = Convert.ToInt16(F_Form["SCTYPE"]);
            opensheetmaster.OSMLDTYPE = Convert.ToInt16(F_Form["OSMLDTYPE"]);
            if (opensheetmaster.SCTYPE == 1)
            {
                string scdateStr = Convert.ToString(F_Form["masterdata[0].SCDATE"]);
                if (!string.IsNullOrWhiteSpace(scdateStr))
                {
                    try
                    {
                        opensheetmaster.SCDATE = Convert.ToDateTime(scdateStr).Date;
                        opensheetmaster.SCTIME = Convert.ToDateTime(scdateStr);
                    }
                    catch
                    {
                        // If conversion fails, set to current date/time
                        opensheetmaster.SCDATE = DateTime.Now.Date;
                        opensheetmaster.SCTIME = DateTime.Now;
                    }
                }
                else
                {
                    // If SCDATE is empty, set to current date/time
                    opensheetmaster.SCDATE = DateTime.Now.Date;
                    opensheetmaster.SCTIME = DateTime.Now;
                }
            }
            else
            {
                // If SCTYPE is not 1, set SCDATE and SCTIME to null or default
                opensheetmaster.SCDATE = null;
                opensheetmaster.SCTIME = null;
            }
            opensheetmaster.SCDESC = Convert.ToString(F_Form["masterdata[0].SCDESC"]);
            opensheetmaster.SCREMRKS = Convert.ToString(F_Form["masterdata[0].SCREMRKS"]);
            opensheetmaster.OSMIGMNO = Convert.ToString(F_Form["masterdata[0].OSMIGMNO"]);
            opensheetmaster.OSMLNO = Convert.ToString(F_Form["OSMLNO"]);
            opensheetmaster.OSMVSLNAME = Convert.ToString(F_Form["masterdata[0].OSMVSLNAME"]);
            opensheetmaster.OSMAAMT = Convert.ToDecimal(F_Form["masterdata[0].OSMAAMT"]);
            opensheetmaster.OSMBLNO = Convert.ToString(F_Form["masterdata[0].OSMBLNO"]);

            string OSMNOP = Convert.ToString(F_Form["masterdata[0].OSMNOP"]);
            if (OSMNOP != null || OSMNOP != "")
            {
                opensheetmaster.OSMNOP = Convert.ToDecimal(OSMNOP);
            }
            else { opensheetmaster.OSMNOP = 0; }
            //opensheetmaster.OSMNOP = Convert.ToDecimal(F_Form["masterdata[0].OSMNOP"]);
            opensheetmaster.OSMWGHT = Convert.ToDecimal(F_Form["masterdata[0].OSMWGHT"]);
            opensheetmaster.OSMUNITID = Convert.ToInt32(F_Form["OSMUNITID"]);
            opensheetmaster.OSMTNOC = 0;// Convert.ToInt32(F_Form["masterdata[0].OSMTNOC"]);
            opensheetmaster.OSMFNOC = 0;// Convert.ToInt32(F_Form["masterdata[0].OSMFNOC"]);
            opensheetmaster.OSMLCATEID = Convert.ToInt32(F_Form["OSMLCATEID"]);
            opensheetmaster.OSMLCATENAME = Convert.ToString(F_Form["masterdata[0].OSMLCATENAME"]);
            opensheetmaster.OSMTYPE = Convert.ToInt16(F_Form["OSMTYPE"]);

            opensheetmaster.OSBILLEDTO = Convert.ToInt16(F_Form["BILLEDTO"]);
            opensheetmaster.OSBILLREFID = Convert.ToInt32(F_Form["masterdata[0].OSBILLREFID"]);
            opensheetmaster.OSBILLREFNAME = Convert.ToString(F_Form["masterdata[0].OSBILLREFNAME"]);

            opensheetmaster.OSMDUTYAMT = Convert.ToDecimal(F_Form["masterdata[0].OSMDUTYAMT"]);
            string bldate = Convert.ToString(F_Form["masterdata[0].OSMBLDATE"]);
            if(bldate==""|| bldate==null)
            {
                opensheetmaster.OSMBLDATE = DateTime.Now.Date;
            }
            else
            {
                opensheetmaster.OSMBLDATE = Convert.ToDateTime(bldate);
            }

            string igmdate = Convert.ToString(F_Form["masterdata[0].OSMIGMDATE"]);
            if (igmdate == "" || igmdate == null)
            {
                opensheetmaster.OSMIGMDATE = DateTime.Now.Date;
            }
            else
            {
                opensheetmaster.OSMIGMDATE = Convert.ToDateTime(igmdate);
            }
            //opensheetmaster.OSMBLDATE = Convert.ToDateTime(F_Form["masterdata[0].OSMBLDATE"]).Date;
            //opensheetmaster.OSMIGMDATE = Convert.ToDateTime(F_Form["masterdata[0].OSMIGMDATE"]).Date;

            string OSCATEAID = Convert.ToString(F_Form["OSBCHACATEAID"]);
            if (OSCATEAID != "" || OSCATEAID != null)
            {
                opensheetmaster.OSBCHACATEAID = Convert.ToInt32(OSCATEAID);
            }
            else
            {
                opensheetmaster.OSBCHACATEAID = 0;
            }

            string OSBBCHACATEAID = Convert.ToString(F_Form["OSBBCHACATEAID"]);
            if (OSBBCHACATEAID != "" || OSBBCHACATEAID != null)
            {
                opensheetmaster.OSBBCHACATEAID = Convert.ToInt32(OSBBCHACATEAID);
            }
            else
            {
                opensheetmaster.OSBBCHACATEAID = 0;
            }

            string OSBCHASTATEID = Convert.ToString(F_Form["masterdata[0].OSBCHASTATEID"]);
            if (OSBCHASTATEID != "" || OSBCHASTATEID != null)
            {
                opensheetmaster.OSBCHASTATEID = Convert.ToInt32(OSBCHASTATEID);
            }
            else
            {
                opensheetmaster.OSBCHASTATEID = 0;
            }
            string OSBBCHASTATEID = Convert.ToString(F_Form["masterdata[0].OSBBCHASTATEID"]);
            if (OSBBCHASTATEID != "" || OSBBCHASTATEID != null)
            {
                opensheetmaster.OSBBCHASTATEID = Convert.ToInt32(OSBBCHASTATEID);
            }
            else
            {
                opensheetmaster.OSBBCHASTATEID = 0;
            }

            opensheetmaster.OSBCHACATEAGSTNO = Convert.ToString(F_Form["masterdata[0].OSBCHACATEAGSTNO"]);
            opensheetmaster.OSBBCHACATEAGSTNO = Convert.ToString(F_Form["masterdata[0].OSBBCHACATEAGSTNO"]);
            opensheetmaster.OSBCHAADDR1 = Convert.ToString(F_Form["masterdata[0].OSBCHAADDR1"]);
            opensheetmaster.OSBBCHAADDR1 = Convert.ToString(F_Form["masterdata[0].OSBBCHAADDR1"]);
            opensheetmaster.OSBCHAADDR2 = Convert.ToString(F_Form["masterdata[0].OSBCHAADDR2"]);
            opensheetmaster.OSBBCHAADDR2 = Convert.ToString(F_Form["masterdata[0].OSBBCHAADDR1"]);
            opensheetmaster.OSBCHAADDR3 = Convert.ToString(F_Form["masterdata[0].OSBCHAADDR3"]);
            opensheetmaster.OSBBCHAADDR3 = Convert.ToString(F_Form["masterdata[0].OSBBCHAADDR3"]);
            opensheetmaster.OSBCHAADDR4 = Convert.ToString(F_Form["masterdata[0].OSBCHAADDR4"]);
            opensheetmaster.OSBBCHAADDR4 = Convert.ToString(F_Form["masterdata[0].OSBBCHAADDR4"]);

            if (OSMID == 0)
                opensheetmaster.CUSRID = Session["CUSRID"].ToString();

            opensheetmaster.LMUSRID = Session["CUSRID"].ToString();
            opensheetmaster.DISPSTATUS = 0;
            opensheetmaster.PRCSDATE = DateTime.Now;
            //--------End of OpenSheet Master 

            //-----------------------------Bill Entry Master----------//
            billentrymaster.SDPTID = 1;
            billentrymaster.COMPYID = Convert.ToInt32(Session["compyid"]);
            billentrymaster.BILLEMDATE = Convert.ToDateTime(F_Form["masterdata[0].BOEDATE"]).Date;
            billentrymaster.BILLEMTIME = Convert.ToDateTime(F_Form["masterdata[0].BOEDATE"]);

            billentrymaster.BILLEMDNO = opensheetmaster.BOENO;
            billentrymaster.CHAID = Convert.ToInt32(F_Form["masterdata[0].CHAID"]);
            billentrymaster.BILLEMNAME = F_Form["masterdata[0].OSMNAME"].ToString();
            billentrymaster.BILLEMAAMT = Convert.ToDecimal(F_Form["masterdata[0].OSMAAMT"]);
            billentrymaster.BLNO = Convert.ToString(F_Form["masterdata[0].OSMBLNO"]);
            billentrymaster.NOP = Convert.ToDecimal(F_Form["masterdata[0].OSMNOP"]);
            billentrymaster.WGHT = Convert.ToDecimal(F_Form["masterdata[0].OSMWGHT"]);
            billentrymaster.UNITID = Convert.ToInt32(F_Form["OSMUNITID"]);
            billentrymaster.BILLEDMTYPE = Convert.ToInt16(F_Form["OSMTYPE"]);
            if (BILLEMID == 0)
                billentrymaster.CUSRID = Session["CUSRID"].ToString();
            //if (OSMID == 0)
            //    billentrymaster.CUSRID = Session["CUSRID"].ToString();

            billentrymaster.LMUSRID = Session["CUSRID"].ToString();
            billentrymaster.DISPSTATUS = 0;
            billentrymaster.PRCSDATE = DateTime.Now;

            //-------End of Bill Entry Master ----------------

            // Calculate version once before any logging to ensure master and detail edits use the same version
            int nextVersion = 1;

            if (OSMID == 0)
            {               
                opensheetmaster.OSMNO = Convert.ToInt32(Autonumber.autonum("opensheetmaster", "OSMNO", "OSMNO<>0 and compyid=" + Convert.ToInt32(Session["compyid"]) + "").ToString());
                int ano = opensheetmaster.OSMNO;
                string prfx = string.Format("{0:D5}", ano);
                opensheetmaster.OSMDNO = prfx.ToString();
                context.opensheetmasters.Add(opensheetmaster);
                context.SaveChanges();
                
                // Calculate version for new record (after OSMDNO is set)
                nextVersion = CalculateNextVersion(opensheetmaster.OSMDNO);
                
                // Create baseline for new record
                try
                {
                    var newRecord = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == opensheetmaster.OSMID);
                    if (newRecord != null)
                    {
                        EnsureBaselineVersionZero(newRecord, userId);
                    }
                }
                catch { /* ignore baseline creation errors */ }
            }
            else
            {
                context.Entry(opensheetmaster).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
                
                // Calculate version for existing record
                nextVersion = CalculateNextVersion(opensheetmaster.OSMDNO);
                
                // Log changes after successful save - reload after state from database to ensure accurate comparison
                if (before != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenSheet SAVE: OSMID={opensheetmaster.OSMID}, OSMDNO={opensheetmaster.OSMDNO}, calling LogOpenSheetEdits");
                        // Reload after state from database to ensure we have the actual saved values
                        var after = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == opensheetmaster.OSMID);
                        if (after != null)
                        {
                            LogOpenSheetEdits(before, after, userId, nextVersion);
                            System.Diagnostics.Debug.WriteLine($"LogOpenSheetEdits completed for OSMID={opensheetmaster.OSMID}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"OpenSheet SAVE: after state not found for OSMID={opensheetmaster.OSMID}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenSheet edit logging failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OpenSheet SAVE: before is null for OSMID={OSMID}");
                }
            }
            if (TMPBOEID == 0)
            {
                if (BILLEMID == 0)
                {
                    billentrymaster.BILLEMNO = Convert.ToInt32(Autonumber.autonum("billentrymaster", "BILLEMNO", "BILLEMNO<>0 and compyid=" + Convert.ToInt32(Session["compyid"]) + "").ToString());
                    context.billentrymasters.Add(billentrymaster);
                    context.SaveChanges();
                }
                else
                {

                    context.Entry(billentrymaster).State = System.Data.Entity.EntityState.Modified;
                    context.SaveChanges();
                }
            }

            //--------------------Bill Entry Detail and Open Sheet Detail---------------------------
            string[] OSDID = F_Form.GetValues("OSDID");
            string[] FBILLEDID = F_Form.GetValues("BILLEDID");
            string[] F_GIDID = F_Form.GetValues("F_GIDID");
            string[] LSEALNO = F_Form.GetValues("LSEALNO");
            string[] SSEALNO = F_Form.GetValues("SSEALNO");
            string[] CSEALNO = F_Form.GetValues("CSEALNO");
            string[] GIDATE = F_Form.GetValues("GIDATE");


            for (int count = 0; count < OSDID.Count(); count++)
            {
                var OSDID_ = Convert.ToInt32(OSDID[count]); var BILLEDID = Convert.ToInt32(FBILLEDID[count]);
                
                // Capture before state for detail logging
                OpenSheetDetail beforeDetail = null;
                if (OSDID_ != 0) 
                { 
                    opensheetdetail = context.opensheetdetails.Find(OSDID_);
                    beforeDetail = context.opensheetdetails.AsNoTracking().FirstOrDefault(x => x.OSDID == OSDID_);
                }
                else
                {
                    opensheetdetail = new OpenSheetDetail();
                }
                
                if (BILLEDID != 0) { billentrydetail = context.billentrydetails.Find(BILLEDID); }
                //----------OpenSheet Detail -----------------//
                opensheetdetail.OSMID = opensheetmaster.OSMID;
                opensheetdetail.GIDID = Convert.ToInt32(F_GIDID[count]);
                opensheetdetail.LSEALNO = LSEALNO[count].ToString();
                opensheetdetail.SSEALNO = SSEALNO[count].ToString();
                if (CSEALNO != null && count < CSEALNO.Length && !string.IsNullOrEmpty(CSEALNO[count]))
                {
                    opensheetdetail.CSEALNO = CSEALNO[count].ToString();
                }
                opensheetdetail.GIDATE = Convert.ToDateTime(GIDATE[count]).Date;
                opensheetdetail.BILLEDID = billentrydetail.BILLEDID;
                opensheetdetail.INSPTYPE = Convert.ToInt16(F_Form["INSP_TYPE"]);
                if (BILLEDID == 0)
                    opensheetdetail.CUSRID = Session["CUSRID"].ToString();

                opensheetdetail.LMUSRID = Session["CUSRID"].ToString(); ;
                opensheetdetail.DISPSTATUS = 0;
                opensheetdetail.PRCSDATE = DateTime.Now;
                //----------End of OpenSheet Detail
                if (OSDID_ == 0)
                {
                    context.opensheetdetails.Add(opensheetdetail);
                    context.SaveChanges();
                    
                    // Log new detail record
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenSheet DETAIL SAVE: New record OSDID={opensheetdetail.OSDID}, OSMDNO={opensheetmaster.OSMDNO}, calling LogOpenSheetDetailEdits");
                        LogOpenSheetDetailEdits(null, opensheetdetail, opensheetmaster.OSMDNO, userId, nextVersion);
                        System.Diagnostics.Debug.WriteLine($"LogOpenSheetDetailEdits completed for new detail OSDID={opensheetdetail.OSDID}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenSheet detail logging failed for new record: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    context.Entry(opensheetdetail).State = System.Data.Entity.EntityState.Modified;
                    context.SaveChanges();
                    
                    // Log detail changes (use the same version calculated earlier)
                    if (beforeDetail != null)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"OpenSheet DETAIL SAVE: Existing record OSDID={opensheetdetail.OSDID}, OSMDNO={opensheetmaster.OSMDNO}, calling LogOpenSheetDetailEdits");
                            LogOpenSheetDetailEdits(beforeDetail, opensheetdetail, opensheetmaster.OSMDNO, userId, nextVersion);
                            System.Diagnostics.Debug.WriteLine($"LogOpenSheetDetailEdits completed for existing detail OSDID={opensheetdetail.OSDID}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"OpenSheet detail logging failed for existing record: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"OpenSheet DETAIL SAVE: beforeDetail is null for OSDID={OSDID_}");
                    }
                }
                //-------------bill entry detail-----------//

                billentrydetail.GIDID = Convert.ToInt32(F_GIDID[count]);
                billentrydetail.GIDATE = Convert.ToDateTime(GIDATE[count]).Date;
                billentrydetail.DNOP = billentrymaster.NOP;
                billentrydetail.DWGHT = billentrymaster.WGHT;
                if (BILLEDID == 0)
                    billentrydetail.CUSRID = Session["CUSRID"].ToString();

                billentrydetail.LMUSRID = Session["CUSRID"].ToString();
                billentrydetail.DISPSTATUS = 0;
                billentrydetail.PRCSDATE = DateTime.Now;
                //------End of bill entry 
                if (TMPBOEID == 0)
                {
                    billentrydetail.BILLEMID = billentrymaster.BILLEMID;
                    if (BILLEDID == 0)
                    {
                        // context.opensheetdetails.Add(opensheetdetail);
                        // context.SaveChanges();
                        context.billentrydetails.Add(billentrydetail);
                        context.SaveChanges();
                        context.Entry(opensheetdetail).Entity.BILLEDID = billentrydetail.BILLEDID;
                        context.SaveChanges();
                    }
                    else
                    {

                        context.Entry(billentrydetail).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                    }
                }
                else
                {
                    billentrydetail.BILLEMID = TMPBOEID;
                    if (BILLEDID == 0)
                    {
                        // context.opensheetdetails.Add(opensheetdetail);
                        // context.SaveChanges();
                        context.billentrydetails.Add(billentrydetail);
                        context.SaveChanges();
                        context.Entry(opensheetdetail).Entity.BILLEDID = billentrydetail.BILLEDID;
                        context.SaveChanges();
                    }
                    else
                    {

                        context.Entry(billentrydetail).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                    }
                   // context.billentrydetails.Add(billentrydetail);
                    //context.Entry(billentrydetail).State = System.Data.Entity.EntityState.Modified;
                   // context.SaveChanges();
                    //context.Entry(opensheetdetail).Entity.BILLEDID = billentrydetail.BILLEDID;
                   // context.SaveChanges();
                }
            }

            string[] PRDTGID1 = F_Form.GetValues("T_PRDTGID");

            string PRDTGID = Convert.ToString(F_Form["T_PRDTGID"]);

            if (PRDTGID == "" || PRDTGID == null || PRDTGID == "0")
            {
                
            }
            else {
                string upquery = "UPDATE  GATEINDETAIL  SET PRDTGID = " + Convert.ToInt32(PRDTGID) + " WHERE SDPTID = 1 AND IGMNO = '" + Convert.ToString(opensheetmaster.OSMIGMNO) + "' AND GPLNO = '" + Convert.ToString(opensheetmaster.OSMLNO) + "'";
                context.Database.ExecuteSqlCommand(upquery);
            }


            Response.Redirect("Index");

        }//---End of Savedata




        //------------To Get Respective Line No to IGM No-----
        public JsonResult GetLineNo(string id)
        {
            var group = (from vw in context.view_opensheet_cbx_assign_01 where vw.IGMNO == id select new { vw.GPLNO }).Distinct().ToList();
            return new JsonResult() { Data = group, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }



        //------------------Autocomplete CHA-------------
        //public JsonResult AutoCha(string term)
        //{
        //    var result = (from catem in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
        //                  where catem.CATENAME.ToLower().Contains(term.ToLower())
        //                  select new { catem.CATENAME, catem.CATEID, catem.CATEBGSTNO }).Distinct();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        public JsonResult AutoCha(string term)
        {
            var result = (from catem in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
                          where catem.CATENAME.ToLower().Contains(term.ToLower())
                          select new { catem.CATENAME, catem.CATEID}).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //--------Autocomplete CHA Name
        //public JsonResult NewAutoCha(string term)
        //{

        //    var result = (from r in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
        //                  where r.CATENAME.ToLower().Contains(term.ToLower())
        //                  select new { r.CATENAME, r.CATEID, r.CATEBGSTNO }).Distinct();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        public JsonResult NewAutoCha(string term)
        {

            var result = (from r in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //cha and importer

        //public JsonResult NewAutoImporter(string term)
        //{
        //    var result = (from r in context.categorymasters.Where(m => m.CATETID == 1).Where(x => x.DISPSTATUS == 0)
        //                  where r.CATENAME.ToLower().Contains(term.ToLower())
        //                  select new { r.CATENAME, r.CATEID, r.CATEBGSTNO }).Distinct(); //CATEBGSTNO - CATEGSTNO RAJESH TO CHECK
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        public JsonResult NewAutoImporter(string term)
        {
            var result = (from r in context.categorymasters.Where(m => m.CATETID == 1).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct(); //CATEBGSTNO - CATEGSTNO RAJESH TO CHECK
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //------------Autocomplete IGM----------
        public JsonResult AutoIGM(string term)
        {
            var result = (from view in context.view_opensheet_cbx_assign_01
                          where view.IGMNO.ToLower().Contains(term.ToLower())
                          select new { view.IGMNO }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }




        //----------Open Sheet Details Table Display--------
        public void Detail(string PIGMNO, string PGPLNO)
        {
            using (SCFSERPContext contxt = new SCFSERPContext())
            {

                var query = context.Database.SqlQuery<SP_OPENSHEET_CONTAINER_FLX_ASSGN_Result>("EXEC SP_OPENSHEET_CONTAINER_FLX_ASSGN @PIGMNO='" + PIGMNO + "',@PGPLNO='" + PGPLNO + "'").ToList();


                var tabl = " <div class='panel-heading navbar-inverse'  style=color:white>Open Sheet Details</div>";
                tabl = tabl + "<Table id=mytabl class='table table-striped table-bordered bootstrap-datatable'> <thead>";
                tabl = tabl + "<tr><th>S.No</th> <th>Container No</th><th> Size</th><th>InDate</th>";
                tabl = tabl + "<th>IGM No</th> <th> Line No </th>";

                tabl = tabl + "<th>Liner Seal No</th><th class = 'hide'>Customs Seal No</th>";
                tabl = tabl + "<th>Scanned</th><th>Scan Type</th>";

                tabl = tabl + "<th> Inspection </th> <th> Blocked </th></tr> </thead>";
                var type = ""; var i = 1;
                foreach (var rslt in query)
                {
                    if (rslt.CSEALNO == null) rslt.CSEALNO = "-";
                    if (rslt.LPSEALNO == null) rslt.LPSEALNO = "-";
                    if (rslt.BOEDATE == null || rslt.BOEDATE == "") rslt.BOEDATE = Convert.ToString(DateTime.Now.Date);
                    if (rslt.IGMDATE == null || rslt.IGMDATE == "") rslt.IGMDATE = Convert.ToString(DateTime.Now.Date);
                    if (rslt.BLNO == null || rslt.BLNO == "") rslt.BLNO = Convert.ToString("-");
                    if (rslt.GBDID == 0) type = "Yes"; else type = "No";

                    string scntypedesc = "";string scnmtypedesc = "";
                    if (rslt.GPSCNTYPE == 1) { scntypedesc = "Yes"; } else { scntypedesc = "No"; }
                    switch (rslt.GPSCNMTYPE)
                    {
                        case 1:
                            scnmtypedesc = "MISMATCH";
                            break;
                        case 2:
                            scnmtypedesc = "CLEAN";
                            break;
                        case 3:
                            scnmtypedesc = "NOT SCANNED";
                            break;
                        default:
                            scnmtypedesc = "NIL";
                            break;
                    }

                    tabl = tabl + "<tbody><tr><td class=hide><input type=text id=OSDID class=OSDID name=OSDID><input type=text id=BILLEDID value=0 class=BILLEDID name=BILLEDID></td>";
                    tabl = tabl + "<td class=hide><input type=text id=F_GIDID value=" + rslt.GIDID + "  class=F_GIDID name=F_GIDID hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=STMRNAME value='" + rslt.STMRNAME + "' class=STMRNAME name=STMRNAME></td>";
                    tabl = tabl + "<td class=hide><input type=text id=IMPRTNAME value='" + rslt.IMPRTNAME + "' class=IMPRTNAME name=IMPRTNAME hidden>";
                    tabl = tabl + "<input type=text id=PRDTGID value='" + rslt.PRDTGID + "' class=PRDTGID name=PRDTGID hidden>";
                    tabl = tabl + "<input type=text id=GIPRDTGCODE value='" + rslt.PRDTGCODE + "' class=GIPRDTGCODE name=GIPRDTGCODE hidden>";
                    tabl = tabl + "<input type=text id=GIPRDTGDESC value='" + rslt.PRDTGDESC + "' class=GIPRDTGDESC name=GIPRDTGDESC hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=GIBLNO value='" + rslt.BLNO + "' class=GIBLNO name=GIBLNO hidden>";
                    tabl = tabl + "<input type=text id=GIBOEDATE value='" + rslt.BOEDATE + "' class=GIBOEDATE name=GIBOEDATE hidden>";
                    tabl = tabl + "<input type=text id=GIIGMDATE value='" + rslt.IGMDATE + "' class=GIIGMDATE name=GIIGMDATE hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=VOYNO value='" + rslt.VOYNO + "' class=VOYNO name=VOYNO hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=VSLNAME value='" + rslt.VSLNAME + "' class=VSLNAME name=VSLNAME hidden></td>";
                    tabl = tabl + "<td>" + i + "</td><td><input type=text id=CONTNRNO value=" + rslt.CONTNRNO + " class=CONTNRNO name=CONTNRNO style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text value=" + rslt.CONTNRSDESC + " id=CONTNRSDESC class=CONTNRSDESC name=CONTNRSDESC style=width:45px readonly></td>";
                    tabl = tabl + "<td><input type=text id=GIDATE value=" + rslt.GIDATE + " class=GIDATE name=GIDATE style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text value=" + rslt.IGMNO + " id=IGMNO class=IGMNO name=IGMNO style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text id=LineNo value=" + rslt.GPLNO + " class=LineNo name=LineNo style=width:50px readonly></td>";
                    tabl = tabl + "<td><input type=text value='" + rslt.LPSEALNO + "' id=LSEALNO class=LSEALNO name=LSEALNO style=width:100px ></td>";
                    tabl = tabl + "<td class='hide'><input type=text value='" + rslt.CSEALNO + "' id=SSEALNO  class=SSEALNO name=SSEALNO style=width:100px ></td>";

                    tabl = tabl + "<td><input type=text value='" + scntypedesc + "' id=GPSCNTYPE class=GPSCNTYPE name=GPSCNTYPE style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text id=GPSCNMTYPE value='" + scnmtypedesc + "' class='GPSCNMTYPE' name=GPSCNMTYPE style=width:100px readonly></td>";

                    tabl = tabl + "<td><input type=text id=INSPTYPE  class='INSPTYPE' name=INSPTYPE value=" + rslt.INSP_DES + " style=width:100px readonly='readonly'></td>";                    
                    tabl = tabl + "<td><input type=text id=BLOCK  class=BLOCK name=BLOCK value='" + type + "' style=width:100px readonly='readonly'> <input type=text id=GIPRDTDESC value='" + rslt.PRDTDESC + "' class='hide GIPRDTDESC' name=GIPRDTDESC hidden></td></tr></tbody>";
                    tabl = tabl + "";

                    i++;
                }
                tabl = tabl + "</Table>";
                Response.Write(tabl);
            }


        }


        ////--------------------Duplicate Check for BOENO----------
        //public void BOEDetail()
        //{
        //    string BoeNo = Request.Form.Get("BoeNo");
        //    using (var contxt = new SCFSERPContext())
        //    {
        //        var query = contxt.Database.SqlQuery<string>("select BILLEMDNO from BILLENTRYMASTER where  BILLEMDNO='" + BoeNo + "' ").ToList();

        //        var BillNo = query.Count();
        //        if (BillNo != 0)
        //        {
        //            Response.Write("Bill of Entry No. already exists");
        //        }

        //    }

        //}//end
        public void GetBOEID(string id)
        {
            var query = context.Database.SqlQuery<int>("select BILLEMID from BILLENTRYMASTER where  BILLEMDNO='" + id + "' ").ToList();

            var BillNo = query.Count();
            if (BillNo != 0)
            {
                Response.Write(query[0]);
            }
            else
            {
                Response.Write("0");
            }
        }//end
        public void BOECheck(string id)
        {
            var param = id.Split('~');
            var boedate = param[3];
            var edate = boedate.Split('-');
            var adate = edate[2] + "-" + edate[1] + "-" + edate[0];
            //var query = context.Database.SqlQuery<OpenSheetMaster>("select * from OpenSheetMaster where  BOENO='" + param[0] + "' ").ToList();
            var query = context.Database.SqlQuery<OpenSheetMaster>("select * from OpenSheetMaster where  BOENO='" + param[0] + "' And BOEDATE = '" + adate + "'").ToList();
            if (query.Count > 0)
            {
                var qry = context.Database.SqlQuery<OpenSheetMaster>("select * from OpenSheetMaster where  OSMIGMNO='" + param[1] + "' and OSMLNO='" + param[2] + "' ").ToList();
                if (qry.Count > 0) { Response.Write("PROCEED"); }
                else if (qry.Count == 0) { Response.Write("IGMNO and LineNo Does Not Match With Existing BOENO...!"); }
            }
            else
            {
                Response.Write("PROCEED");
            }

        }


        //----------Detail Table in Edit Mode----------------
        public void DetailEdit(int id)
        {
            using (SCFSERPContext contxt = new SCFSERPContext())
            {
                //var detail = (from open in contxt.opensheetdetails
                //              join boe in contxt.billentrydetails on open.BILLEDID equals boe.BILLEDID
                //              join gate in contxt.gateindetails on open.GIDID equals gate.GIDID
                //              //join  blck in contxt.gateinblockdetails on gate.GIDID equals blck.GIDID
                //              where gate.IGMNO == Igm && gate.GPLNO == Line
                //              select new { boe.BILLEDID,boe.BILLEMID,open.OSDID, open.OSMID, open.GIDID, open.GIDATE, open.LSEALNO, open.SSEALNO, gate.IMPRTNAME, gate.STMRNAME, gate.VSLNAME, gate.CONTNRNO, gate.CONTNRSID, gate.IGMNO, gate.GPLNO,gate.VOYNO }).Distinct();
                var detail = context.Database.SqlQuery<VW_OPENSHEET_DETAIL_CTRL_ASSGN>("select * from VW_OPENSHEET_DETAIL_CTRL_ASSGN where  OSMID=" + id).ToList();
                var tabl = " <div class='panel-heading navbar-inverse'  style=color:white>Open Sheet Details</div>";
                tabl = tabl + "<Table id=mytabl class='table table-striped table-bordered bootstrap-datatable'> <thead><tr>";
                tabl = tabl + " <th>S.No</th> ";
                tabl = tabl + "<th>Container No</th>";
                tabl = tabl + "<th> Size</th><th>InDate</th>";
                tabl = tabl + "<th>IGM No</th> <th> Line No </th>";
                tabl = tabl + "<th>Liner Seal No</th><th class = 'hide'>Customs Seal No</th>";
                tabl = tabl + "<th>Scanned</th><th>Scan Type</th>";
                tabl = tabl + " <th> Inspection </th><th> Blocked </th></tr> </thead>";
                var sseal = "-"; var lseal = "-";var i = 1;
                foreach (var result in detail)
                {
                    if (result.SSEALNO != null) sseal = result.SSEALNO;
                    else sseal = "-";
                    if (result.LSEALNO != null) lseal = result.LSEALNO;
                    else lseal = "-";

                    string scntypedesc = ""; string scnmtypedesc = "";

                    if (result.GPSCNTYPE == 1) { scntypedesc = "Yes"; }else { scntypedesc = "No"; }
                    switch (result.GPSCNMTYPE)
                    {
                        case 1:
                            scnmtypedesc = "MISMATCH";
                            break;
                        case 2:
                            scnmtypedesc = "CLEAN";
                            break;
                        case 3:
                            scnmtypedesc = "NOT SCANNED";
                            break;
                        default:
                            scnmtypedesc = "NIL";
                            break;
                    }

                    tabl = tabl + "<tbody><tr><td class=hide><input type=text id=OSDID value=" + result.OSDID + " class=OSDID name=OSDID>";
                    tabl = tabl + "<input type=text id=BILLEDID value=" + result.BILLEDID + " class=BILLEDID name=BILLEDID>";
                    tabl = tabl + "<input type=text id=FBILLEMID value=" + result.BILLEMID + " class=FBILLEMID name=FBILLEMID></td>";
                    tabl = tabl + "<td class=hide><input type=text id=F_GIDID value=" + result.GIDID + "  class=F_GIDID name=F_GIDID hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=STMRNAME value='" + result.STMRNAME + "' class=STMRNAME name=STMRNAME></td>";
                    tabl = tabl + "<td class=hide><input type=text id=IMPRTNAME value='" + result.IMPRTNAME + "' class=IMPRTNAME name=IMPRTNAME hidden>";
                    tabl = tabl + "<input type=text id=PRDTGDESC value='" + result.PRDTGDESC + "' class=PRDTGDESC name=PRDTGDESC hidden>";
                    tabl = tabl + "<input type=text id=PRDTGID value='" + result.PRDTGID + "' class=PRDTGID name=PRDTGID hidden>";
                    tabl = tabl + "<input type=text id=PRDTGCODE value='" + result.PRDTGCODE + "' class=PRDTGCODE name=PRDTGCODE hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=VOYNO value='" + result.VOYNO + "' class=VOYNO name=VOYNO hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=VSLNAME value='" + result.VSLNAME + "' class=VSLNAME name=VSLNAME hidden></td>";
                    tabl = tabl + "<td>" + i + "</td>";
                    tabl = tabl + "<td><input type=text id=CONTNRNO value=" + result.CONTNRNO + " class=CONTNRNO name=CONTNRNO style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text value=" + result.CONTNRSID + " id=CONTNRSDESC class=CONTNRSDESC name=CONTNRSDESC style=width:45px readonly></td>";
                    tabl = tabl + "<td><input type=text id=GIDATE value=" + result.GIDATE + " class=GIDATE name=GIDATE style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text value=" + result.OSMIGMNO + " id=IGMNO class=IGMNO name=IGMNO style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text id=LineNo value=" + result.OSMLNO + " class=LineNo name=LineNo style=width:50px readonly></td>";
                    tabl = tabl + "<td><input type=text value='" + lseal + "' id=LSEALNO class=LSEALNO name=LSEALNO style=width:100px></td>";
                    tabl = tabl + "<td class = 'hide'><input type=text id=SSEALNO value='" + sseal + "' class='SSEALNO' name=SSEALNO style=width:100px></td>";

                    tabl = tabl + "<td><input type=text value='" + scntypedesc + "' id=GPSCNTYPE class=GPSCNTYPE name=GPSCNTYPE style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text id=GPSCNMTYPE value='" + scnmtypedesc + "' class='GPSCNMTYPE' name=GPSCNMTYPE style=width:100px readonly></td>";

                    tabl = tabl + "<td><input type=text id=INSPTYPE  class='INSPTYPE' name=INSPTYPE value=" + result.INSP_DES + " style=width:100px readonly='readonly'></td>";
                    //tabl = tabl + "<td><input type=text id=BLOCK  class=BLOCK name=BLOCK value='" + result.Block + "' style=width:100px readonly='readonly'></td>";
                    tabl = tabl + "<td><input type=text id=BLOCK  class=BLOCK name=BLOCK value='" + result.Block + "' style=width:100px readonly='readonly'> <input type=text id=GIPRDTDESC value='" + result.PRDTDESC + "' class='hide GIPRDTDESC' name=GIPRDTDESC hidden></td></tr></tbody>";
                    tabl = tabl + "</tr></tbody>";
                    i++;
                }
                tabl = tabl + "</Table>";
                Response.Write(tabl);

            }


        }

        public void Detail_EditData(int id)
        {
            using (SCFSERPContext contxt = new SCFSERPContext())
            {
                
                var detail = context.Database.SqlQuery<VW_OPENSHEET_DETAIL_CTRL_ASSGN>("select * from VW_OPENSHEET_DETAIL_CTRL_ASSGN where  OSMID=" + id).ToList();
                var tabl = "<div class='panel-heading navbar-inverse'  style=color:white>Open Sheet Details</div>";
                tabl = tabl + "<Table id=mytabl class='table table-striped table-bordered bootstrap-datatable'> <thead>";
                tabl = tabl + "<tr><th>S.No</th><th>Container No</th><th> Size</th><th>InDate</th>";
                tabl = tabl + "<th>IGM No</th> <th> Line No </th>";
                tabl = tabl + "<th>Liner Seal No</th><th class = 'hide'>Customs Seal No</th>";
                tabl = tabl + "<th>Scanned</th><th>Scan Type</th>";
                tabl = tabl + "<th> Inspection </th><th> Blocked </th><th> Container Seal </th></tr> </thead>";                  
                var sseal = "-"; var lseal = "-";var CSEALNO = "-";var i = 1;string consize = "";
                foreach (var result in detail)
                {

                    string scntypedesc = ""; string scnmtypedesc = "";

                    if (result.GPSCNTYPE == 1) { scntypedesc = "Yes"; } else { scntypedesc = "No"; }
                    switch (result.GPSCNMTYPE)
                    {
                        case 1:
                            scnmtypedesc = "MISMATCH";
                            break;
                        case 2:
                            scnmtypedesc = "CLEAN";
                            break;
                        case 3:
                            scnmtypedesc = "NOT SCANNED";
                            break;
                        default:
                            scnmtypedesc = "NIL";
                            break;
                    }

                    if (result.SSEALNO == null) { sseal = "-"; }
                    else { sseal = result.SSEALNO; }

                    if (result.LSEALNO == null) { lseal = "-"; }
                    else { lseal = result.LSEALNO; }
                    
                    if (result.CSEALNO == null) { CSEALNO = "-"; }
                    else { CSEALNO = result.CSEALNO; }

                    if (result.CONTNRSID == 1) { consize = "All"; }
                    else if(result.CONTNRSID == 2) { consize = "NR"; }
                    else if (result.CONTNRSID == 3) { consize = "20'"; }
                    else if (result.CONTNRSID == 4) { consize = "40'"; }
                    else { consize = "45'"; }

                    tabl = tabl + "<tbody><tr><td class=hide><input type=text id=OSDID value=" + result.OSDID + " class=OSDID name=OSDID>";
                    tabl = tabl + "<input type=text id=BILLEDID value=" + result.BILLEDID + " class=BILLEDID name=BILLEDID>";
                    tabl = tabl + "<input type=text id=FBILLEMID value=" + result.BILLEMID + " class=FBILLEMID name=FBILLEMID></td>";
                    tabl = tabl + "<td class=hide><input type=text id=F_GIDID value=" + result.GIDID + "  class=F_GIDID name=F_GIDID hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=STMRNAME value='" + result.STMRNAME + "' class=STMRNAME name=STMRNAME></td>";
                    tabl = tabl + "<td class=hide><input type=text id=IMPRTNAME value='" + result.IMPRTNAME + "' class=IMPRTNAME name=IMPRTNAME hidden>";
                    tabl = tabl + "<input type=text id=PRDTGDESC value='" + result.PRDTGDESC + "' class=PRDTGDESC name=PRDTGDESC hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=VOYNO value='" + result.VOYNO + "' class=VOYNO name=VOYNO hidden></td>";
                    tabl = tabl + "<td class=hide><input type=text id=VSLNAME value='" + result.VSLNAME + "' class=VSLNAME name=VSLNAME hidden></td>";
                    tabl = tabl + "<td>" + i + "</td>";
                    tabl = tabl + "<td><input type='text' id=CONTNRNO value=" + result.CONTNRNO + " class='CONTNRNO form-control' name=CONTNRNO readonly='readonly'></td>";
                    tabl = tabl + "<td><input type='text' value=" + consize + " id=CONTNRSDESC class='CONTNRSDESC form-control' name=CONTNRSDESC style=width:45px readonly='readonly'></td>";
                    tabl = tabl + "<td><input type='text' id=GIDATE value=" + result.GIDATE + " class='GIDATE form-control' name=GIDATE readonly='readonly'></td>";
                    tabl = tabl + "<td><input type='text' value=" + result.OSMIGMNO + " id=IGMNO class='IGMNO form-control' name=IGMNO style=width:100px readonly='readonly'></td>";
                    tabl = tabl + "<td><input type='text' id=LineNo value=" + result.OSMLNO + " class='LineNo form-control name=LineNo style=width:100px readonly='readonly'></td>";

                    tabl = tabl + "<td><input type='text' value='" + lseal + "' id=LSEALNO class='LSEALNO form-control' name=LSEALNO style=width:100px readonly='readonly'></td>";
                    tabl = tabl + "<td class='hide'><input type='text' id=SSEALNO value='" + sseal + "' class='SSEALNO form-control' name=SSEALNO style=width:100px readonly='readonly'></td>";

                    tabl = tabl + "<td><input type=text value='" + scntypedesc + "' id=GPSCNTYPE class=GPSCNTYPE name=GPSCNTYPE style=width:100px readonly></td>";
                    tabl = tabl + "<td><input type=text id=GPSCNMTYPE value='" + scnmtypedesc + "' class='GPSCNMTYPE' name=GPSCNMTYPE style=width:100px readonly></td>";

                    tabl = tabl + "<td><input type='text' id=INSPTYPE  class='INSPTYPE form-control' name=INSPTYPE value=" + result.INSP_DES + " style=width:100px readonly='readonly'></td>";
                    tabl = tabl + "<td><input type=text id=BLOCK  class='BLOCK form-control' name=BLOCK value='" + result.Block + "' style=width:100px readonly='readonly'></td>";
                    tabl = tabl + "<td><input type='number' id=CSEALNO  class='CSEALNO form-control' value='" + CSEALNO + "'  name=CSEALNO style='width:100px' maxLength='10' /></td>";
                    tabl = tabl + "</tr></tbody>";
                    i++;
                }
                tabl = tabl + "</Table>";
                Response.Write(tabl);

            }


        }

        public void ShDetail_EditData(int id)
        {
            using (SCFSERPContext contxt = new SCFSERPContext())
            {

                var detail = context.Database.SqlQuery<VW_OPENSHEET_SEAL_DETAIL_CTRL_ASSGN>("select * from VW_OPENSHEET_SEAL_DETAIL_CTRL_ASSGN where  OSMID=" + id).ToList();
                var tabl = "<div class='panel-heading navbar-inverse'  style=color:white>Seal History Details</div>";
                tabl = tabl + "<table  id='sealhmytabl' class='table table-striped table-bordered bootstrap-datatable sealhmytabl'><thead>";
                tabl = tabl + "<tr><th>S.No</th><th>Container No</th><th> Size</th>";
                tabl = tabl + "<th>Seal No</th><th></th>";
                tabl = tabl + "</tr> </thead>";
                var sseal = "-"; var lseal = "-"; var CSEALNO = "-"; var i = 1; string consize = "";
                foreach (var result in detail)
                {

                    if (result.SSEALNO == null) { sseal = "-"; }
                    else { sseal = result.SSEALNO; }

                    if (result.LSEALNO == null) { lseal = "-"; }
                    else { lseal = result.LSEALNO; }

                    if (result.CSEALNO == null) { CSEALNO = "-"; }
                    else { CSEALNO = result.CSEALNO; }

                    if (result.CONTNRSID == 1) { consize = "All"; }
                    else if (result.CONTNRSID == 2) { consize = "NR"; }
                    else if (result.CONTNRSID == 3) { consize = "20'"; }
                    else if (result.CONTNRSID == 4) { consize = "40'"; }
                    else { consize = "45'"; }

                    tabl = tabl + "<tbody id='sealbody' class='sealbody'><tr><td class=hide><input type=text id='S_OSDID' value='" + result.OSDID + "' class='S_OSDID' name='S_OSDID' />";
                    tabl = tabl + "<input type='text' id='OSSID' value='" + result.OSSID + "' class='OSSID' name='OSSID' />";                    
                    tabl = tabl + "<td class='hide'><input type='text' id='SF_GIDID' value='" + result.GIDID + "'  class='SF_GIDID' name='SF_GIDID' hidden></td>";
                    tabl = tabl + "<td class='hide'><input type='text' id='SSTMRNAME' value='" + result.STMRNAME + "' class='SSTMRNAME' name='SSTMRNAME'></td>";
                    tabl = tabl + "<td class='hide'><input type='text' id='SIMPRTNAME' value='" + result.IMPRTNAME + "' class='SIMPRTNAME' name='SIMPRTNAME' hidden>";
                    tabl = tabl + "<input type='text' id='SPRDTGDESC' value='" + result.PRDTGDESC + "' class='SPRDTGDESC' name='SPRDTGDESC' hidden></td>";
                    tabl = tabl + "<td class='hide'><input type='text' id='SVOYNO' value='" + result.VOYNO + "' class='SVOYNO' name='SVOYNO' hidden></td>";
                    tabl = tabl + "<td class='hide'><input type='text' id='SVSLNAME' value='" + result.VSLNAME + "' class='SVSLNAME' name='SVSLNAME' hidden></td>";
                    tabl = tabl + "<td>" + i + "</td>";
                    tabl = tabl + "<td><input type='text' id='SCONTNRNO' value=" + result.CONTNRNO + " class='SCONTNRNO form-control' name='SCONTNRNO' readonly='readonly'></td>";
                    tabl = tabl + "<td><input type='text' value='" + consize + "' id='SCONTNRSDESC' class='SCONTNRSDESC form-control' name='SCONTNRSDESC' style='width:45px' readonly='readonly'></td>";                    
                    tabl = tabl + "<td><input type='text' value='" + result.SealNos + "' title='" + result.SealNos + "' id='SealNos' class='SealNos form-control' name='SealNos' style='width:100px' readonly='readonly'></td>";
                    tabl = tabl + "<td><a class='' id='shdel_detail'><i class='glyphicon glyphicon-trash shdel_detail' style='color:#ff0000;font-size:large'></i></a></td>";
                    tabl = tabl + "</tr></tbody>";
                    i++;
                }
                tabl = tabl + "</table>";
                Response.Write(tabl);

            }


        }


        [Authorize(Roles = "OpenSheetPrint")]
        public void PrintView(int? id = 0)
        {
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");

            var sealno = "";

            //var query = context.Database.SqlQuery<OpenSheetSealDetails>("select * from OpenSheet_Seal_Detail where OSMID=" + id).ToList();
            var query = context.Database.SqlQuery<OpenSheetDetail>("select * from OPENSHEETDETAIL where OSMID=" + id).ToList();
            if (query.Count > 0)
            {

                foreach (var val in query)
                {
                    //sealno = sealno + "," + "'" + val.OSSDESC + "'";
                    sealno = sealno + "," + "'" + val.CSEALNO + "'";
                }
            }
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "OPENSHT_GEN", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;


                cryRpt.Load( ConfigurationManager.AppSettings["Reporturl"]+  "Import_OpenSheet_General.rpt");
                cryRpt.RecordSelectionFormula = "{VW_OSHEET_CRY_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' and {VW_OSHEET_CRY_PRINT_ASSGN.OSMID} =" + id;

                string paramName = "@FSEALNO";

                for (int i = 0; i < cryRpt.DataDefinition.FormulaFields.Count; i++)
                    if (cryRpt.DataDefinition.FormulaFields[i].FormulaName == "{" + paramName + "}")
                        cryRpt.DataDefinition.FormulaFields[i].Text = "'" + sealno + "'";
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
                cryRpt.Close();
                cryRpt.Dispose();
                GC.Collect();
                stringbuilder.Clear();
            }

        }
        [Authorize(Roles = "OpenSheetPrint")]
        public void EPrintView(int? id = 0)/*EIR*/
        {
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "OPENSHTEIR", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;


                cryRpt.Load( ConfigurationManager.AppSettings["Reporturl"]+  "Import_OpenSheet_EIR.rpt");
                cryRpt.RecordSelectionFormula = "{VW_OSHEET_CRY_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' and {VW_OSHEET_CRY_PRINT_ASSGN.OSMID} =" + id;

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
                cryRpt.Close();
                cryRpt.Dispose();
                GC.Collect();
                stringbuilder.Clear();
            }
        }
        //-----------------Delete Row-----------------
        [Authorize(Roles = "OpenSheetDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");
            String temp = Delete_fun.delete_check1(fld, id);
            var SQL = context.Database.SqlQuery<Int32>("SELECT BILLENTRYMASTER.BILLEMID FROM BILLENTRYMASTER INNER JOIN BILLENTRYDETAIL ON BILLENTRYMASTER.BILLEMID=BILLENTRYDETAIL.BILLEMID INNER JOIN OPENSHEETDETAIL ON OPENSHEETDETAIL.BILLEDID=BILLENTRYDETAIL.BILLEDID WHERE OSMID=" + Convert.ToInt32(id) + " GROUP BY BILLENTRYMASTER.BILLEMID").ToList();

            if (temp.Equals("PROCEED"))
            {
                OpenSheetMaster opensheetmasters = context.opensheetmasters.Find(Convert.ToInt32(id));
                BillEntryMaster billentrymasters = context.billentrymasters.Find(Convert.ToInt32(SQL[0]));
                context.opensheetmasters.Remove(opensheetmasters);
                context.billentrymasters.Remove(billentrymasters);
                context.SaveChanges();
                Response.Write("Deleted successfully...");
            }
            else
                Response.Write(temp);

        }//---End of Delete

        public void shDel()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");
            String temp = "";
            var SQL = context.Database.SqlQuery<OpenSheetSealDetails>("SELECT *FROM OPENSHEET_SEAL_DETAIL  WHERE OSSID=" + Convert.ToInt32(id) + "").ToList();
            if (SQL.Count > 0)
            {
                temp = "PROCEED";
                if (temp.Equals("PROCEED"))
                {
                    OpenSheetSealDetails osealdetails = context.opensheetsealdetails.Find(Convert.ToInt32(id));
                    context.opensheetsealdetails.Remove(osealdetails);

                    context.SaveChanges();
                    Response.Write("Deleted successfully...");
                }
            }
            else
                Response.Write(temp);

        }//---End of Delete

        public ActionResult GateinMaxDate(string id)
        {
            string igmno = "", gplno = "";

            var param = id.Split(';');
            igmno = (param[0]);
            gplno = (param[1]);

            var result = (from n in context.gateindetails
                          where n.IGMNO == igmno && n.GPLNO == gplno && n.SDPTID == 1
                          group n by n.IGMNO into g
                          select new { GIDATE = g.Max(t => t.GIDATE) }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EditLogOpenSheet(int? osmid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var list = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    string query = @"SELECT TOP 2000 [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                    FROM [dbo].[GateInDetailEditLog]
                                    WHERE [Modules] = 'OpenSheet'";
                    
                    if (osmid.HasValue)
                    {
                        // Find OSMDNO from OSMID
                        var openSheetRecord = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid.Value);
                        if (openSheetRecord != null && !string.IsNullOrEmpty(openSheetRecord.OSMDNO))
                        {
                            // Handle both string and integer GIDNO values in the log table
                            query += " AND (CAST([GIDNO] AS NVARCHAR(50)) = @OSMDNO_STR OR CAST([GIDNO] AS INT) = @OSMID)";
                        }
                        else
                        {
                            query += " AND CAST([GIDNO] AS INT) = @OSMID";
                        }
                    }

                    if (from.HasValue)
                        query += " AND [ChangedOn] >= @FROM";
                    if (to.HasValue)
                        query += " AND [ChangedOn] < DATEADD(day, 1, @TO)";
                    if (!string.IsNullOrWhiteSpace(user))
                        query += " AND [ChangedBy] LIKE @USERPAT";
                    if (!string.IsNullOrWhiteSpace(fieldName))
                        query += " AND [FieldName] LIKE @FIELDPAT";
                    if (!string.IsNullOrWhiteSpace(version))
                        query += " AND [Version] LIKE @VERPAT";

                    query += " AND NOT (RTRIM(LTRIM([Version])) IN ('0','V0') OR LEFT(RTRIM(LTRIM([Version])),3) IN ('v0-','V0-'))";
                    query += " ORDER BY [ChangedOn] DESC, [Version] DESC, [FieldName]";

                    using (var cmd = new SqlCommand(query, sql))
                    {
                        if (osmid.HasValue)
                        {
                            var openSheetRecord = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid.Value);
                            if (openSheetRecord != null && !string.IsNullOrEmpty(openSheetRecord.OSMDNO))
                            {
                                cmd.Parameters.AddWithValue("@OSMDNO_STR", openSheetRecord.OSMDNO);
                                cmd.Parameters.AddWithValue("@OSMID", osmid.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@OSMID", osmid.Value);
                            }
                        }
                        if (from.HasValue)
                            cmd.Parameters.AddWithValue("@FROM", from.Value);
                        if (to.HasValue)
                            cmd.Parameters.AddWithValue("@TO", to.Value);
                        if (!string.IsNullOrWhiteSpace(user))
                            cmd.Parameters.AddWithValue("@USERPAT", "%" + user + "%");
                        if (!string.IsNullOrWhiteSpace(fieldName))
                            cmd.Parameters.AddWithValue("@FIELDPAT", "%" + fieldName + "%");
                        if (!string.IsNullOrWhiteSpace(version))
                            cmd.Parameters.AddWithValue("@VERPAT", "%" + version + "%");

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
                                    Version = Convert.ToString(r["Version"]),
                                    Modules = r["Modules"] == DBNull.Value ? null : Convert.ToString(r["Modules"])
                                });
                            }
                        }
                    }
                }
            }

            // Map technical field names to friendly form labels and raw codes to display values
            try
            {
                // Build lookup dictionaries
                var dictCHA = context.categorymasters.Where(c => c.CATETID == 4 && c.DISPSTATUS == 0)
                    .GroupBy(x => x.CATEID)
                    .ToDictionary(g => g.Key, g => g.First().CATENAME);
                
                var dictCategory = context.categorymasters.Where(c => c.CATETID == 6 && c.DISPSTATUS == 0)
                    .GroupBy(x => x.CATEID)
                    .ToDictionary(g => g.Key, g => g.First().CATENAME);
                
                var dictUnit = context.unitmasters.Where(u => u.DISPSTATUS == 0)
                    .GroupBy(x => x.UNITID)
                    .ToDictionary(g => g.Key, g => g.First().UNITDESC);
                
                var dictViaType = context.opensheetviadetails
                    .GroupBy(x => x.OSMTYPE)
                    .ToDictionary(g => g.Key, g => g.First().OSMTYPEDESC);

                string Map(string field, string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return raw;
                    int ival;
                    // Handle Detail fields (prefixed with "Detail.")
                    var fieldNameLocal = field;
                    if (field != null && field.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase))
                    {
                        fieldNameLocal = field.Substring(7); // Remove "Detail." prefix
                    }
                    switch (fieldNameLocal?.ToUpperInvariant())
                    {
                        case "CHAID":
                            return int.TryParse(raw, out ival) && dictCHA.ContainsKey(ival) ? dictCHA[ival] : raw;
                        case "OSMLCATEID":
                        case "LCATEID":
                            return int.TryParse(raw, out ival) && dictCategory.ContainsKey(ival) ? dictCategory[ival] : raw;
                        case "OSMUNITID":
                        case "UNITID":
                            return int.TryParse(raw, out ival) && dictUnit.ContainsKey(ival) ? dictUnit[ival] : raw;
                        case "OSMTYPE":
                            // Lookup OpenSheetType description
                            return int.TryParse(raw, out ival) && dictViaType.ContainsKey(ival) ? dictViaType[ival] : raw;
                        case "DOTYPE":
                            return raw == "1" ? "Yes" : raw == "0" ? "No" : raw;
                        case "SCTYPE":
                            return raw == "1" ? "Yes" : raw == "0" ? "No" : raw;
                        case "OSMLDTYPE":
                            return raw == "1" ? "LCL" : raw == "0" ? "FCL" : raw;
                        case "OSBILLEDTO":
                            return raw == "1" ? "IMPORTER" : raw == "0" ? "CHA" : raw;
                        case "INSPTYPE":
                            return raw == "1" ? "Yes" : raw == "0" ? "No" : raw;
                        case "DISPSTATUS":
                            return raw == "1" ? "CANCELLED" : raw == "0" ? "INBOOKS" : raw;
                        default:
                            return raw;
                    }
                }

                string Friendly(string field)
                {
                    if (string.IsNullOrWhiteSpace(field)) return field;
                    var f = field.Trim();
                    // Handle Detail fields (prefixed with "Detail.")
                    if (f.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase))
                    {
                        f = f.Substring(7); // Remove "Detail." prefix
                    }
                    switch (f.ToUpperInvariant())
                    {
                        // Master fields
                        case "OSMDATE": return "Open Sheet Date";
                        case "OSMTIME": return "Open Sheet Time";
                        case "OSMNO": return "Open Sheet No";
                        case "OSMDNO": return "Open Sheet Display No";
                        case "CHAID": return "CHA(Tally)";
                        case "OSMNAME": return "CHA(Licence Name)";
                        case "OSMCNAME": return "CHA Staff Name";
                        case "OSMLNAME": return "Open Sheet L Name";
                        case "OSMIGMNO": return "IGM No";
                        case "OSMVSLNAME": return "Vessel Name";
                        case "BOENO": return "BOE No";
                        case "BOEDATE": return "BOE Date";
                        case "OSMTYPE": return "Via";
                        case "DOTYPE": return "DO Liner";
                        case "DODATE": return "Validity Date";
                        case "DONO": return "DO No";
                        case "DOIDATE": return "DO Issue Date";
                        case "SCTYPE": return "Seal Cut Type";
                        case "SCDATE": return "Seal Cut Date";
                        case "SCTIME": return "Seal Cut Time";
                        case "SCDESC": return "Seal No's";
                        case "SCREMRKS": return "Remarks";
                        case "OSMBLNO": return "BL No";
                        case "OSMBLDATE": return "BL Date";
                        case "OSMIGMDATE": return "IGM Date";
                        case "OSMLNO": return "Line No";
                        case "OSMUNITID": return "Unit";
                        case "OSMLCATEID": return "Labour";
                        case "OSMAAMT": return "Assessable Value";
                        case "OSMWGHT": return "Weight";
                        case "OSMNOP": return "Nop";
                        case "OSMTNOC": return "Total Number of Containers";
                        case "OSMFNOC": return "Full Number of Containers";
                        case "OSMLDTYPE": return "Load Type";
                        case "OSBILLEDTO": return "Billed To";
                        case "OSBILLREFNAME": return "Billing Name";
                        case "OSMDUTYAMT": return "Duty Amount";
                        case "OSBCHASTATEID": return "State";
                        case "OSBBCHASTATEID": return "Billing State Code";
                        case "OSBCHACATEAID": return "CHA Address type";
                        case "OSBBCHACATEAID": return "CHA Address type";
                        case "OSBCHACATEAGSTNO": return "CHA GST No";
                        case "OSBBCHACATEAGSTNO": return "Billing GST No";
                        case "OSBCHAADDR1": return "CHA Address 1";
                        case "OSBCHAADDR2": return "CHA Address 2";
                        case "OSBCHAADDR3": return "CHA Address 3";
                        case "OSBCHAADDR4": return "CHA Address 4";
                        case "OSBBCHAADDR1": return "Billing Address 1";
                        case "OSBBCHAADDR2": return "Billing Address 2";
                        case "OSBBCHAADDR3": return "Billing Address 3";
                        case "OSBBCHAADDR4": return "Billing Address 4";
                        case "OOCNO": return "OOC No";
                        case "OOCDATE": return "OOC Date";
                        case "DISPSTATUS": return "Status";
                        // Detail fields
                        case "GIDID": return "Gate In Detail ID";
                        case "GIDATE": return "Gate In Date";
                        case "LSEALNO": return "Liner Seal No";
                        case "SSEALNO": return "SANCO Seal No";
                        case "CSEALNO": return "Custom Seal No";
                        case "BILLEDID": return "Billed ID";
                        case "PRDTGID": return "Product Category";
                        default: return field.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase) ? "Detail." + f : field;
                    }
                }

                foreach (var row in list)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
            }
            catch { /* Best-effort mapping; do not fail page if lookups have issues */ }

            ViewBag.Module = "OpenSheet";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        public ActionResult EditLogOpenSheetCompare(int? osmid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            // Fallbacks: try alternate parameter names
            if (osmid == null)
            {
                int tmp;
                var qsOsmid = Request["osmid"] ?? Request["id"];
                if (!string.IsNullOrWhiteSpace(qsOsmid) && int.TryParse(qsOsmid, out tmp))
                {
                    osmid = tmp;
                }
            }

            if (osmid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide OSMID, Version A and Version B to compare.";
                return RedirectToAction("EditLogOpenSheet", new { osmid = osmid });
            }

            // Get OSMDNO from OSMID
            string osmdnoString = osmid.Value.ToString();
            var openSheetRecord = context.opensheetmasters.AsNoTracking().FirstOrDefault(x => x.OSMID == osmid.Value);
            if (openSheetRecord != null && !string.IsNullOrEmpty(openSheetRecord.OSMDNO))
            {
                osmdnoString = openSheetRecord.OSMDNO;
            }

            // Normalize version strings
            versionA = (versionA ?? string.Empty).Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "");
            versionB = (versionB ?? string.Empty).Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "");
            
            // Map '0' or 'v0'/'V0' to 'v0-<OSMDNO>' for baseline comparisons
            if (osmid.HasValue)
            {
                var baseLabel = "v0-" + osmdnoString;
                if (string.Equals(versionA, "0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionA, "V0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionA, "v0", StringComparison.OrdinalIgnoreCase))
                    versionA = baseLabel;
                if (string.Equals(versionB, "0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionB, "V0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionB, "v0", StringComparison.OrdinalIgnoreCase))
                    versionB = baseLabel;
            }

            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            var rowsA = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var rowsB = new List<scfs_erp.Models.GateInDetailEditLogRow>();

            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    // Fetch version A (case-insensitive and trimmed comparison)
                    using (var cmd = new SqlCommand(@"
                        SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                        FROM [dbo].[GateInDetailEditLog]
                        WHERE (CAST([GIDNO] AS NVARCHAR(50)) = @OSMDNO OR CAST([GIDNO] AS INT) = @OSMID) 
                          AND [Modules] = 'OpenSheet' 
                          AND LOWER(RTRIM(LTRIM([Version]))) = LOWER(@VersionA)
                        ORDER BY [FieldName]", sql))
                    {
                        cmd.Parameters.AddWithValue("@OSMDNO", osmdnoString);
                        cmd.Parameters.AddWithValue("@OSMID", osmid.Value);
                        cmd.Parameters.AddWithValue("@VersionA", versionA.Trim());
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                rowsA.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = Convert.ToString(r["GIDNO"]),
                                    FieldName = Convert.ToString(r["FieldName"]),
                                    OldValue = r["OldValue"] == DBNull.Value ? null : Convert.ToString(r["OldValue"]),
                                    NewValue = r["NewValue"] == DBNull.Value ? null : Convert.ToString(r["NewValue"]),
                                    ChangedBy = Convert.ToString(r["ChangedBy"]),
                                    ChangedOn = r["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r["ChangedOn"]) : DateTime.MinValue,
                                    Version = Convert.ToString(r["Version"]),
                                    Modules = r["Modules"] == DBNull.Value ? null : Convert.ToString(r["Modules"])
                                });
                            }
                        }
                    }
                    // Fetch version B (case-insensitive and trimmed comparison)
                    using (var cmd = new SqlCommand(@"
                        SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                        FROM [dbo].[GateInDetailEditLog]
                        WHERE (CAST([GIDNO] AS NVARCHAR(50)) = @OSMDNO OR CAST([GIDNO] AS INT) = @OSMID) 
                          AND [Modules] = 'OpenSheet' 
                          AND LOWER(RTRIM(LTRIM([Version]))) = LOWER(@VersionB)
                        ORDER BY [FieldName]", sql))
                    {
                        cmd.Parameters.AddWithValue("@OSMDNO", osmdnoString);
                        cmd.Parameters.AddWithValue("@OSMID", osmid.Value);
                        cmd.Parameters.AddWithValue("@VersionB", versionB.Trim());
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                rowsB.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = Convert.ToString(r["GIDNO"]),
                                    FieldName = Convert.ToString(r["FieldName"]),
                                    OldValue = r["OldValue"] == DBNull.Value ? null : Convert.ToString(r["OldValue"]),
                                    NewValue = r["NewValue"] == DBNull.Value ? null : Convert.ToString(r["NewValue"]),
                                    ChangedBy = Convert.ToString(r["ChangedBy"]),
                                    ChangedOn = r["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r["ChangedOn"]) : DateTime.MinValue,
                                    Version = Convert.ToString(r["Version"]),
                                    Modules = r["Modules"] == DBNull.Value ? null : Convert.ToString(r["Modules"])
                                });
                            }
                        }
                    }
                }
            }

            // Apply friendly name mapping (same as EditLogOpenSheet)
            try
            {
                var dictCHA = context.categorymasters.Where(c => c.CATETID == 4 && c.DISPSTATUS == 0)
                    .GroupBy(x => x.CATEID)
                    .ToDictionary(g => g.Key, g => g.First().CATENAME);
                
                var dictCategory = context.categorymasters.Where(c => c.CATETID == 6 && c.DISPSTATUS == 0)
                    .GroupBy(x => x.CATEID)
                    .ToDictionary(g => g.Key, g => g.First().CATENAME);
                
                var dictUnit = context.unitmasters.Where(u => u.DISPSTATUS == 0)
                    .GroupBy(x => x.UNITID)
                    .ToDictionary(g => g.Key, g => g.First().UNITDESC);

                string Map(string field, string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return raw;
                    int ival;
                    var fieldNameLocal = field;
                    if (field != null && field.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase))
                    {
                        fieldNameLocal = field.Substring(7);
                    }
                    switch (fieldNameLocal?.ToUpperInvariant())
                    {
                        case "CHAID":
                            return int.TryParse(raw, out ival) && dictCHA.ContainsKey(ival) ? dictCHA[ival] : raw;
                        case "OSMLCATEID":
                        case "LCATEID":
                            return int.TryParse(raw, out ival) && dictCategory.ContainsKey(ival) ? dictCategory[ival] : raw;
                        case "OSMUNITID":
                        case "UNITID":
                            return int.TryParse(raw, out ival) && dictUnit.ContainsKey(ival) ? dictUnit[ival] : raw;
                        case "DOTYPE":
                            return raw == "1" ? "Yes" : raw == "0" ? "No" : raw;
                        case "SCTYPE":
                            return raw == "1" ? "Yes" : raw == "0" ? "No" : raw;
                        case "OSMLDTYPE":
                            return raw == "1" ? "LCL" : raw == "0" ? "FCL" : raw;
                        case "OSBILLEDTO":
                            return raw == "1" ? "IMPORTER" : raw == "0" ? "CHA" : raw;
                        case "INSPTYPE":
                            return raw == "1" ? "Yes" : raw == "0" ? "No" : raw;
                        case "DISPSTATUS":
                            return raw == "1" ? "CANCELLED" : raw == "0" ? "INBOOKS" : raw;
                        default:
                            return raw;
                    }
                }

                string Friendly(string field)
                {
                    if (string.IsNullOrWhiteSpace(field)) return field;
                    var f = field.Trim();
                    if (f.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase))
                    {
                        f = f.Substring(7);
                    }
                    switch (f.ToUpperInvariant())
                    {
                        case "OSMDATE": return "Open Sheet Date";
                        case "OSMTIME": return "Open Sheet Time";
                        case "OSMNO": return "Open Sheet No";
                        case "OSMDNO": return "Open Sheet Display No";
                        case "CHAID": return "CHA(Tally)";
                        case "OSMNAME": return "CHA(Licence Name)";
                        case "OSMCNAME": return "CHA Staff Name";
                        case "OSMLNAME": return "Open Sheet L Name";
                        case "OSMIGMNO": return "IGM No";
                        case "OSMVSLNAME": return "Vessel Name";
                        case "BOENO": return "BOE No";
                        case "BOEDATE": return "BOE Date";
                        case "OSMTYPE": return "Via";
                        case "DOTYPE": return "DO Liner";
                        case "DODATE": return "Validity Date";
                        case "DONO": return "DO No";
                        case "DOIDATE": return "DO Issue Date";
                        case "SCTYPE": return "Seal Cut Type";
                        case "SCDATE": return "Seal Cut Date";
                        case "SCTIME": return "Seal Cut Time";
                        case "SCDESC": return "Seal No's";
                        case "SCREMRKS": return "Remarks";
                        case "OSMBLNO": return "BL No";
                        case "OSMBLDATE": return "BL Date";
                        case "OSMIGMDATE": return "IGM Date";
                        case "OSMLNO": return "Line No";
                        case "OSMUNITID": return "Unit";
                        case "OSMLCATEID": return "Labour";
                        case "OSMAAMT": return "Assessable Value";
                        case "OSMWGHT": return "Weight";
                        case "OSMNOP": return "Nop";
                        case "OSMTNOC": return "Total Number of Containers";
                        case "OSMFNOC": return "Full Number of Containers";
                        case "OSMLDTYPE": return "Load Type";
                        case "OSBILLEDTO": return "Billed To";
                        case "OSBILLREFNAME": return "Billing Name";
                        case "OSMDUTYAMT": return "Duty Amount";
                        case "OSBCHASTATEID": return "State";
                        case "OSBBCHASTATEID": return "Billing State Code";
                        case "OSBCHACATEAID": return "CHA Address type";
                        case "OSBBCHACATEAID": return "CHA Address type";
                        case "OSBCHACATEAGSTNO": return "CHA GST No";
                        case "OSBBCHACATEAGSTNO": return "Billing GST No";
                        case "OSBCHAADDR1": return "CHA Address 1";
                        case "OSBCHAADDR2": return "CHA Address 2";
                        case "OSBCHAADDR3": return "CHA Address 3";
                        case "OSBCHAADDR4": return "CHA Address 4";
                        case "OSBBCHAADDR1": return "Billing Address 1";
                        case "OSBBCHAADDR2": return "Billing Address 2";
                        case "OSBBCHAADDR3": return "Billing Address 3";
                        case "OSBBCHAADDR4": return "Billing Address 4";
                        case "OOCNO": return "OOC No";
                        case "OOCDATE": return "OOC Date";
                        case "DISPSTATUS": return "Status";
                        case "GIDID": return "Gate In Detail ID";
                        case "GIDATE": return "Gate In Date";
                        case "LSEALNO": return "Liner Seal No";
                        case "SSEALNO": return "SANCO Seal No";
                        case "CSEALNO": return "Custom Seal No";
                        case "BILLEDID": return "Billed ID";
                        case "PRDTGID": return "Product Category";
                        default: return field.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase) ? "Detail." + f : field;
                    }
                }

                foreach (var row in rowsA)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
                foreach (var row in rowsB)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
            }
            catch { /* Best-effort mapping; do not fail page if lookups have issues */ }

            ViewBag.GIDNO = osmid.Value;
            ViewBag.VersionA = versionA.Trim();
            ViewBag.VersionB = versionB.Trim();
            ViewBag.RowsA = rowsA;
            ViewBag.RowsB = rowsB;
            ViewBag.Module = "OpenSheet";

            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        private int CalculateNextVersion(string osmdno)
        {
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return 1;
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
                    WHERE [GIDNO] = @OSMDNO AND [Modules] = 'OpenSheet'", sql))
                {
                    cmd.Parameters.AddWithValue("@OSMDNO", osmdno);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        return Convert.ToInt32(obj);
                }
            }
            catch { }
            return 1;
        }

        private void LogOpenSheetEdits(OpenSheetMaster before, OpenSheetMaster after, string userId, int nextVersion)
        {
            if (before == null || after == null)
            {
                System.Diagnostics.Debug.WriteLine($"LogOpenSheetEdits: before={before == null}, after={after == null}");
                return;
            }
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine($"LogOpenSheetEdits: Connection string is null or empty");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"LogOpenSheetEdits: Starting for OSMDNO={after.OSMDNO}, nextVersion={nextVersion}, userId={userId}");

            // Exclude only truly system/internal fields - MINIMIZE exclusions to capture ALL user-editable fields
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "OSMID",  // Primary key
                "PRCSDATE", // System timestamp
                "LMUSRID",  // Audit field
                "CUSRID",   // Audit field
                "COMPYID",  // System field
                "SDPTID"    // System field
                // Removed exclusions: OSBCHACATEAID, OSBBCHACATEAID, OSBILLREFID, OSMLCATENAME - these should be logged
            };

            var props = typeof(OpenSheetMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int changeCount = 0;
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType)
                    continue;
                if (exclude.Contains(p.Name)) continue;

                var ov = p.GetValue(before, null);
                var nv = p.GetValue(after, null);

                if (BothNull(ov, nv)) continue;

                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                bool changed;

                if (type == typeof(decimal) || type == typeof(decimal?))
                {
                    var d1 = ToNullableDecimal(ov) ?? 0m;
                    var d2 = ToNullableDecimal(nv) ?? 0m;
                    changed = d1 != d2;
                }
                else if (type == typeof(double) || type == typeof(float))
                {
                    var d1 = Convert.ToDouble(ov ?? 0.0);
                    var d2 = Convert.ToDouble(nv ?? 0.0);
                    changed = Math.Abs(d1 - d2) > 1e-9;
                }
                else if (type == typeof(int) || type == typeof(int?) || type == typeof(long) || type == typeof(long?) || type == typeof(short) || type == typeof(short?))
                {
                    var i1 = ov == null ? (long?)null : Convert.ToInt64(ov);
                    var i2 = nv == null ? (long?)null : Convert.ToInt64(nv);
                    if (!i1.HasValue && !i2.HasValue) continue;
                    var val1 = i1 ?? 0;
                    var val2 = i2 ?? 0;
                    changed = val1 != val2;
                }
                else if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    var t1 = (ov as DateTime?) ?? default(DateTime);
                    var t2 = (nv as DateTime?) ?? default(DateTime);
                    if (t1 == default(DateTime) && t2 == default(DateTime)) continue;
                    if (p.Name.Contains("DATE") && !p.Name.Contains("TIME"))
                    {
                        changed = t1.Date != t2.Date;
                    }
                    else
                    {
                        t1 = new DateTime(t1.Year, t1.Month, t1.Day, t1.Hour, t1.Minute, t1.Second);
                        t2 = new DateTime(t2.Year, t2.Month, t2.Day, t2.Hour, t2.Minute, t2.Second);
                        changed = t1 != t2;
                    }
                }
                else if (type == typeof(string))
                {
                    var s1 = (Convert.ToString(ov) ?? string.Empty).Trim();
                    var s2 = (Convert.ToString(nv) ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) continue;
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

                var versionLabel = $"V{nextVersion}-{after.OSMDNO}";
                try
                {
                    System.Diagnostics.Debug.WriteLine($"OpenSheet LogOpenSheetEdits: Logging change for field {p.Name}, OldValue={os}, NewValue={ns}, Version={versionLabel}");
                    InsertEditLogRow(cs.ConnectionString, after.OSMDNO, p.Name, os, ns, userId, versionLabel, "OpenSheet");
                    changeCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OpenSheet InsertEditLogRow failed for field {p.Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"OpenSheet LogOpenSheetEdits: Completed logging for OSMDNO={after.OSMDNO}, Version={nextVersion}, Total changes logged={changeCount}");
            
            // Manually log OSBCHACATEAID (CHA Address type), OSBBCHACATEAID (Billing Address type), and OSBCHASTATEID (State) with display values
            try
            {
                var versionLabel = $"V{nextVersion}-{after.OSMDNO}";
                
                // Log CHA Address type
                if (after.OSBCHACATEAID.HasValue && after.OSBCHACATEAID.Value > 0)
                {
                    var chaAddressType = context.categoryaddressdetails.AsNoTracking()
                        .Where(c => c.CATEAID == after.OSBCHACATEAID.Value)
                        .Select(c => c.CATEATYPEDESC)
                        .FirstOrDefault();
                    if (!string.IsNullOrEmpty(chaAddressType))
                    {
                        var oldChaAddressType = before != null && before.OSBCHACATEAID.HasValue && before.OSBCHACATEAID.Value > 0 ?
                            (context.categoryaddressdetails.AsNoTracking()
                                .Where(c => c.CATEAID == before.OSBCHACATEAID.Value)
                                .Select(c => c.CATEATYPEDESC)
                                .FirstOrDefault() ?? "") : "";
                        if (oldChaAddressType != chaAddressType)
                        {
                            InsertEditLogRow(cs.ConnectionString, after.OSMDNO, "OSBCHACATEAID", oldChaAddressType, chaAddressType, userId, versionLabel, "OpenSheet");
                        }
                    }
                }
                
                // Log Billing Address type
                if (after.OSBBCHACATEAID.HasValue && after.OSBBCHACATEAID.Value > 0)
                {
                    var billingAddressType = context.categoryaddressdetails.AsNoTracking()
                        .Where(c => c.CATEAID == after.OSBBCHACATEAID.Value)
                        .Select(c => c.CATEATYPEDESC)
                        .FirstOrDefault();
                    if (!string.IsNullOrEmpty(billingAddressType))
                    {
                        var oldBillingAddressType = before != null && before.OSBBCHACATEAID.HasValue && before.OSBBCHACATEAID.Value > 0 ?
                            (context.categoryaddressdetails.AsNoTracking()
                                .Where(c => c.CATEAID == before.OSBBCHACATEAID.Value)
                                .Select(c => c.CATEATYPEDESC)
                                .FirstOrDefault() ?? "") : "";
                        if (oldBillingAddressType != billingAddressType)
                        {
                            InsertEditLogRow(cs.ConnectionString, after.OSMDNO, "OSBBCHACATEAID", oldBillingAddressType, billingAddressType, userId, versionLabel, "OpenSheet");
                        }
                    }
                }
                
                // Log State (OSBCHASTATEID)
                if (after.OSBCHASTATEID > 0)
                {
                    var chaState = context.statemasters.AsNoTracking()
                        .Where(s => s.STATEID == after.OSBCHASTATEID)
                        .Select(s => s.STATEDESC)
                        .FirstOrDefault();
                    if (!string.IsNullOrEmpty(chaState))
                    {
                        var oldChaState = before != null && before.OSBCHASTATEID > 0 ?
                            (context.statemasters.AsNoTracking()
                                .Where(s => s.STATEID == before.OSBCHASTATEID)
                                .Select(s => s.STATEDESC)
                                .FirstOrDefault() ?? "") : "";
                        if (oldChaState != chaState)
                        {
                            InsertEditLogRow(cs.ConnectionString, after.OSMDNO, "OSBCHASTATEID", oldChaState, chaState, userId, versionLabel, "OpenSheet");
                        }
                    }
                }
                
                // Log Billing State (OSBBCHASTATEID)
                if (after.OSBBCHASTATEID > 0)
                {
                    var billingState = context.statemasters.AsNoTracking()
                        .Where(s => s.STATEID == after.OSBBCHASTATEID)
                        .Select(s => s.STATEDESC)
                        .FirstOrDefault();
                    if (!string.IsNullOrEmpty(billingState))
                    {
                        var oldBillingState = before != null && before.OSBBCHASTATEID > 0 ?
                            (context.statemasters.AsNoTracking()
                                .Where(s => s.STATEID == before.OSBBCHASTATEID)
                                .Select(s => s.STATEDESC)
                                .FirstOrDefault() ?? "") : "";
                        if (oldBillingState != billingState)
                        {
                            InsertEditLogRow(cs.ConnectionString, after.OSMDNO, "OSBBCHASTATEID", oldBillingState, billingState, userId, versionLabel, "OpenSheet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log OpenSheet address/state fields: {ex.Message}");
            }
        }

        private void LogOpenSheetDetailEdits(OpenSheetDetail before, OpenSheetDetail after, string osmdno, string userId, int nextVersion)
        {
            if (after == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            // Exclude only truly system/internal fields
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "OSDID",  // Primary key
                "OSMID",  // Foreign key to master (we use OSMDNO in log)
                "PRCSDATE", // System timestamp
                "BILLEDID",  // Link to BillEntryDetail
                "CUSRID",  // Audit field
                "LMUSRID"  // Audit field
            };

            var props = typeof(OpenSheetDetail).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType)
                    continue;
                if (exclude.Contains(p.Name)) continue;

                var ov = before != null ? p.GetValue(before, null) : null;
                var nv = p.GetValue(after, null);

                if (BothNull(ov, nv)) continue;

                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                bool changed;

                if (type == typeof(decimal) || type == typeof(decimal?))
                {
                    var d1 = ToNullableDecimal(ov) ?? 0m;
                    var d2 = ToNullableDecimal(nv) ?? 0m;
                    changed = d1 != d2;
                }
                else if (type == typeof(double) || type == typeof(float))
                {
                    var d1 = Convert.ToDouble(ov ?? 0.0);
                    var d2 = Convert.ToDouble(nv ?? 0.0);
                    changed = Math.Abs(d1 - d2) > 1e-9;
                }
                else if (type == typeof(int) || type == typeof(int?) || type == typeof(long) || type == typeof(long?) || type == typeof(short) || type == typeof(short?))
                {
                    var i1 = ov == null ? (long?)null : Convert.ToInt64(ov);
                    var i2 = nv == null ? (long?)null : Convert.ToInt64(nv);
                    if (!i1.HasValue && !i2.HasValue) continue;
                    var val1 = i1 ?? 0;
                    var val2 = i2 ?? 0;
                    changed = val1 != val2;
                }
                else if (type == typeof(DateTime) || type == typeof(DateTime?))
                {
                    var t1 = (ov as DateTime?) ?? default(DateTime);
                    var t2 = (nv as DateTime?) ?? default(DateTime);
                    if (t1 == default(DateTime) && t2 == default(DateTime)) continue;
                    if (p.Name.Contains("DATE") && !p.Name.Contains("TIME"))
                    {
                        changed = t1.Date != t2.Date;
                    }
                    else
                    {
                        t1 = new DateTime(t1.Year, t1.Month, t1.Day, t1.Hour, t1.Minute, t1.Second);
                        t2 = new DateTime(t2.Year, t2.Month, t2.Day, t2.Hour, t2.Minute, t2.Second);
                        changed = t1 != t2;
                    }
                }
                else if (type == typeof(string))
                {
                    var s1 = (Convert.ToString(ov) ?? string.Empty).Trim();
                    var s2 = (Convert.ToString(nv) ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) continue;
                    changed = !string.Equals(s1, s2, StringComparison.Ordinal);
                }
                else
                {
                    var s1 = FormatVal(ov);
                    var s2 = FormatVal(nv);
                    changed = !string.Equals(s1, s2, StringComparison.Ordinal);
                }

                if (!changed) continue;

                // Prefix field name with "Detail." to distinguish from master fields
                var fieldName = "Detail." + p.Name;
                var os = FormatValForLoggingDetail(p.Name, ov);
                var ns = FormatValForLoggingDetail(p.Name, nv);

                var versionLabel = $"V{nextVersion}-{osmdno}";
                InsertEditLogRow(cs.ConnectionString, osmdno, fieldName, os, ns, userId, versionLabel, "OpenSheet");
            }
        }

        private string FormatValForLoggingDetail(string fieldName, object val)
        {
            if (val == null) return null;
            var type = val.GetType();
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                var dt = (DateTime)val;
                if (fieldName.Contains("DATE") && !fieldName.Contains("TIME"))
                    return dt.ToString("yyyy-MM-dd");
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            if (type == typeof(decimal) || type == typeof(decimal?))
                return ((decimal)val).ToString("0.00");
            if (type == typeof(double) || type == typeof(float))
                return Convert.ToDouble(val).ToString("0.00");
            return FormatVal(val);
        }

        private void EnsureBaselineVersionZero(OpenSheetMaster snapshot, string userId)
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
                if (string.IsNullOrWhiteSpace(snapshot.OSMDNO)) return;

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[GateInDetailEditLog] WHERE [GIDNO]=@OSMDNO AND [Modules]='OpenSheet' AND (RTRIM(LTRIM([Version]))=@VLower OR RTRIM(LTRIM([Version]))=@VUpper OR RTRIM(LTRIM([Version]))='0' OR RTRIM(LTRIM([Version]))='V0')", sql))
                {
                    cmd.Parameters.AddWithValue("@OSMDNO", snapshot.OSMDNO);
                    var baselineVerLower = "v0-" + snapshot.OSMDNO;
                    var baselineVerUpper = "V0-" + snapshot.OSMDNO;
                    cmd.Parameters.AddWithValue("@VLower", baselineVerLower);
                    cmd.Parameters.AddWithValue("@VUpper", baselineVerUpper);
                    sql.Open();
                    var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    if (exists) return;
                }

                InsertBaselineSnapshot(snapshot, userId, "OpenSheet");
            }
            catch { /* Best-effort; do not fail if baseline creation has issues */ }
        }

        private void InsertBaselineSnapshot(OpenSheetMaster snapshot, string userId, string module)
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
                if (string.IsNullOrWhiteSpace(snapshot.OSMDNO)) return;

                var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "OSMID", "PRCSDATE", "LMUSRID", "CUSRID", "COMPYID", "SDPTID",
                    "OSBCHACATEAID", "OSBBCHACATEAID", "OSBILLREFID", "OSMLCATENAME"
                };

                var props = typeof(OpenSheetMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var versionLabel = $"v0-{snapshot.OSMDNO}";

                foreach (var p in props)
                {
                    if (!p.CanRead) continue;
                    if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType)
                        continue;
                    if (exclude.Contains(p.Name)) continue;

                    var val = p.GetValue(snapshot, null);
                    var formatted = FormatValForLogging(p.Name, val);
                    if (formatted != null)
                    {
                        InsertEditLogRow(cs.ConnectionString, snapshot.OSMDNO, p.Name, formatted, formatted, userId, versionLabel, module);
                    }
                }
            }
            catch { /* Best-effort; do not fail if baseline creation has issues */ }
        }

        private string FormatValForLogging(string fieldName, object val)
        {
            if (val == null) return null;
            var type = val.GetType();
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                var dt = (DateTime)val;
                if (fieldName.Contains("DATE") && !fieldName.Contains("TIME"))
                    return dt.ToString("yyyy-MM-dd");
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            if (type == typeof(decimal) || type == typeof(decimal?))
                return ((decimal)val).ToString("0.00");
            if (type == typeof(double) || type == typeof(float))
                return Convert.ToDouble(val).ToString("0.00");
            return FormatVal(val);
        }

        private string FormatVal(object val)
        {
            if (val == null) return null;
            if (val is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            if (val is decimal dec)
                return dec.ToString("0.00");
            if (val is double dbl)
                return dbl.ToString("0.00");
            return Convert.ToString(val);
        }

        private bool BothNull(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == DBNull.Value && b == null) return true;
            if (a == null && b == DBNull.Value) return true;
            if (a == DBNull.Value && b == DBNull.Value) return true;
            return false;
        }

        private decimal? ToNullableDecimal(object val)
        {
            if (val == null || val == DBNull.Value) return null;
            try { return Convert.ToDecimal(val); }
            catch { return null; }
        }

        private void InsertEditLogRow(string connectionString, string gidno, string fieldName, string oldValue, string newValue, string changedBy, string version, string modules)
        {
            try
            {
                using (var sql = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(@"
                    INSERT INTO [dbo].[GateInDetailEditLog] ([GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules])
                    VALUES (@GIDNO, @FieldName, @OldValue, @NewValue, @ChangedBy, @ChangedOn, @Version, @Modules)", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", gidno ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FieldName", fieldName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OldValue", oldValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", newValue ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChangedOn", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Version", version ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Modules", modules ?? (object)DBNull.Value);
                    sql.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InsertEditLogRow failed: {ex.Message}");
            }
        }

    }//---End of Class
}//--End of namespace