using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using scfs_erp.Context;
using scfs_erp.Helper;
using scfs_erp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using scfs.Data;

namespace scfs_erp.Controllers.Import
{
    [SessionExpire]
    public class ImportDeStuffController : Controller
    {
        // GET: ImportDeStuff

        #region Context declaration
        SCFSERPContext context = new SCFSERPContext();
        #endregion

        #region Index Page
        [Authorize(Roles = "ImportDeStuffSlipIndex")]
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

            DateTime fromdate = Convert.ToDateTime(Session["SDATE"]).Date;
            DateTime todate = Convert.ToDateTime(Session["EDATE"]).Date;


            TotalContainerDetails(fromdate, todate);

            return View();
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


            var result = context.Database.SqlQuery<PR_IMPORT_LOADESTUFFTOTCONTAINER_DETAILS_Result>("EXEC PR_IMPORT_LOADESTUFFTOTCONTAINER_DETAILS @PFDT='" + fdate + "',@PTDT='" + tdate + "',@PSDPTID=" + 1).ToList();

            foreach (var rslt in result)
            {
                if ((rslt.Sno == 2) && (rslt.Descriptn == "IMPORT - DESTUFF"))
                {
                    @ViewBag.Total20 = rslt.c_20;
                    @ViewBag.Total40 = rslt.c_40;
                    @ViewBag.Total45 = rslt.c_45;
                    @ViewBag.TotalTues = rslt.c_tues;

                    Session["ID20"] = rslt.c_20;
                    Session["ID40"] = rslt.c_40;
                    Session["ID45"] = rslt.c_45;
                    Session["IDTU"] = rslt.c_tues;
                }

            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        #endregion

        #region GetAjaxData
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {

            using (var e = new CFSImportEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                var data = e.pr_Search_Import_DeStuffSlip(param.sSearch,
                                                Convert.ToInt32(Request["iSortCol_0"]),
                                                Request["sSortDir_0"],
                                                param.iDisplayStart,
                                                param.iDisplayStart + param.iDisplayLength,
                                                totalRowsCount,
                                                filteredRowsCount, Convert.ToInt32(Session["compyid"]), Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]));

                var aaData = data.Select(d => new string[] { d.ASLMDATE.Value.ToString("dd/MM/yyyy"), d.ASLMDNO, d.CONTNRNO, d.CONTNRSDESC, d.CHANAME, d.BOENO, d.IGMNO, d.GPLNO, d.DISPSTATUS.ToString(), d.ASLMID.ToString(), d.ASLDID.ToString(), d.DOSTS }).ToArray();
                //var aaData = data.Select(d => new { ASLMDATE = d.ASLMDATE.Value.ToString("dd-MM-yyyy"), ASLMDNO = d.ASLMDNO, CONTNRNO = d.CONTNRNO, CONTNRSDESC = d.CONTNRSDESC, CHANAME = d.CHANAME, BOENO = d.BOENO, IGMNO = d.IGMNO, GPLNO = d.GPLNO, DISPSTATUS = d.DISPSTATUS, ASLMID = d.ASLMID.ToString(), ASLDID = d.ASLDID.ToString() }).ToArray();
                return Json(new
                {
                    sEcho = param.sEcho,
                    aaData = aaData,
                    iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                    iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Edit
        [Authorize(Roles = "ImportDeStuffSlipEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/ImportDeStuff/Form/" + id);
        }
        #endregion

        #region Form
        [Authorize(Roles = "ImportDeStuffSlipCreate")]
        public ActionResult Form(int id = 0)
        {

            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            AuthorizationSlipMD vm = new AuthorizationSlipMD();
            AuthorizationSlipMaster tab = new AuthorizationSlipMaster();

            ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            ViewBag.ASLOTYPE = new SelectList(context.ImportDestuffSlipOperation, "OPRTYPE", "OPRTYPEDESC");

            //-----------------------------stuff -TYPE-------------------------
            List<SelectListItem> selectedDOTYPE = new List<SelectListItem>();
            SelectListItem selectedItem1 = new SelectListItem { Text = "MANUAL", Value = "0", Selected = true };
            selectedDOTYPE.Add(selectedItem1);
            selectedItem1 = new SelectListItem { Text = "MECHANICAL", Value = "1", Selected = false };
            selectedDOTYPE.Add(selectedItem1);
            ViewBag.ASLDTYPE = selectedDOTYPE;

           

            //------------------------------Labour Type-------------------------
            List<SelectListItem> selectedScut = new List<SelectListItem>();
            SelectListItem selectedItem = new SelectListItem { Text = "SANCO", Value = "0", Selected = false };
            selectedScut.Add(selectedItem);
            selectedItem = new SelectListItem { Text = "CHA", Value = "1", Selected = true };
            selectedScut.Add(selectedItem);
            ViewBag.ASLLTYPE = selectedScut;

            //-------------------------------DISPSTATUS----
            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "INBOOKS", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDSP);
            //  selectedItemDSP = new SelectListItem { Text = "CANCELLED", Value = "1", Selected = false };
            //  selectedDISPSTATUS.Add(selectedItemDSP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;

            if (id != 0)
            {
                tab = context.authorizatioslipmaster.Find(id);//find selected record

                vm.masterdata = context.authorizatioslipmaster.Where(det => det.ASLMID == id).ToList();
                vm.detaildata = context.authorizationslipdetail.Where(det => det.ASLMID == id).ToList();
                //ViewBag.ModifyField = DetailEdit(id);
                GateInDetail gitab = new GateInDetail();
                gitab = context.gateindetails.Find(Convert.ToInt32(vm.detaildata[0].GIDID));
                var oocdtry = context.Database.SqlQuery<VW_IMPORT_IGMNO_CONTAINER_CBX_ASSGN>("select * from VW_IMPORT_IGMNO_CONTAINER_CBX_ASSGN_01(nolock) where IGMNO='" + gitab.IGMNO.ToString() + "' and GPLNO='" + gitab.GPLNO.ToString() + "'").ToList();
                if (oocdtry != null)
                {
                    if (oocdtry[0].OOCDATE != null && oocdtry[0].OOCDATE != "")
                        ViewBag.OOCDATE = Convert.ToDateTime(oocdtry[0].OOCDATE).ToString("dd/MM/yyyy");
                }
                int ASLDTYPE = 0;
                if (vm.detaildata[0].GIDID > 0)
                {
                    var sqry = context.Database.SqlQuery<GateInDetail>("select *from GateInDetail where GIDID =" + vm.detaildata[0].GIDID).ToList();

                    ViewBag.GPLNO = sqry[0].GPLNO;
                    ViewBag.IGMNO = sqry[0].IGMNO;
                   
                }


                if (vm.detaildata != null || vm.detaildata.Count > 0)
                {
                    ASLDTYPE = vm.detaildata[0].ASLOTYPE;
                }

                List<SelectListItem> selectedDISPSTATUS1 = new List<SelectListItem>();
                if (Convert.ToInt32(tab.DISPSTATUS) == 1)
                {
                    SelectListItem selectedItem3 = new SelectListItem { Text = "CANCELLED", Value = "1", Selected = true };
                    selectedDISPSTATUS1.Add(selectedItem3);
                    selectedItem3 = new SelectListItem { Text = "INBOOKS", Value = "0", Selected = false };
                    selectedDISPSTATUS1.Add(selectedItem3);

                    ViewBag.DISPSTATUS = selectedDISPSTATUS1;
                }
                else
                {
                    SelectListItem selectedItem3 = new SelectListItem { Text = "CANCELLED", Value = "1", Selected = false };
                    selectedDISPSTATUS1.Add(selectedItem3);
                    selectedItem3 = new SelectListItem { Text = "INBOOKS", Value = "0", Selected = true };
                    selectedDISPSTATUS1.Add(selectedItem3);

                    ViewBag.DISPSTATUS = selectedDISPSTATUS1;
                }

                List<SelectListItem> selectedDOTYPE1 = new List<SelectListItem>();
                if (Convert.ToInt32(vm.detaildata[0].ASLDTYPE) == 1)
                {
                    SelectListItem selectedItem3 = new SelectListItem { Text = "MANUAL", Value = "0", Selected = false };
                    selectedDOTYPE1.Add(selectedItem3);
                    selectedItem3 = new SelectListItem { Text = "MECHANICAL", Value = "1", Selected = true };
                    selectedDOTYPE1.Add(selectedItem3);

                    ViewBag.ASLDTYPE = selectedDOTYPE1;
                }
                else
                {
                    SelectListItem selectedItem3 = new SelectListItem { Text = "MANUAL", Value = "0", Selected = true };
                    selectedDOTYPE1.Add(selectedItem3);
                    selectedItem3 = new SelectListItem { Text = "MECHANICAL", Value = "1", Selected = false };
                    selectedDOTYPE1.Add(selectedItem3);

                    ViewBag.ASLDTYPE = selectedDOTYPE1;
                }
                //---------Dropdown lists-------------------

                ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
                vm.destuffdata = context.Database.SqlQuery<PR_IMPORT_AUTHORIZATION_DETAIL_CTRL_ASSGN_Result>("EXEC PR_IMPORT_AUTHORIZATION_DETAIL_CTRL_ASSGN @PASLMID=" + id).ToList();
                //--------End of dropdown

            }

            return View(vm);
        }
        #endregion

        #region Details Table Display
        public void Detail(string id)
        {
            var param = id.Split(';');
            var PIGMNO = (param[0]);
            var PGPLNO = (param[1]);

            PIGMNO = PIGMNO.Replace('^', '/');
            PGPLNO = PGPLNO.Replace('^', '/');
            var query = context.Database.SqlQuery<VW_IMPORT_IGMNO_CONTAINER_CBX_ASSGN>("select * from VW_IMPORT_IGMNO_CONTAINER_CBX_ASSGN(nolock) where IGMNO='" + PIGMNO + "' and GPLNO='" + PGPLNO + "'").ToList();

            var tabl = "<div class='panel-heading navbar-inverse'  style=color:white>Container Details</div><Table id=mytabl class='table table-striped table-bordered bootstrap-datatable'> <thead><tr> <th></th><th>Container No</th><th> Size</th><th>In Date</th><th>OpenSheet No</th><th>BOE No</th><th>BOE Date</th><th>DODate</th><th>Liner Seal No</th><th>SANCO Seal No</th> </tr> </thead>";

            foreach (var rslt in query)
            {

                if (rslt.LSEALNO == null) rslt.LSEALNO = "-";
                if (rslt.SSEALNO == null) rslt.SSEALNO = "-";
                if (rslt.BOEDATE == null) rslt.BOEDATE = DateTime.Now.Date;
                if (rslt.GIDATE == null) rslt.GIDATE = DateTime.Now.Date;
                if (rslt.OOCDATE != null && rslt.OOCDATE != "") 
                    ViewBag.OOCDATE = Convert.ToDateTime(rslt.OOCDATE).ToString("dd/MM/yyyy");

                tabl = tabl + "<tbody>";
                tabl = tabl + "<tr><td><input type='checkbox' name='CHKBX' class='CHKBX' id='CHKBX' onchange='SelectedCont(this)' /><input type=text name='booltype' class='booltype hidden' /></td><td class=hide>";
                tabl = tabl + "<input type=text id=OSDID class=OSDID name=OSDID value=" + rslt.OSDID + "></td>";
                tabl = tabl + "<td class=hide><input type=text id=ASLDID value=0  class=ASLDID name=ASLDID hidden></td>";
                tabl = tabl + "<td class=hide><input type=text id=STMRNAME  class=STMRNAME name=STMRNAME></td>";
                tabl = tabl + "<td class=hide><input type=text id=IMPRTNAME  class=IMPRTNAME name=IMPRTNAME hidden></td>";
                tabl = tabl + "<td class=hide><input type=text id=VOYNO  class=VOYNO name=VOYNO hidden></td><td class=hide>";
                tabl = tabl + "<input type=text id=VSLNAME value='' class=VSLNAME name=VSLNAME hidden></td>";
                tabl = tabl + "<td class='col-md-3'><input type=text id=CONTNRNO value=" + rslt.CONTNRNO + " class='form-control CONTNRNO' name=CONTNRNO readonly></td>";
                tabl = tabl + "<td class='col-md-1'><input type=text value=" + rslt.CONTNRSDESC + " id=CONTNRSDESC class='CONTNRSDESC form-control' name=CONTNRSDESC readonly>";
                //tabl = tabl + "</td><td class='col-md-2'><input type=text id=GIDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class='GIDATE form-control' name=GIDATE readonly></td>";
                tabl = tabl + "</td><td class='col-md-2'><input type=text id=GIDID value='" + rslt.GIDID + "' class='GIDID form-control hidden' name=GIDID readonly><input type=text id=GIDATE value='" + rslt.GIDATE.ToString("dd/MM/yyyy") + "' class='GIDATE form-control' name=GIDATE readonly></td>";
                tabl = tabl + "<td class='col-lg-2'><input type=text id=OSMNO  class='form-control OSMNO' name=OSMNO value='" + rslt.OSMDNO + "' readonly></td><td class='col-lg-2'>";
                tabl = tabl + "<input type=text id=BOENO  class='form-control BOENO' name=BOENO value='" + rslt.BOENO + "'  readonly></td>";
                tabl = tabl + "<td class='col-md-2'><input type=text id=BOEDATE  class='form-control BOEDATE' name=BOEDATE value='" + rslt.BOEDATE.Value.ToString("dd/MM/yyyy") + "' readonly></td>";
                tabl = tabl + "<td class='col-md-2'><input type=text id=DODATE  class='form-control DODATE' name=DODATE  readonly style=width:89px value='" + rslt.DODATE.Value.ToString("dd/MM/yyyy") + "'></td>";
                tabl = tabl + "<td class='col-md-2'><input type=text id=OOCDT  class='form-control hide OOCDT' name=OOCDT  readonly style=width:89px value='" + rslt.OOCDATE + "'></td>";
                tabl = tabl + "<td class='col-md-2'><input type=text value='" + rslt.LSEALNO + "' id=LSEALNO class='form-control LSEALNO' name=LSEALNO readonly style=width:65px></td>";
                tabl = tabl + "<td class='col-md-2'><input type=text value='" + rslt.SSEALNO + "' id=SSEALNO  class='form-control SSEALNO' name=SSEALNO readonly style=width:65px></td>";
                tabl = tabl + "</tr></tbody>";


                //tabl = tabl + "<tbody>";
                //tabl = tabl + "<tr><td><input type='checkbox' name='CHKBX' class='CHKBX' id='CHKBX' onchange='SelectedCont(this)' />";
                //tabl = tabl + "<input type=text name='booltype' class='booltype hidden' /></td>";
                //tabl = tabl + "<td class=hide><input type=text id=OSDID class=OSDID name=OSDID value=" + rslt.OSDID + "></td>";
                //tabl = tabl + "<td class=hide><input type=text id=ASLDID value=0  class=ASLDID name=ASLDID hidden></td>";
                //tabl = tabl + "<td class=hide><input type=text id=STMRNAME  class=STMRNAME name=STMRNAME></td>";
                //tabl = tabl + "<td class=hide><input type=text id=IMPRTNAME  class=IMPRTNAME name=IMPRTNAME hidden></td>";
                //tabl = tabl + "<td class=hide><input type=text id=VOYNO  class=VOYNO name=VOYNO hidden></td>";
                //tabl = tabl + "<td class=hide><input type=text id=VSLNAME value='' class=VSLNAME name=VSLNAME hidden></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text id=CONTNRNO value=" + rslt.CONTNRNO + " class='form-control CONTNRNO' name=CONTNRNO readonly></td>";
                //tabl = tabl + "<td class='col-md-1'><input type=text value=" + rslt.CONTNRSDESC + " id=CONTNRSDESC class='CONTNRSDESC form-control' name=CONTNRSDESC readonly></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text id=GIDID value='" + rslt.GIDID + "' class='GIDID form-control hidden' name=GIDID readonly><input type=text id=GIDATE value='" + rslt.GIDATE.ToString("dd/MM/yyyy") + "' class='GIDATE form-control' name=GIDATE readonly></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text id=OSMNO  class='form-control OSMNO' name=OSMNO value='" + rslt.OSMDNO + "' readonly></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text id=BOENO  class='form-control BOENO' name=BOENO value='" + rslt.BOENO + "'  readonly></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text id=BOEDATE  class='form-control BOEDATE' name=BOEDATE value='" + rslt.BOEDATE.Value.ToString("dd/MM/yyyy") + "' readonly></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text id=DODATE  class='form-control DODATE' name=DODATE  readonly style=width:89px value='" + rslt.DODATE.Value.ToString("dd/MM/yyyy") + "'></td>";
                //tabl = tabl + "<td class='col-md-2'><input type=text value='" + rslt.LSEALNO + "' id=LSEALNO class='form-control LSEALNO' name=LSEALNO readonly style=width:65px></td>";
                //tabl = tabl + "tabl = tabl +<td class='col-md-2'><input type=text value='" + rslt.SSEALNO + "' id=SSEALNO  class='form-control SSEALNO' name=SSEALNO readonly style=width:65px></td>";
                //tabl = tabl + "</tr></tbody>";

            }
            tabl = tabl + "</Table>";
            Response.Write(tabl);

        }
        #endregion

        #region Saveformdata
        public void Savedata(FormCollection F_Form)
        {
            AuthorizationSlipMaster authorizatioslipmaster = new AuthorizationSlipMaster();
            AuthorizationSlipDetail authorizatioslipdetail = new AuthorizationSlipDetail();
            //-------Getting Primarykey field--------

            string todaydt = Convert.ToString(DateTime.Now);
            string todayd = Convert.ToString(DateTime.Now.Date);

            string mid = Convert.ToString(F_Form["masterdata[0].ASLMID"]);
            int ASLMID = 0;
            if (mid == null || mid == "" || mid == "0")
            {
                ASLMID = 0;
            }
            else { ASLMID = Convert.ToInt32(mid); }

            //Int32 ASLMID = Convert.ToInt32(F_Form["masterdata[0].ASLMID"]);
            int ASLDID = 0;
            string DELIDS = "";
            //-----End

            string userId = Session["CUSRID"]?.ToString() ?? "";
            AuthorizationSlipMaster before = null;
            
            if (ASLMID != 0)
            {
                authorizatioslipmaster = context.authorizatioslipmaster.Find(ASLMID);
                // Capture before state for edit logging AFTER loading the entity
                try
                {
                    before = context.authorizatioslipmaster.AsNoTracking().FirstOrDefault(x => x.ASLMID == ASLMID);
                    if (before != null)
                    {
                        EnsureBaselineVersionZero(before, userId);
                    }
                }
                catch { /* ignore if baseline creation fails */ }
            }

            authorizatioslipmaster.COMPYID = Convert.ToInt32(Session["compyid"]);
            authorizatioslipmaster.SDPTID = 1;
            authorizatioslipmaster.ASLMTYPE = 2;
            authorizatioslipmaster.CUSRID = Session["CUSRID"].ToString();
            //   authorizatioslipmaster.CUSRID = "admin";
            authorizatioslipmaster.LMUSRID = Session["CUSRID"].ToString(); 
            authorizatioslipmaster.DISPSTATUS = Convert.ToInt16(F_Form["DISPSTATUS"]);
            
            authorizatioslipmaster.PRCSDATE = DateTime.Now;

            string indate = Convert.ToString(F_Form["masterdata[0].ASLMDATE"]);

            if (indate != null || indate != "")
            {
                authorizatioslipmaster.ASLMDATE = Convert.ToDateTime(indate).Date;
            }
            else { authorizatioslipmaster.ASLMDATE = DateTime.Now.Date; }

            if (authorizatioslipmaster.ASLMDATE > Convert.ToDateTime(todayd))
            {
                authorizatioslipmaster.ASLMDATE = Convert.ToDateTime(todayd);
            }

            string intime = Convert.ToString(F_Form["masterdata[0].ASLMTIME"]);
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

                        authorizatioslipmaster.ASLMTIME = Convert.ToDateTime(in_datetime);
                    }
                    else { authorizatioslipmaster.ASLMTIME = DateTime.Now; }
                }
                else { authorizatioslipmaster.ASLMTIME = DateTime.Now; }
            }
            else { authorizatioslipmaster.ASLMTIME = DateTime.Now; }

