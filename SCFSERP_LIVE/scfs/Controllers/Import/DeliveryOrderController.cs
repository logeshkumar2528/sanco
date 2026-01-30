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

namespace scfs_erp.Controllers.Import
{
    [SessionExpire]
    public class DeliveryOrderController : Controller
    {
        //
        // GET: /DeliveryOrder/
        SCFSERPContext context = new SCFSERPContext();
        public static String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
        SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);
        [Authorize(Roles = "ImportDeliveryOrderIndex")]
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
            return View();
        }//........end of index grid
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {

            using (var e = new CFSImportEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                var data = e.pr_Search_DeliveryOrder(param.sSearch,
                                                Convert.ToInt32(Request["iSortCol_0"]),
                                                Request["sSortDir_0"],
                                                param.iDisplayStart,
                                                param.iDisplayStart + param.iDisplayLength,
                                                totalRowsCount,
                                                filteredRowsCount, Convert.ToInt32(Session["compyid"]), Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]));

                var aaData = data.Select(d => new string[] { d.DODATE.Value.ToString("dd/MM/yyyy"), d.DONO, d.DODREFNO, d.DODREFNAME, d.CONTNRSDESC, d.DOREFNAME, d.IGMNO, d.GPLNO, d.DISPSTATUS.ToString(), d.DOMID.ToString() }).ToArray();

                return Json(new
                {
                    sEcho = param.sEcho,
                    aaData = aaData,
                    iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                    iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                }, JsonRequestBehavior.AllowGet);
            }
        }
        //[Authorize(Roles = "ImportDeliveryOrderEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/DeliveryOrder/Form/" + id);
        }
        [Authorize(Roles = "ImportDeliveryOrderCreate")]
        public ActionResult Form(int? id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            DeliveryOrderMaster tab = new DeliveryOrderMaster();
            DeliveryOrderMD vm = new DeliveryOrderMD();
            ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC");
            ViewBag.DOMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.TARIFFMID = new SelectList("");
            ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");

            if (id != 0)
            {
                vm.masterdata = context.DeliveryOrderMasters.Find(id);
                vm.detaildata = context.DeliveryOrderDetails.Where(x => x.DOMID == id).ToList();
                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", vm.masterdata.BANKMID);
                ViewBag.DOMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", vm.masterdata.DOMODE);
                ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC", vm.masterdata.TARIFFMID);
                var tariffmid = Convert.ToInt32(vm.masterdata.TARIFFMID);
                var sql = context.Database.SqlQuery<int>("select TGID from TariffMaster where TARIFFMID=" + tariffmid).ToList();
                ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC", Convert.ToInt32(sql[0]));
                vm.detailedit = context.Database.SqlQuery<VW_DELIVERY_ORDER_DETAIL_GRID_ASSGN>("select * from VW_DELIVERY_ORDER_DETAIL_GRID_ASSGN where DOMID=" + Convert.ToInt32(id) + "").ToList();

                ViewBag.IGMNO = vm.detailedit[0].IGMNO;
                ViewBag.GPLNO = vm.detailedit[0].GPLNO;
                ViewBag.BILLEMID = vm.detailedit[0].BILLEMID;
                ViewBag.DPAIDAMT = vm.detailedit[0].DPAIDAMT;
                ViewBag.DPAIDNO = vm.detailedit[0].DPAIDNO;
            }

            return View(vm);
        }


        public JsonResult GetTariff(int id)
        {
            var data = context.Database.SqlQuery<TariffMaster>("Select * from TariffMaster where DISPSTATUS=0 and TGID=" + id);
            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCHADetail(string id)
        {
            var param = id.Split(';');

            var qry = "PR_DORDER_IGMNO_BOE_NO_CTRL_ASSGN_E001 @PIGMNO='" + param[0] + "',@PGPLNO='" + param[1] + "'";
            var data = context.Database.SqlQuery<PR_DORDER_IGMNO_BOE_NO_CTRL_ASSGN_E001_Result>(qry);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public void Detail(string id)
        {
            var param = id.Split(';');
            using (SCFSERPContext contxt = new SCFSERPContext())
            {

                var query = context.Database.SqlQuery<PR_DORDER_IGMNO_BILLENTRY_WISE_CONTAINER_FLX_ASSGN_Result>("PR_DORDER_IGMNO_BILLENTRY_WISE_CONTAINER_FLX_ASSGN @PIGMNO='" + param[0] + "',@PGPLNO='" + param[1] + "'").ToList();


                var tabl = " <div class='panel-heading navbar-inverse'  style=color:white>DO Details</div><Table id=mytabl class='table table-striped table-bordered bootstrap-datatable'> <thead><tr> <th>BOE NO</th><th>Container No</th><th> Size</th><th>InDate</th><th>Vessel</th> <th> Voy No </th><th>Product</th><th>Weight</th></tr> </thead>";

                foreach (var rslt in query)
                {

                    tabl = tabl + "<tbody><tr><td class=hide><input type=text id='DODID' class='DODID' name='DODID' value=0></td><td class=hide><input type=text id=F_GIDID value=" + rslt.GIDID + "  class=F_GIDID name=F_GIDID hidden></td><td class=hide><input type=text id=STMRNAME value='" + rslt.STMRNAME + "' class=STMRNAME name=STMRNAME></td><td class=hide><input type=text id=IMPRTNAME value='" + rslt.IMPRTNAME + "' class=IMPRTNAME name=IMPRTNAME hidden></td><td><input type=text value='" + rslt.BILLEMDNO + "' id='BILLEMDNO' class='form-control BILLEMDNO' name='BILLEMDNO'  readonly><input type=text value='" + rslt.BILLEDID + "' id='BILLEDID' class='hide form-control BILLEDID' name='BILLEDID'  readonly></td><td><input type=text id=CONTNRNO value=" + rslt.CONTNRNO + " class=CONTNRNO name=CONTNRNO readonly></td><td><input type=text value=" + rslt.CONTNRSDESC + " id=CONTNRSDESC class=CONTNRSDESC name=CONTNRSDESC style=width:45px readonly></td><td><input type=text id=GIDATE value=" + rslt.GIDATE + " class=GIDATE name=GIDATE readonly></td><td><input type=text value='" + rslt.VSLNAME + "' id='VSLNAME' class='VSLNAME form-control' name='VSLNAME' readonly></td><td><input type=text id='VOYNO' value='" + rslt.VOYNO + "' class='VOYNO form-control' name='VOYNO' readonly></td><td><input type=text id='PRDTDESC' value='" + rslt.PRDTDESC + "'  class='PRDTDESC form-control' name='PRDTDESC' readonly></td><td><input type=text id='GPWGHT' value=" + rslt.GPWGHT + "  class='GPWGHT form-control' name='GPWGHT' readonly></td></tr></tbody>";

                }
                tabl = tabl + "</Table>";
                Response.Write(tabl);
            }
        }
        public void DetailEdit(string id)
        {
            var param = id.Split(';');
            using (SCFSERPContext contxt = new SCFSERPContext())
            {

                var query = context.Database.SqlQuery<VW_DELIVERY_ORDER_DETAIL_GRID_ASSGN>("select * from VW_DELIVERY_ORDER_DETAIL_GRID_ASSGN where DOMID=" + Convert.ToInt32(param[0]) + "").ToList();


                var tabl = " <div class='panel-heading navbar-inverse'  style=color:white>DO Details</div><Table id=mytabl class='table table-striped table-bordered bootstrap-datatable'> <thead><tr> <th>BOE NO</th><th>Container No</th><th> Size</th><th>InDate</th><th>Vessel</th> <th> Voy No </th><th>Product</th><th>Weight</th></tr> </thead>";

                foreach (var rslt in query)
                {

                    tabl = tabl + "<tbody><tr><td class=hide><input type=text id='DODID' class='DODID' name='DODID' value=" + rslt.DODID + "></td><td class=hide><input type=text id=F_GIDID value=" + rslt.GIDID + "  class=F_GIDID name=F_GIDID hidden></td><td class=hide><input type=text id=STMRNAME value='" + rslt.STMRNAME + "' class=STMRNAME name=STMRNAME></td><td class=hide><input type=text id=IMPRTNAME value='" + rslt.IMPRTNAME + "' class=IMPRTNAME name=IMPRTNAME hidden></td><td><input type=text value='" + rslt.DODREFNO + "' id='BILLEMDNO' class='form-control BILLEMDNO' name='BILLEMDNO'  readonly><input type=text value='" + rslt.BILLEDID + "' id='BILLEDID' class='hide form-control BILLEDID' name='BILLEDID'  readonly></td><td><input type=text id=CONTNRNO value=" + rslt.CONTNRNO + " class=CONTNRNO name=CONTNRNO readonly></td><td><input type=text value=" + rslt.CONTNRSDESC + " id=CONTNRSDESC class=CONTNRSDESC name=CONTNRSDESC style=width:45px readonly></td><td><input type=text id=GIDATE value=" + rslt.GIDATE + " class=GIDATE name=GIDATE readonly></td><td><input type=text value='" + rslt.VSLNAME + "' id='VSLNAME' class='VSLNAME form-control' name='VSLNAME' readonly></td><td><input type=text id='VOYNO' value='" + rslt.VOYNO + "' class='VOYNO form-control' name='VOYNO' readonly></td><td><input type=text id='PRDTDESC' value='" + rslt.PRDTDESC + "'  class='PRDTDESC form-control' name='PRDTDESC' readonly></td><td><input type=text id='GPWGHT' value=" + rslt.GPWGHT + "  class='GPWGHT form-control' name='GPWGHT' readonly></td><td class=hide><input type=text id='BILLEMID_MOD' value=" + rslt.BILLEMID + "  class='BILLEMID_MOD form-control' name='BILLEMID_MOD'><input type=text id='IGMNO_MOD' value=" + rslt.IGMNO + "  class='IGMNO_MOD form-control' name='IGMNO_MOD'><input type=text id='GPLNO_MOD' value=" + rslt.GPLNO + "  class='GPLNO_MOD form-control' name='GPLNO_MOD'><input type=text id='DPAIDNO_MOD' value=" + rslt.DPAIDNO + "  class='DPAIDNO_MOD form-control' name='DPAIDNO_MOD'><input type=text id='DPAIDAMT_MOD' value=" + rslt.DPAIDAMT + "  class='DPAIDAMT_MOD form-control' name='DPAIDAMT_MOD'></td></tr></tbody>";

                }
                tabl = tabl + "</Table>";
                Response.Write(tabl);
            }
        }
        //.................Insert/update values into database.............//
        public void savedata(FormCollection F_Form)
        {
            using (SCFSERPContext dataContext = new SCFSERPContext())
            {
                using (var trans = dataContext.Database.BeginTransaction())
                {
                    try
                    {
                        string todaydt = Convert.ToString(DateTime.Now);
                        string todayd = Convert.ToString(DateTime.Now.Date);

                        DeliveryOrderMaster DeliveryOrderMaster = new DeliveryOrderMaster();
                        DeliveryOrderDetail DeliveryOrderDetail = new DeliveryOrderDetail();
                        //-------Getting Primarykey field--------
                        Int32 DOMID = Convert.ToInt32(F_Form["masterdata.DOMID"]);
                        Int32 DODID = 0;
                        string DELIDS = "";
                        //-----End

                        // Capture BEFORE state for edit logging
                        DeliveryOrderMaster original = null;
                        if (DOMID != 0)
                        {
                            original = context.DeliveryOrderMasters.AsNoTracking().FirstOrDefault(x => x.DOMID == DOMID);
                        }

                        if (DOMID != 0)
                        {
                            DeliveryOrderMaster = context.DeliveryOrderMasters.Find(DOMID);
                        }

                        DeliveryOrderMaster.COMPYID = Convert.ToInt32(Session["compyid"]);
                        DeliveryOrderMaster.SDPTID = 1;

                        string indate = Convert.ToString(F_Form["masterdata.DODATE"]);                        
                        if (indate != null || indate != "")
                        {
                            DeliveryOrderMaster.DODATE = Convert.ToDateTime(indate).Date;
                        }
                        else { DeliveryOrderMaster.DODATE = DateTime.Now.Date; }

                        if (DeliveryOrderMaster.DODATE > Convert.ToDateTime(todayd))
                        { DeliveryOrderMaster.DODATE = Convert.ToDateTime(todayd); }

                        string intime = Convert.ToString(F_Form["masterdata.DOTIME"]);
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

                                    DeliveryOrderMaster.DOTIME = Convert.ToDateTime(in_datetime);
                                }
                                else { DeliveryOrderMaster.DOTIME = DateTime.Now; }
                            }
                            else { DeliveryOrderMaster.DOTIME = DateTime.Now; }
                        }
                        else { DeliveryOrderMaster.DOTIME = DateTime.Now; }

                        if (DeliveryOrderMaster.DOTIME > Convert.ToDateTime(todaydt))
                        { DeliveryOrderMaster.DOTIME = Convert.ToDateTime(todaydt); }

                        //DeliveryOrderMaster.DODATE = Convert.ToDateTime(F_Form["masterdata.DOTIME"]).Date;
                        //DeliveryOrderMaster.DOTIME = Convert.ToDateTime(F_Form["masterdata.DOTIME"]);

                        DeliveryOrderMaster.DOREFID = Convert.ToInt32(F_Form["masterdata.DOREFID"]);

                        DeliveryOrderMaster.DOREFNAME = F_Form["masterdata.DOREFNAME"].ToString();
                        DeliveryOrderMaster.DONARTN = null; DeliveryOrderMaster.DORMKS = null;
                        if (DOMID == 0 )
                            DeliveryOrderMaster.CUSRID = Session["CUSRID"].ToString();
                        DeliveryOrderMaster.LMUSRID = Session["CUSRID"].ToString();
                        DeliveryOrderMaster.DISPSTATUS = Convert.ToInt16(F_Form["DISPSTATUS"]);
                        DeliveryOrderMaster.PRCSDATE = DateTime.Now;
                        DeliveryOrderMaster.DOPCOUNT = 0;
                        DeliveryOrderMaster.TARIFFMID = Convert.ToInt32(F_Form["TARIFFMID"]);
                        DeliveryOrderMaster.DOMODE = Convert.ToInt16(F_Form["DOMODE"]);
                        DeliveryOrderMaster.DOMODEDETL = (F_Form["masterdata.DOMODEDETL"]);
                        DeliveryOrderMaster.DOREFAMT = Convert.ToDecimal(F_Form["masterdata.DOREFAMT"]);

                        //string OOCdate = Convert.ToString(F_Form["masterdata.OOCDATE"]);
                        //if (OOCdate != null || OOCdate != "")
                        //{
                        //    DeliveryOrderMaster.OOCDATE = Convert.ToDateTime(OOCdate).Date;
                        //}
                        //else { DeliveryOrderMaster.OOCDATE = DateTime.Now.Date; }

                        //DeliveryOrderMaster.OOCNO = (F_Form["masterdata.OOCNO"]);

                        var DOMODE = Convert.ToInt16(F_Form["DOMODE"]);
                        if (DOMODE != 2 && DOMODE != 3)
                        {
                            DeliveryOrderMaster.DOREFNO = "";
                            DeliveryOrderMaster.DOREFBNAME = "";
                            DeliveryOrderMaster.BANKMID = 0;
                            DeliveryOrderMaster.DOREFDATE = DateTime.Now;
                        }
                        else
                        {
                            DeliveryOrderMaster.DOREFNO = (F_Form["masterdata.DOREFNO"]).ToString();
                            DeliveryOrderMaster.DOREFBNAME = (F_Form["masterdata.DOREFBNAME"]).ToString();
                            DeliveryOrderMaster.BANKMID = Convert.ToInt32(F_Form["BANKMID"]);
                            DeliveryOrderMaster.DOREFDATE = Convert.ToDateTime(F_Form["masterdata.DOREFDATE"]).Date;

                        }

                        if (DOMID == 0)
                        {
                            DeliveryOrderMaster.DONO = Convert.ToInt32(Autonumber.autonum("DeliveryOrderMaster", "DONO", "DONO<>0 and SDPTID=1 and COMPYID=" + Convert.ToInt32(Session["compyid"]) + "").ToString());

                            int ano = DeliveryOrderMaster.DONO;
                            string prfx = string.Format( "{0:D5}", ano);
                            DeliveryOrderMaster.DODNO = prfx.ToString();

                            //........end of autonumber
                            context.DeliveryOrderMasters.Add(DeliveryOrderMaster);
                            context.SaveChanges();
                            DOMID = DeliveryOrderMaster.DOMID;
                        }
                        else
                        {
                            context.Entry(DeliveryOrderMaster).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }


                        //-------------transaction Details
                        string[] F_DODID = F_Form.GetValues("DODID");
                        // string[] F_GIDID = F_Form.GetValues("F_GIDID");
                        string[] BILLEDID = F_Form.GetValues("BILLEDID");
                        string[] DOIDATE = F_Form.GetValues("GIDATE");
                        string[] DODREFNO = F_Form.GetValues("BILLEMDNO");
                        string[] DODREFNAME = F_Form.GetValues("CONTNRNO");
                        string[] DODREFID = F_Form.GetValues("F_GIDID");
                        for (int count = 0; count < F_DODID.Count(); count++)
                        {

                            DODID = Convert.ToInt32(F_DODID[count]);

                            if (DODID != 0)
                            {
                                DeliveryOrderDetail = context.DeliveryOrderDetails.Find(DODID);
                            }
                            DeliveryOrderDetail.DOMID = DeliveryOrderMaster.DOMID;
                            DeliveryOrderDetail.DODREFNO = (DODREFNO[count]).ToString();
                            DeliveryOrderDetail.DODREFNAME = (DODREFNAME[count]).ToString();
                            DeliveryOrderDetail.DODREFID = Convert.ToInt32(DODREFID[count]);
                            DeliveryOrderDetail.DOVDATE = Convert.ToDateTime(F_Form["detaildata[0].DOVDATE"]);
                            DeliveryOrderDetail.DOIDATE = Convert.ToDateTime(DOIDATE[count]);
                            DeliveryOrderDetail.BILLEDID = Convert.ToInt32(BILLEDID[count]);
                            DeliveryOrderDetail.DISPSTATUS = 0;
                            //  DeliveryOrderDetail.DOVDATE = DateTime.Now;
                            //  DeliveryOrderDetail.DOIDATE = DateTime.Now;

                            if (Convert.ToInt32(DODID) == 0)
                            {
                                context.DeliveryOrderDetails.Add(DeliveryOrderDetail);
                                context.SaveChanges();
                                DODID = DeliveryOrderDetail.DODID;
                            }
                            else
                            {

                                context.Entry(DeliveryOrderDetail).State = System.Data.Entity.EntityState.Modified;
                                context.SaveChanges();
                            }//..............end
                            DELIDS = DELIDS + "," + DODID.ToString();

                            context.Database.ExecuteSqlCommand("UPDATE BILLENTRYMASTER SET DPAIDNO='" + F_Form["F_DPAIDNO"].ToString() + "',DPAIDAMT=" + Convert.ToDecimal(F_Form["F_DPAIDAMT"]) + " WHERE BILLEMID=" + Convert.ToInt32(F_Form["BILLEMID"]));

                        }


                        context.Database.ExecuteSqlCommand("DELETE FROM DeliveryOrderDetail  WHERE DOMID=" + DOMID + " and  DODID NOT IN(" + DELIDS.Substring(1) + ")");
                        trans.Commit();
                        
                        // Log changes to GateInDetailEditLog after successful save
                        try
                        {
                            if (DOMID != 0 && original != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ORIGINAL RECORD FOUND: DOMID={original.DOMID}, calling LogDeliveryOrderEdits");
                                
                                // Ensure baseline snapshot (Version = "0") exists for this record before logging diffs
                                EnsureBaselineVersionZero(original, Session["CUSRID"] != null ? Session["CUSRID"].ToString() : "");
                                
                                // Reload the saved record to get the final state (after transaction commit)
                                using (var logContext = new SCFSERPContext())
                                {
                                    var savedRecord = logContext.DeliveryOrderMasters.AsNoTracking().FirstOrDefault(x => x.DOMID == DOMID);
                                    if (savedRecord != null)
                                    {
                                        LogDeliveryOrderEdits(original, savedRecord, Session["CUSRID"] != null ? Session["CUSRID"].ToString() : "");
                                        System.Diagnostics.Debug.WriteLine($"LogDeliveryOrderEdits completed successfully");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"SAVED RECORD NOT FOUND after commit for DOMID={DeliveryOrderMaster.DOMID}");
                                    }
                                }
                            }
                            else if (DOMID != 0 && original == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ORIGINAL RECORD NOT FOUND for DOMID={DeliveryOrderMaster.DOMID}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the error for debugging - don't fail the save if logging fails
                            System.Diagnostics.Debug.WriteLine($"Edit logging failed: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        }
                        
                        Response.Redirect("Index");
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                        trans.Rollback();
                        //Response.Write("Sorry!!An Error Ocurred...");
                        Response.Redirect("/Error/AccessDenied");
                    }
                }
            }

        }
        [Authorize(Roles = "ImportDeliveryOrderPrint")]
        public void PrintView(int id)
        {
            //  ........delete TMPRPT...//
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "DO", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;


                cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] +  "DELIVERYORDER.RPT");

                cryRpt.RecordSelectionFormula = "{VW_DORDER_CRY_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_DORDER_CRY_PRINT_ASSGN.DOMID} = " + id;



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
        //end
        [Authorize(Roles = "ImportDeliveryOrderDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");

            //   var param = id.Split('-');

            String temp = Delete_fun.delete_check1(fld, id);

            if (temp.Equals("PROCEED"))
            {
                DeliveryOrderMaster DeliveryOrderMaster = context.DeliveryOrderMasters.Find(Convert.ToInt32(id));
                context.DeliveryOrderMasters.Remove(DeliveryOrderMaster);
                context.Database.ExecuteSqlCommand("delete from deliveryorderdetail where domid=" + Convert.ToInt32(id));
                context.SaveChanges();
                Response.Write("Deleted successfully...");
            }
            else
                Response.Write(temp);
        }//..End of delete

        #region Edit Log Methods
        public ActionResult EditLogDeliveryOrder(int? domid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
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
                                    WHERE [Modules] = 'DeliveryOrder'";
                    
                    if (domid.HasValue)
                    {
                        // Find DONO from DOMID
                        var deliveryOrderRecord = context.DeliveryOrderMasters.AsNoTracking().FirstOrDefault(x => x.DOMID == domid.Value);
                        if (deliveryOrderRecord != null && deliveryOrderRecord.DONO > 0)
                        {
                            query += " AND [GIDNO] = @DONO";
                        }
                        else
                        {
                            query += " AND CAST([GIDNO] AS INT) = @DOMID";
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
                        if (domid.HasValue)
                        {
                            var deliveryOrderRecord = context.DeliveryOrderMasters.AsNoTracking().FirstOrDefault(x => x.DOMID == domid.Value);
                            if (deliveryOrderRecord != null && deliveryOrderRecord.DONO > 0)
                            {
                                cmd.Parameters.AddWithValue("@DONO", deliveryOrderRecord.DONO.ToString());
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@DOMID", domid.Value);
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

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = reader["GIDNO"]?.ToString() ?? "",
                                    FieldName = reader["FieldName"]?.ToString() ?? "",
                                    OldValue = reader["OldValue"]?.ToString() ?? "",
                                    NewValue = reader["NewValue"]?.ToString() ?? "",
                                    ChangedBy = reader["ChangedBy"]?.ToString() ?? "",
                                    ChangedOn = reader["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(reader["ChangedOn"]) : DateTime.MinValue,
                                    Version = reader["Version"]?.ToString() ?? "",
                                    Modules = reader["Modules"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }

            // Apply friendly name mappings
            try
            {
                Func<string, string> Friendly = field =>
                {
                    if (string.IsNullOrWhiteSpace(field)) return field;
                    // Add field name mappings for DeliveryOrder if needed
                    return field.Replace("_", " ").Trim();
                };

                foreach (var row in list)
                {
                    row.FieldName = Friendly(row.FieldName);
                }
            }
            catch { /* Best-effort mapping */ }

            ViewBag.Module = "DeliveryOrder";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        public ActionResult EditLogDeliveryOrderCompare(int? domid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            // Fallbacks: try alternate parameter names
            if (domid == null)
            {
                int tmp;
                var qsDom = Request["domid"] ?? Request["id"];
                if (!string.IsNullOrWhiteSpace(qsDom) && int.TryParse(qsDom.Trim(), out tmp))
                {
                    domid = tmp;
                }
            }

            // Normalize version strings (remove any whitespace/tabs/newlines)
            versionA = (versionA ?? string.Empty).Trim().Replace("\t", "").Replace("\n", "").Replace("\r", "");
            versionB = (versionB ?? string.Empty).Trim().Replace("\t", "").Replace("\n", "").Replace("\r", "");

            if (domid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide DOMID, Version A and Version B to compare.";
                return RedirectToAction("EditLogDeliveryOrder", new { domid = domid });
            }

            // Get DONO from DOMID to use as GIDNO
            string gidnoString = "";
            if (domid.HasValue)
            {
                var deliveryOrderRecord = context.DeliveryOrderMasters.AsNoTracking().FirstOrDefault(x => x.DOMID == domid.Value);
                if (deliveryOrderRecord != null && deliveryOrderRecord.DONO > 0)
                {
                    gidnoString = deliveryOrderRecord.DONO.ToString();
                }
                else
                {
                    gidnoString = domid.Value.ToString();
                }
            }

            // Support baseline shortcuts - normalize to lowercase for baseline (as stored in DB)
            if (string.Equals(versionA, "0", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(versionA, "V0", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(versionA, "v0", StringComparison.OrdinalIgnoreCase) ||
                versionA.StartsWith("V0-", StringComparison.OrdinalIgnoreCase) ||
                versionA.StartsWith("v0-", StringComparison.OrdinalIgnoreCase))
            {
                versionA = "v0-" + gidnoString;
            }
            if (string.Equals(versionB, "0", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(versionB, "V0", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(versionB, "v0", StringComparison.OrdinalIgnoreCase) ||
                versionB.StartsWith("V0-", StringComparison.OrdinalIgnoreCase) ||
                versionB.StartsWith("v0-", StringComparison.OrdinalIgnoreCase))
            {
                versionB = "v0-" + gidnoString;
            }
            
            // Normalize version format: if it's V1-12513, keep it; if it's v1-12513, keep it
            // But ensure we match what's in the database

            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            var a = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var b = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    
                    // Debug: Check what versions exist for this GIDNO (try both DONO and DOMID)
                    var allGidnos = new List<string> { gidnoString };
                    if (domid.HasValue)
                    {
                        allGidnos.Add(domid.Value.ToString());
                    }
                    
                    foreach (var testGidno in allGidnos)
                    {
                        using (var debugCmd = new SqlCommand(@"SELECT DISTINCT [Version], COUNT(*) as RowCount 
                                                              FROM [dbo].[GateInDetailEditLog] 
                                                              WHERE [GIDNO]=@GIDNO AND [Modules]='DeliveryOrder'
                                                              GROUP BY [Version]", sql))
                        {
                            debugCmd.Parameters.AddWithValue("@GIDNO", testGidno);
                            var existingVersions = new List<string>();
                            try
                            {
                                using (var debugR = debugCmd.ExecuteReader())
                                {
                                    while (debugR.Read())
                                    {
                                        var ver = Convert.ToString(debugR["Version"]);
                                        var count = Convert.ToInt32(debugR["RowCount"]);
                                        existingVersions.Add($"{ver} ({count} rows)");
                                    }
                                }
                                if (existingVersions.Count > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: GIDNO={testGidno}, Existing versions: {string.Join(", ", existingVersions)}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: Error checking GIDNO={testGidno}: {ex.Message}");
                            }
                        }
                    }
                    
                    // Query for version A - try multiple GIDNO formats and case variations
                    string queryA = @"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                      FROM [dbo].[GateInDetailEditLog]
                                      WHERE [Modules]='DeliveryOrder' 
                                      AND ([GIDNO]=@GIDNO1 OR [GIDNO]=@GIDNO2)
                                      AND LOWER(RTRIM(LTRIM([Version]))) = LOWER(RTRIM(LTRIM(@V)))";
                    
                    using (var cmd = new SqlCommand(queryA, sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO1", gidnoString);
                        cmd.Parameters.AddWithValue("@GIDNO2", domid.HasValue ? domid.Value.ToString() : gidnoString);
                        cmd.Parameters.AddWithValue("@V", versionA);
                        
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                a.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = Convert.ToString(r["GIDNO"]),
                                    FieldName = Convert.ToString(r["FieldName"]),
                                    OldValue = r["OldValue"] == DBNull.Value ? null : Convert.ToString(r["OldValue"]),
                                    NewValue = r["NewValue"] == DBNull.Value ? null : Convert.ToString(r["NewValue"]),
                                    ChangedBy = Convert.ToString(r["ChangedBy"]),
                                    ChangedOn = r["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r["ChangedOn"]) : DateTime.MinValue,
                                    Version = Convert.ToString(r["Version"]),
                                    Modules = Convert.ToString(r["Modules"])
                                });
                            }
                        }
                    }

                    // Query for version B - try multiple GIDNO formats and case variations
                    string queryB = @"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                      FROM [dbo].[GateInDetailEditLog]
                                      WHERE [Modules]='DeliveryOrder' 
                                      AND ([GIDNO]=@GIDNO1 OR [GIDNO]=@GIDNO2)
                                      AND LOWER(RTRIM(LTRIM([Version]))) = LOWER(RTRIM(LTRIM(@V)))";
                    
                    using (var cmd = new SqlCommand(queryB, sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO1", gidnoString);
                        cmd.Parameters.AddWithValue("@GIDNO2", domid.HasValue ? domid.Value.ToString() : gidnoString);
                        cmd.Parameters.AddWithValue("@V", versionB);
                        
                        using (var r2 = cmd.ExecuteReader())
                        {
                            while (r2.Read())
                            {
                                b.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = Convert.ToString(r2["GIDNO"]),
                                    FieldName = Convert.ToString(r2["FieldName"]),
                                    OldValue = r2["OldValue"] == DBNull.Value ? null : Convert.ToString(r2["OldValue"]),
                                    NewValue = r2["NewValue"] == DBNull.Value ? null : Convert.ToString(r2["NewValue"]),
                                    ChangedBy = Convert.ToString(r2["ChangedBy"]),
                                    ChangedOn = r2["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r2["ChangedOn"]) : DateTime.MinValue,
                                    Version = Convert.ToString(r2["Version"]),
                                    Modules = Convert.ToString(r2["Modules"])
                                });
                            }
                        }
                    }
                    
                    // Debug output
                    System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: DOMID={domid}, DONO={gidnoString}, VersionA={versionA}, VersionB={versionB}");
                    System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: Found {a.Count} rows for version A, {b.Count} rows for version B");
                    if (a.Count > 0)
                        System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: First row A - GIDNO={a[0].GIDNO}, Version={a[0].Version}, Field={a[0].FieldName}, Old={a[0].OldValue}, New={a[0].NewValue}");
                    if (b.Count > 0)
                        System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: First row B - GIDNO={b[0].GIDNO}, Version={b[0].Version}, Field={b[0].FieldName}, Old={b[0].OldValue}, New={b[0].NewValue}");
                    
                    // If no data found, try a broader query to see what exists
                    if (a.Count == 0 && b.Count == 0)
                    {
                        using (var fallbackCmd = new SqlCommand(@"SELECT TOP 10 [GIDNO], [Version], [FieldName], COUNT(*) as cnt
                                                                  FROM [dbo].[GateInDetailEditLog]
                                                                  WHERE [Modules]='DeliveryOrder' 
                                                                  AND ([GIDNO]=@GIDNO1 OR [GIDNO]=@GIDNO2)
                                                                  GROUP BY [GIDNO], [Version], [FieldName]", sql))
                        {
                            fallbackCmd.Parameters.AddWithValue("@GIDNO1", gidnoString);
                            fallbackCmd.Parameters.AddWithValue("@GIDNO2", domid.HasValue ? domid.Value.ToString() : gidnoString);
                            using (var fallbackR = fallbackCmd.ExecuteReader())
                            {
                                var found = new List<string>();
                                while (fallbackR.Read())
                                {
                                    found.Add($"GIDNO={fallbackR["GIDNO"]}, Version={fallbackR["Version"]}, Field={fallbackR["FieldName"]}, Count={fallbackR["cnt"]}");
                                }
                                if (found.Count > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: Found data but version mismatch. Sample rows: {string.Join("; ", found)}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: No data found for GIDNO={gidnoString} or DOMID={domid}");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("EditLogDeliveryOrderCompare: No SCFSERP_EditLog connection string found");
            }

            // Map values
            try
            {
                var dictBank = context.bankmasters.ToDictionary(x => x.BANKMID, x => x.BANKMDESC);
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTariff = context.tariffmasters.ToDictionary(x => x.TARIFFMID, x => x.TARIFFMDESC);
                var dictMode = context.transactionmodemaster.ToDictionary(x => x.TRANMODE, x => x.TRANMODEDETL);

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrWhiteSpace(val)) return val;
                    try
                    {
                        int id = 0;
                        if (field == "BANKMID" && int.TryParse(val, out id) && dictBank.ContainsKey(id))
                            return dictBank[id];
                        if (field == "DOREFID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "TARIFFMID" && int.TryParse(val, out id) && dictTariff.ContainsKey(id))
                            return dictTariff[id];
                        if (field == "DOMODE" && int.TryParse(val, out id) && dictMode.ContainsKey(id))
                            return dictMode[id];
                        if (field == "DISPSTATUS")
                            return val == "1" ? "CANCELLED" : val == "0" ? "INBOOKS" : val;
                    }
                    catch { }
                    return val;
                };

                Func<string, string> Friendly = field =>
                {
                    if (string.IsNullOrWhiteSpace(field)) return field;
                    // DeliveryOrder field name mappings
                    var fieldNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        {"DODATE", "Date"}, {"DOTIME", "Time"}, {"DONO", "No"}, {"DODNO", "DO Number"},
                        {"DOREFID", "CHA"}, {"DOREFNAME", "CHA Name"}, {"TARIFFMID", "Tariff"},
                        {"DOMODE", "Mode"}, {"DOMODEDETL", "Mode Detail"}, {"DOREFAMT", "Amount"},
                        {"DOREFNO", "Reference Number"}, {"DOREFDATE", "Reference Date"},
                        {"DOREFBNAME", "Bank Name"}, {"BANKMID", "Bank"}, {"DISPSTATUS", "Status"},
                        {"PRCSDATE", "Process Date"}
                    };
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

            // Check if version A is V0
            bool isVersionAV0 = !string.IsNullOrEmpty(versionA) && 
                (versionA.StartsWith("v0-", StringComparison.OrdinalIgnoreCase) || 
                 versionA.StartsWith("V0-", StringComparison.OrdinalIgnoreCase) || 
                 versionA.Equals("v0", StringComparison.OrdinalIgnoreCase) || 
                 versionA.Equals("V0", StringComparison.OrdinalIgnoreCase) || 
                 versionA == "0");
            
            // If no data found for V0, try to create it on-the-fly from the current record
            if (a.Count == 0 && isVersionAV0 && domid.HasValue && cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: V0 baseline not found, attempting to create from current record");
                var currentRecord = context.DeliveryOrderMasters.AsNoTracking().FirstOrDefault(x => x.DOMID == domid.Value);
                if (currentRecord != null)
                {
                    // Create baseline snapshot entries
                    EnsureBaselineVersionZero(currentRecord, Session["CUSRID"] != null ? Session["CUSRID"].ToString() : "");
                    // Re-query for V0
                    using (var sql = new SqlConnection(cs.ConnectionString))
                    {
                        sql.Open();
                        string queryA = @"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                          FROM [dbo].[GateInDetailEditLog]
                                          WHERE [Modules]='DeliveryOrder' 
                                          AND ([GIDNO]=@GIDNO1 OR [GIDNO]=@GIDNO2)
                                          AND LOWER(RTRIM(LTRIM([Version]))) = LOWER(RTRIM(LTRIM(@V)))";
                        using (var cmd = new SqlCommand(queryA, sql))
                        {
                            cmd.Parameters.AddWithValue("@GIDNO1", gidnoString);
                            cmd.Parameters.AddWithValue("@GIDNO2", domid.Value.ToString());
                            cmd.Parameters.AddWithValue("@V", versionA);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    a.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                    {
                                        GIDNO = Convert.ToString(r["GIDNO"]),
                                        FieldName = Convert.ToString(r["FieldName"]),
                                        OldValue = r["OldValue"] == DBNull.Value ? null : Convert.ToString(r["OldValue"]),
                                        NewValue = r["NewValue"] == DBNull.Value ? null : Convert.ToString(r["NewValue"]),
                                        ChangedBy = Convert.ToString(r["ChangedBy"]),
                                        ChangedOn = r["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(r["ChangedOn"]) : DateTime.MinValue,
                                        Version = Convert.ToString(r["Version"]),
                                        Modules = Convert.ToString(r["Modules"])
                                    });
                                }
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: After creating baseline, found {a.Count} rows for version A");
                }
            }

            // Ensure GIDNO is set to DONO (the actual delivery order number used in the log)
            // If DONO is not available, fall back to DOMID
            string displayGidno = gidnoString;
            if (string.IsNullOrEmpty(displayGidno) && domid.HasValue)
            {
                displayGidno = domid.Value.ToString();
            }
            
            // Try to parse as int for display, but keep as string for ViewBag
            int displayGidnoInt = 0;
            if (!string.IsNullOrEmpty(displayGidno) && int.TryParse(displayGidno, out displayGidnoInt))
            {
                ViewBag.GIDNO = displayGidnoInt; // Pass as int so view can display it correctly
            }
            else
            {
                ViewBag.GIDNO = displayGidno; // Pass as string if parsing fails
            }
            
            ViewBag.VersionA = versionA;
            ViewBag.VersionB = versionB;
            ViewBag.RowsA = a;
            ViewBag.RowsB = b;
            ViewBag.Module = "DeliveryOrder";
            
            // Debug: Log what we're passing to the view
            System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: Passing to view - GIDNO={displayGidno} (int: {displayGidnoInt}), VersionA={versionA}, VersionB={versionB}, RowsA={a.Count}, RowsB={b.Count}");
            if (a.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: Sample row A - GIDNO={a[0].GIDNO}, Field={a[0].FieldName}, Version={a[0].Version}");
            }
            if (b.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"EditLogDeliveryOrderCompare: Sample row B - GIDNO={b[0].GIDNO}, Field={b[0].FieldName}, Version={b[0].Version}");
            }
            
            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        private void LogDeliveryOrderEdits(DeliveryOrderMaster before, DeliveryOrderMaster after, string userId)
        {
            if (before == null || after == null)
            {
                System.Diagnostics.Debug.WriteLine($"LogDeliveryOrderEdits: before={before != null}, after={after != null}");
                return;
            }
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine("LogDeliveryOrderEdits: No SCFSERP_EditLog connection string found");
                return;
            }

            // Exclude system or noisy fields
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DOMID", "COMPYID", "SDPTID", "PRCSDATE", "LMUSRID", "CUSRID",
                "DOPCOUNT", "DONARTN", "DORMKS"
            };

            // Compute the next version ONCE per save
            // Use DONO as GIDNO (convert to string)
            var gidno = after.DONO > 0 ? after.DONO.ToString() : after.DOMID.ToString();
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
                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'DeliveryOrder'", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", gidno);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        nextVersion = Convert.ToInt32(obj);
                }
            }
            catch { /* ignore logging version errors */ }

            var props = typeof(DeliveryOrderMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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

                if (type == typeof(decimal))
                {
                    var d1 = ToNullableDecimal(ov) ?? 0m;
                    var d2 = ToNullableDecimal(nv) ?? 0m;
                    if (d1 == 0m && d2 == 0m) continue;
                    changed = d1 != d2;
                }
                else if (type == typeof(double) || type == typeof(float))
                {
                    var d1 = Convert.ToDouble(ov ?? 0.0);
                    var d2 = Convert.ToDouble(nv ?? 0.0);
                    if (Math.Abs(d1) < 1e-9 && Math.Abs(d2) < 1e-9) continue;
                    changed = Math.Abs(d1 - d2) > 1e-9;
                }
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                {
                    var i1 = Convert.ToInt64(ov ?? 0);
                    var i2 = Convert.ToInt64(nv ?? 0);
                    if (i1 == 0 && i2 == 0) continue;
                    changed = i1 != i2;
                }
                else if (type == typeof(DateTime))
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

                var versionLabel = $"V{nextVersion}-{gidno}";
                
                System.Diagnostics.Debug.WriteLine($"Logging change: Field={p.Name}, Old={os}, New={ns}, Version={versionLabel}, GIDNO={gidno}");
                InsertEditLogRow(cs.ConnectionString, gidno, p.Name, os, ns, userId, versionLabel, "DeliveryOrder");
            }
            
            System.Diagnostics.Debug.WriteLine($"LogDeliveryOrderEdits completed. Total fields processed, changes logged for GIDNO={gidno}");
        }

        private string FormatValForLogging(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            if (string.IsNullOrEmpty(formattedValue)) return formattedValue;

            try
            {
                int lookupId;
                if (fieldName == "BANKMID" && int.TryParse(formattedValue, out lookupId))
                {
                    var bank = context.bankmasters.FirstOrDefault(x => x.BANKMID == lookupId);
                    if (bank != null) return bank.BANKMDESC;
                }
                else if (fieldName == "TARIFFMID" && int.TryParse(formattedValue, out lookupId))
                {
                    var tariff = context.tariffmasters.FirstOrDefault(x => x.TARIFFMID == lookupId);
                    if (tariff != null) return tariff.TARIFFMDESC;
                }
                else if (fieldName == "DOMODE" && int.TryParse(formattedValue, out lookupId))
                {
                    var mode = context.transactionmodemaster.FirstOrDefault(x => x.TRANMODE == lookupId);
                    if (mode != null) return mode.TRANMODEDETL;
                }
                else if (fieldName == "DOREFID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cate = context.categorymasters.FirstOrDefault(x => x.CATEID == lookupId);
                    if (cate != null) return cate.CATENAME;
                }
                else if (fieldName == "DISPSTATUS")
                {
                    return formattedValue == "1" ? "CANCELLED" : formattedValue == "0" ? "INBOOKS" : formattedValue;
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

        private void EnsureBaselineVersionZero(DeliveryOrderMaster snapshot, string userId)
        {
            if (snapshot == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            // Use DONO as GIDNO (convert to string)
            var gidno = snapshot.DONO > 0 ? snapshot.DONO.ToString() : snapshot.DOMID.ToString();
            var baselineVer = "v0-" + gidno;

            // Check if baseline already exists
            try
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM [dbo].[GateInDetailEditLog] 
                                                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'DeliveryOrder' 
                                                    AND RTRIM(LTRIM([Version])) = @V", sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO", gidno);
                        cmd.Parameters.AddWithValue("@V", baselineVer);
                        var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                        if (exists) return; // Baseline already exists
                    }
                }
            }
            catch { return; }

            InsertBaselineSnapshot(snapshot, userId, cs.ConnectionString, gidno, baselineVer);
        }

        private void InsertBaselineSnapshot(DeliveryOrderMaster snapshot, string userId, string connectionString, string gidno, string baselineVer)
        {
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DOMID", "COMPYID", "SDPTID", "PRCSDATE", "LMUSRID", "CUSRID",
                "DOPCOUNT", "DONARTN", "DORMKS"
            };

            var props = typeof(DeliveryOrderMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                    bool isDefault = string.IsNullOrEmpty(s) || s == "-" || s == "0" || s == "0.0" || s == "0.00" || s == "0.000" || s == "0.0000";
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
                else if (type == typeof(double) || type == typeof(float))
                {
                    var d = Convert.ToDouble(valObj ?? 0.0);
                    if (Math.Abs(d) < 1e-9) continue;
                }

                var newVal = FormatValForLogging(p.Name, valObj);
                InsertEditLogRow(connectionString, gidno, p.Name, null, newVal, userId, baselineVer, "DeliveryOrder");
            }
        }
        #endregion
    }
}