            if (authorizatioslipmaster.ASLMTIME > Convert.ToDateTime(todaydt))
            {
                authorizatioslipmaster.ASLMTIME = Convert.ToDateTime(todaydt);
            }

            //authorizatioslipmaster.ASLMDATE = Convert.ToDateTime(F_Form["masterdata[0].ASLMTIME"]).Date;
            //authorizatioslipmaster.ASLMTIME = Convert.ToDateTime(F_Form["masterdata[0].ASLMTIME"]);
            //authorizatioslipmaster.ASLMLTYPE = 0;
            if (ASLMID == 0)
            {
                //authorizatioslipmaster.ASLMNO = Convert.ToInt16(Autonumber.autonum("AUTHORIZATIONSLIPMASTER", "ASLMNO", "COMPYID=" + Convert.ToInt32(Session["compyid"]) + " AND SDPTID=1 AND ASLMTYPE=2 AND ASLMLTYPE=0").ToString());
                authorizatioslipmaster.ASLMNO = Convert.ToInt32(Autonumber.autonum("AUTHORIZATIONSLIPMASTER", "ASLMNO", "COMPYID=" + Convert.ToInt32(Session["compyid"]) + " AND SDPTID=1 AND ASLMTYPE=2").ToString());

                int ano = authorizatioslipmaster.ASLMNO;
                string prfx = string.Format("{0:D5}", ano);
                authorizatioslipmaster.ASLMDNO = prfx.ToString();
                context.authorizatioslipmaster.Add(authorizatioslipmaster);
                context.SaveChanges();
                
                // Create baseline for new record
                try
                {
                    var newRecord = context.authorizatioslipmaster.AsNoTracking().FirstOrDefault(x => x.ASLMID == authorizatioslipmaster.ASLMID);
                    if (newRecord != null)
                    {
                        EnsureBaselineVersionZero(newRecord, userId);
                    }
                }
                catch { /* ignore baseline creation errors */ }
            }
            else
            {
                context.Entry(authorizatioslipmaster).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
                
                // Log changes after successful save - use authorizatioslipmaster directly since it has the updated values after SaveChanges
                if (before != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"ImportDeStuff SAVE: ASLMID={authorizatioslipmaster.ASLMID}, ASLMDNO={authorizatioslipmaster.ASLMDNO}, calling LogDeStuffEdits");
                        int nextVersion = CalculateNextVersion(authorizatioslipmaster.ASLMDNO);
                        LogDeStuffEdits(before, authorizatioslipmaster, userId, nextVersion);
                        System.Diagnostics.Debug.WriteLine($"LogDeStuffEdits completed for ASLMID={authorizatioslipmaster.ASLMID}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ImportDeStuff edit logging failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ImportDeStuff SAVE: before is null for ASLMID={ASLMID}");
                }
            }

            //-------------Shipping Bill Details
            string[] F_ASLDID = F_Form.GetValues("ASLDID");
            string[] F_booltype = F_Form.GetValues("booltype");
            string[] OSDID = F_Form.GetValues("OSDID");
            string[] GIDID = F_Form.GetValues("GIDID");
            string LCATEID = F_Form["detaildata[0].LCATEID"];
            string ASLDTYPE = F_Form["detaildata[0].ASLDTYPE"];
            //string ASLDTYPE = F_Form["ASLDTYPE"]; 
            string ASLLTYPE = F_Form["detaildata[0].ASLLTYPE"];
            string ASLOTYPE = F_Form["detaildata[0].ASLOTYPE"];
            string[] CSEALNO = F_Form.GetValues("CSEALNO");
            string[] ASEALNO = F_Form.GetValues("ASEALNO");
            string booltype = "";

            for (int count = 0; count < F_ASLDID.Count(); count++)
            {
                ASLDID = Convert.ToInt32(F_ASLDID[count]);
                booltype = F_booltype[count].ToString();
                if (ASLDID != 0)
                {
                    authorizatioslipdetail = context.authorizationslipdetail.Find(ASLDID);
                }
                if (booltype == "true")
                {
                    authorizatioslipdetail.ASLMID = authorizatioslipmaster.ASLMID;
                    authorizatioslipdetail.OSDID = Convert.ToInt32(OSDID[count]);
                    authorizatioslipdetail.GIDID = Convert.ToInt32(GIDID[count]);
                    authorizatioslipdetail.LCATEID = Convert.ToInt32(LCATEID);
                    authorizatioslipdetail.ASLDTYPE = Convert.ToInt16(ASLDTYPE);
                    authorizatioslipdetail.ASLLTYPE = Convert.ToInt16(ASLLTYPE);
                    authorizatioslipdetail.ASLOTYPE = Convert.ToInt16(ASLOTYPE);
                    authorizatioslipdetail.STFDID = 0;
                    authorizatioslipdetail.VHLNO = null;
                    authorizatioslipdetail.DRVNAME = null;
                    //authorizatioslipdetail.CSEALNO = CSEALNO[count].ToString();
                    //authorizatioslipdetail.ASEALNO = ASEALNO[count].ToString();
                    authorizatioslipdetail.ASLDODATE = null;
                    authorizatioslipdetail.DISPSTATUS = 0;
                    authorizatioslipdetail.PRCSDATE = DateTime.Now;

                    if (ASLDID == 0)
                    {
                        context.authorizationslipdetail.Add(authorizatioslipdetail);
                        context.SaveChanges();
                        ASLDID = authorizatioslipdetail.ASLDID;
                    }
                    else
                    {
                        context.Entry(authorizatioslipdetail).State = System.Data.Entity.EntityState.Modified;
                        context.SaveChanges();
                    }

                    DELIDS = DELIDS + "," + ASLDID.ToString();
                }
            }
            // context.Database.ExecuteSqlCommand("DELETE FROM authorizationslipdetail  WHERE ASLMID=" + ASLMID + " and  ASLDID NOT IN(" + DELIDS.Substring(1) + ")");
            Response.Redirect("Index");
        }
        #endregion

        #region AutoIGM
        public JsonResult AutoIGM(string term)
        {
            var result = (from view in context.view_opensheet_cbx_assign_01
                          where view.IGMNO.ToLower().Contains(term.ToLower())
                          select new { view.IGMNO }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region GetOperation
        public JsonResult GetOperation()
        {
            var result = context.Database.SqlQuery<ImportDestuffSlipOperation>("select * from IMPORT_DESTUFF_SLIP_OPERATION_TYPE").ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region PrintView
        [Authorize(Roles = "ImportDeStuffSlipPrint")]
        public void PrintView(int? id = 0)
        {
            String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
            SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);
            //  ........delete TMPRPT...//
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "Destuff_Slip", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;

                cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "Import_Destuff_Slip.RPT");
                cryRpt.RecordSelectionFormula = "{VW_ADSLIP_CRY_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_ADSLIP_CRY_PRINT_ASSGN.ASLMID} = " + id;

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
                GC.Collect();
                stringbuilder.Clear();
            }

        }
        #endregion

        #region Cancel
        [Authorize(Roles = "ImportDeStuffSlipCancel")]
        public void Cancel()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");

            String temp = Delete_fun.delete_check1(fld, id);

            if (temp.Equals("PROCEED"))
            {
                AuthorizationSlipMaster authorizatioslipmaster = context.authorizatioslipmaster.Find(Convert.ToInt32(id));
                context.Entry(authorizatioslipmaster).Entity.DISPSTATUS = 1;
                context.SaveChanges();
                Response.Write("Cancelled successfully...");
            }
            else
                Response.Write(temp);
        }
        #endregion

        #region OSMaxDate

        public ActionResult OSMaxDate(string id)
        {
            string igmno = "", gplno = "";

            var param = id.Split(';');
            igmno = (param[0]);
            gplno = (param[1]);

            var result = (from a in context.gateindetails
                          join b in context.opensheetdetails on a.GIDID equals b.GIDID
                          join c in context.opensheetmasters on b.OSMID equals c.OSMID
                          where c.OSMIGMNO == igmno && c.OSMLNO == gplno
                          group c by c.OSMIGMNO into g
                          select new { OSMDATE = g.Max(t => t.OSMDATE) }).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult OSMaxDate(string igmno, string gplno)
        //{

        //    var result = (from a in context.gateindetails
        //                  join b in context.opensheetdetails on a.GIDID equals b.GIDID
        //                  join c in context.opensheetmasters on b.OSMID equals c.OSMID
        //                  where c.OSMIGMNO == igmno && c.OSMLNO == gplno
        //                  group c by c.OSMIGMNO into g
        //                  select new { OSMDATE = g.Max(t => t.OSMDATE) }).ToList();

        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
        #endregion

        #region Delete 
        [Authorize(Roles = "ImportDeStuffSlipDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");

            //   var param = id.Split('-');

            String temp = Delete_fun.delete_check1(fld, id);

            if (temp.Equals("PROCEED"))
            {
                AuthorizationSlipMaster authorizatioslipmaster = context.authorizatioslipmaster.Find(Convert.ToInt32(id));
                context.authorizatioslipmaster.Remove(authorizatioslipmaster);
                context.SaveChanges();
                Response.Write("Deleted successfully...");
            }
            else
                Response.Write(temp);
        }

        [Authorize(Roles = "ImportDeStuffSlipDelete")]
        public void Del_det()
        {
            using (SCFSERPContext context = new SCFSERPContext())
            {
                using (var trans = context.Database.BeginTransaction())
                {
                    try
                    {
                        String id = Request.Form.Get("id");
                        String fld = Request.Form.Get("fld");

                        //   var param = id.Split('-');

                        String temp = Delete_fun.delete_check1(fld, id);

                        if (temp.Equals("PROCEED"))
                        {
                            AuthorizationSlipDetail authorizationSlipDetail = context.authorizationslipdetail.Find(Convert.ToInt32(id));
                            context.authorizationslipdetail.Remove(authorizationSlipDetail);
                            context.SaveChanges();
                            Response.Write("Deleted successfully...");
                        }
                        else
                            Response.Write(temp);
                        
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback(); //Response.Redirect("/Error/SavepointErr");
                        Response.Write("Sorry !!!An Error Occurred");
                    }
                }
            }
        }
        #endregion

        #region Edit Log Pages
        public ActionResult EditLogDeStuff(int? aslmid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var list = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                // Get actual ASLMDNO string from database to handle leading zeros
                string aslmdnoString = null;
                if (aslmid.HasValue)
                {
                    try
                    {
                        var deStuffRecord = context.authorizatioslipmaster.AsNoTracking().FirstOrDefault(x => x.ASLMID == aslmid.Value);
                        if (deStuffRecord != null && !string.IsNullOrEmpty(deStuffRecord.ASLMDNO))
                        {
                            aslmdnoString = deStuffRecord.ASLMDNO;
                        }
                        else
                        {
                            aslmdnoString = aslmid.Value.ToString();
                        }
                    }
                    catch { aslmdnoString = aslmid.Value.ToString(); }
                }

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"SELECT TOP 2000 [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [Modules] = 'ImportDeStuff'
                                                  AND (@ASLMDNO_STR IS NULL OR CAST([GIDNO] AS NVARCHAR(50)) = @ASLMDNO_STR OR CAST([GIDNO] AS NVARCHAR(50)) = CAST(@ASLMID AS NVARCHAR(50)))
                                                  AND (@FROM IS NULL OR [ChangedOn] >= @FROM)
                                                  AND (@TO   IS NULL OR [ChangedOn] <  DATEADD(day, 1, @TO))
                                                  AND (@USER IS NULL OR [ChangedBy] LIKE @USERPAT)
                                                  AND (@FIELD IS NULL OR [FieldName] LIKE @FIELDPAT)
                                                  AND (@VERSION IS NULL OR [Version] LIKE @VERPAT)
                                                  AND NOT (RTRIM(LTRIM([Version])) IN ('0','V0') OR LEFT(RTRIM(LTRIM([Version])),3) IN ('v0-','V0-'))
                                                ORDER BY [ChangedOn] DESC, [GIDNO] DESC", sql))
                {
                    cmd.Parameters.Add("@ASLMID", System.Data.SqlDbType.Int);
                    cmd.Parameters.Add("@ASLMDNO_STR", System.Data.SqlDbType.NVarChar, 50);
                    cmd.Parameters["@ASLMID"].Value = aslmid.HasValue ? (object)aslmid.Value : DBNull.Value;
                    cmd.Parameters["@ASLMDNO_STR"].Value = !string.IsNullOrEmpty(aslmdnoString) ? (object)aslmdnoString : DBNull.Value;
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

            // Map technical field names to friendly form labels and raw codes to display values
            try
            {
                var dictCategory = context.categorymasters.Where(c => c.CATETID == 6 && c.DISPSTATUS == 0)
                    .GroupBy(x => x.CATEID)
                    .ToDictionary(g => g.Key, g => g.First().CATENAME);
                var dictOperation = context.ImportDestuffSlipOperation
                    .GroupBy(x => x.OPRTYPE)
                    .ToDictionary(g => g.Key, g => g.First().OPRTYPEDESC);

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
                        case "LCATEID":
                            return int.TryParse(raw, out ival) && dictCategory.ContainsKey(ival) ? dictCategory[ival] : raw;
                        case "ASLDTYPE":
                            if (raw == "0") return "MANUAL";
                            if (raw == "1") return "MECHANICAL";
                            return raw;
                        case "ASLLTYPE":
                            if (raw == "0") return "SANCO";
                            if (raw == "1") return "CHA";
                            return raw;
                        case "ASLOTYPE":
                            return int.TryParse(raw, out ival) && dictOperation.ContainsKey(ival) ? dictOperation[ival] : raw;
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
                        case "ASLMDATE": return "Date";
                        case "ASLMTIME": return "Time";
                        case "ASLMNO": return "Slip No";
                        case "ASLMDNO": return "Slip Detail No";
                        case "ASLMTYPE": return "Type";
                        case "DISPSTATUS": return "Status";
                        case "LCATEID": return "Labour";
                        case "ASLDTYPE": return "Destuff Type";
                        case "ASLLTYPE": return "Labour Type";
                        case "OSDID": return "Open Sheet Detail ID";
                        case "GIDID": return "Gate In Detail ID";
                        case "VHLNO": return "Vehicle No";
                        case "DRVNAME": return "Driver Name";
                        case "ASLFDESC": return "Description";
                        case "ASLTDESC": return "Type Description";
                        case "ASLDODATE": return "DO Date";
                        case "CSEALNO": return "CHA Seal No";
                        case "ASEALNO": return "Agency Seal No";
                        case "SLTID": return "Slot ID";
                        default: return field.StartsWith("Detail.", StringComparison.OrdinalIgnoreCase) ? "Detail." + f : field;
                    }
                }

                list = list.Where(row => {
                    if (row.FieldName == null) return true;
                    var fieldNameLocal = row.FieldName.Trim();
                    if (fieldNameLocal.Equals("ASLOTYPE", StringComparison.OrdinalIgnoreCase) || 
                        fieldNameLocal.Equals("Detail.ASLOTYPE", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("Operation Type", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }).ToList();

                foreach (var row in list)
                {
                    row.OldValue = Map(row.FieldName, row.OldValue);
                    row.NewValue = Map(row.FieldName, row.NewValue);
                    row.FieldName = Friendly(row.FieldName);
                }
            }
            catch { /* Best-effort mapping; do not fail page if lookups have issues */ }

            ViewBag.Module = "ImportDeStuff";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        public ActionResult EditLogDeStuffCompare(int? aslmid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            if (aslmid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide ASLMID, Version A and Version B to compare.";
                return RedirectToAction("EditLogDeStuff", new { aslmid = aslmid });
            }

            string aslmdnoString = aslmid.Value.ToString();
            var deStuffRecord = context.authorizatioslipmaster.AsNoTracking().FirstOrDefault(x => x.ASLMID == aslmid.Value);
            if (deStuffRecord != null && !string.IsNullOrEmpty(deStuffRecord.ASLMDNO))
            {
                aslmdnoString = deStuffRecord.ASLMDNO;
            }

            versionA = (versionA ?? string.Empty).Trim();
            versionB = (versionB ?? string.Empty).Trim();
            
            if (aslmid.HasValue)
            {
                var baseLabel = "v0-" + aslmdnoString;
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
                using (var cmd = new SqlCommand(@"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [Modules] = 'ImportDeStuff'
                                                  AND (CAST([GIDNO] AS NVARCHAR(50)) = @ASLMDNO_STR OR CAST([GIDNO] AS NVARCHAR(50)) = CAST(@ASLMID AS NVARCHAR(50)))
                                                  AND RTRIM(LTRIM([Version])) = @V", sql))
                {
                    cmd.Parameters.Add("@ASLMID", System.Data.SqlDbType.Int);
                    cmd.Parameters.Add("@ASLMDNO_STR", System.Data.SqlDbType.NVarChar, 50);
                    cmd.Parameters.Add("@V", System.Data.SqlDbType.NVarChar, 100);

                    sql.Open();
                    cmd.Parameters["@ASLMID"].Value = aslmid.Value;
                    cmd.Parameters["@ASLMDNO_STR"].Value = aslmdnoString;
                    cmd.Parameters["@V"].Value = versionA.Trim();
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
                                Version = versionA,
                                Modules = r["Modules"] == DBNull.Value ? null : Convert.ToString(r["Modules"])
                            });
                        }
                    }

                    cmd.Parameters["@V"].Value = versionB.Trim();
                    using (var r2 = cmd.ExecuteReader())
                    {
                        while (r2.Read())
                        {
                            rowsB.Add(new scfs_erp.Models.GateInDetailEditLogRow
                            {
                                GIDNO = Convert.ToString(r2["GIDNO"]),
                                FieldName = Convert.ToString(r2["FieldName"]),
                                OldValue = r2["OldValue"] == DBNull.Value ? null : Convert.ToString(r2["OldValue"]),
                                NewValue = r2["NewValue"] == DBNull.Value ? null : Convert.ToString(r2["NewValue"]),
                                ChangedBy = Convert.ToString(r2["ChangedBy"]),
                                ChangedOn = r2["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r2["ChangedOn"]) : DateTime.MinValue,
                                Version = versionB,
                                Modules = r2["Modules"] == DBNull.Value ? null : Convert.ToString(r2["Modules"])
                            });
                        }
                    }
                }
            }

            ViewBag.Module = "ImportDeStuff";
            ViewBag.ASLMID = aslmid.Value;
            ViewBag.VersionA = versionA;
            ViewBag.VersionB = versionB;
            ViewBag.RowsA = rowsA;
            ViewBag.RowsB = rowsB;

            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        // ========================= Edit Logging Helper Methods =========================
        private int CalculateNextVersion(string aslmdno)
        {
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return 1;
            if (string.IsNullOrWhiteSpace(aslmdno)) return 1;

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
                    WHERE [GIDNO] = @ASLMDNO AND [Modules] = 'ImportDeStuff'", sql))
                {
                    cmd.Parameters.AddWithValue("@ASLMDNO", aslmdno);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        return Convert.ToInt32(obj);
                }
            }
            catch { }
            return 1;
        }

        private void LogDeStuffEdits(AuthorizationSlipMaster before, AuthorizationSlipMaster after, string userId, int nextVersion)
        {
            if (before == null || after == null)
            {
                System.Diagnostics.Debug.WriteLine($"LogDeStuffEdits: before={before != null}, after={after != null}");
                return;
            }
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine("LogDeStuffEdits: No SCFSERP_EditLog connection string found");
                return;
            }
            if (string.IsNullOrWhiteSpace(after.ASLMDNO))
            {
                System.Diagnostics.Debug.WriteLine($"LogDeStuffEdits: ASLMDNO is null or empty for ASLMID={after.ASLMID}");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"LogDeStuffEdits: Starting for ASLMID={after.ASLMID}, ASLMDNO={after.ASLMDNO}, Version={nextVersion}");

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ASLMID", "PRCSDATE", "LMUSRID", "CUSRID", "COMPYID", "SDPTID", "ASLMNO"
            };

            var props = typeof(AuthorizationSlipMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                var ov = p.GetValue(before, null);
                var nv = p.GetValue(after, null);

                if (BothNull(ov, nv)) continue;

                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                bool changed = false;

                if (type == typeof(decimal) || type == typeof(decimal?))
                {
                    var d1 = ToNullableDecimal(ov) ?? 0m;
                    var d2 = ToNullableDecimal(nv) ?? 0m;
                    if (d1 == 0m && d2 == 0m) continue;
                    changed = d1 != d2;
                }
                else if (type == typeof(int) || type == typeof(int?) || type == typeof(long) || type == typeof(long?) || type == typeof(short) || type == typeof(short?))
                {
                    var i1 = ov == null ? (long?)null : Convert.ToInt64(ov);
                    var i2 = nv == null ? (long?)null : Convert.ToInt64(nv);
                    if (!i1.HasValue && !i2.HasValue) continue;
                    var val1 = i1 ?? 0;
                    var val2 = i2 ?? 0;
                    changed = val1 != val2;
                    if (val1 == 0 && val2 == 0) continue;
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
                        changed = t1 != t2;
                    }
                }
                else if (type == typeof(string))
                {
                    var s1 = (Convert.ToString(ov) ?? string.Empty).Trim();
                    var s2 = (Convert.ToString(nv) ?? string.Empty).Trim();
                    bool def1 = string.IsNullOrEmpty(s1) || s1 == "-" || s1 == "0";
                    bool def2 = string.IsNullOrEmpty(s2) || s2 == "-" || s2 == "0";
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

                var versionLabel = $"V{nextVersion}-{after.ASLMDNO}";
                System.Diagnostics.Debug.WriteLine($"LogDeStuffEdits: Logging change - Field={p.Name}, Old={os}, New={ns}, Version={versionLabel}");
                InsertEditLogRow(cs.ConnectionString, after.ASLMDNO, p.Name, os, ns, userId, versionLabel, "ImportDeStuff");
            }
            
            System.Diagnostics.Debug.WriteLine($"LogDeStuffEdits: Completed for ASLMDNO={after.ASLMDNO}, Version={nextVersion}");
        }

        private void EnsureBaselineVersionZero(AuthorizationSlipMaster snapshot, string userId)
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
                if (string.IsNullOrWhiteSpace(snapshot.ASLMDNO)) return;

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[GateInDetailEditLog] WHERE [GIDNO]=@ASLMDNO AND [Modules]='ImportDeStuff' AND (RTRIM(LTRIM([Version]))=@VLower OR RTRIM(LTRIM([Version]))=@VUpper OR RTRIM(LTRIM([Version]))='0' OR RTRIM(LTRIM([Version]))='V0')", sql))
                {
                    cmd.Parameters.AddWithValue("@ASLMDNO", snapshot.ASLMDNO);
                    var baselineVerLower = "v0-" + snapshot.ASLMDNO;
                    var baselineVerUpper = "V0-" + snapshot.ASLMDNO;
                    cmd.Parameters.AddWithValue("@VLower", baselineVerLower);
                    cmd.Parameters.AddWithValue("@VUpper", baselineVerUpper);
                    sql.Open();
                    var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    if (exists) return;
                }

                InsertBaselineSnapshot(snapshot, userId);
            }
            catch { }
        }

        private void InsertBaselineSnapshot(AuthorizationSlipMaster snapshot, string userId)
        {
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
            if (string.IsNullOrWhiteSpace(snapshot.ASLMDNO)) return;
            var baselineVer = "v0-" + snapshot.ASLMDNO;

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ASLMID", "PRCSDATE", "LMUSRID", "CUSRID", "COMPYID", "SDPTID", "ASLMNO"
            };

            var props = typeof(AuthorizationSlipMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                var valObj = p.GetValue(snapshot, null);
                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                if (type == typeof(string))
                {
                    var s = (Convert.ToString(valObj) ?? string.Empty).Trim();
                    bool isDefault = string.IsNullOrEmpty(s) || s == "-" || s == "0";
                    if (isDefault) continue;
                }
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                {
                    var i = Convert.ToInt64(valObj ?? 0);
                    if (i == 0) continue;
                }
                else if (type == typeof(decimal))
                {
                    var d = ToNullableDecimal(valObj) ?? 0m;
                    if (d == 0m) continue;
                }

                var newVal = FormatValForLogging(p.Name, valObj);
                InsertEditLogRow(cs.ConnectionString, snapshot.ASLMDNO, p.Name, null, newVal, userId, baselineVer, "ImportDeStuff");
            }
        }

        private string FormatValForLogging(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            if (string.IsNullOrEmpty(formattedValue)) return formattedValue;

            try
            {
                int lookupId;
                if (fieldName.Equals("LCATEID", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(formattedValue, out lookupId) && lookupId > 0)
                    {
                        var category = context.categorymasters.FirstOrDefault(c => c.CATEID == lookupId && c.CATETID == 6 && c.DISPSTATUS == 0);
                        if (category != null && !string.IsNullOrEmpty(category.CATENAME))
                            return category.CATENAME;
                    }
                }
                else if (fieldName.Equals("ASLDTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "0") return "MANUAL";
                    if (formattedValue == "1") return "MECHANICAL";
                }
                else if (fieldName.Equals("ASLLTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "0") return "SANCO";
                    if (formattedValue == "1") return "CHA";
                }
                else if (fieldName.Equals("DISPSTATUS", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "1") return "CANCELLED";
                    if (formattedValue == "0") return "INBOOKS";
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

        private static void InsertEditLogRow(string connectionString, string aslmdno, string fieldName, string oldValue, string newValue, string changedBy, string versionLabel, string modules)
        {
            try
            {
                using (var sql = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(@"
                    INSERT INTO [dbo].[GateInDetailEditLog] ([GIDNO], [FieldName], [OldValue], [NewValue], [ChangedBy], [ChangedOn], [Version], [Modules])
                    VALUES (@GIDNO, @FieldName, @OldValue, @NewValue, @ChangedBy, GETDATE(), @Version, @Modules)", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", aslmdno);
                    cmd.Parameters.AddWithValue("@FieldName", fieldName);
                    cmd.Parameters.AddWithValue("@OldValue", (object)oldValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", (object)newValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Version", (object)versionLabel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Modules", modules ?? string.Empty);
                    sql.Open();
                    cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"InsertEditLogRow: Successfully inserted log for GIDNO={aslmdno}, Field={fieldName}, Version={versionLabel}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InsertEditLogRow: Failed to insert log - GIDNO={aslmdno}, Field={fieldName}, Error={ex.Message}");
            }
        }
        #endregion
    }
}