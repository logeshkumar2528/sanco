using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using scfs.Data;
using scfs_erp.Context;
using scfs_erp.Helper;
using scfs_erp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using System.Data.Entity;

namespace scfs.Controllers.Bond
{
    public class ExBondVehicleTicketController : Controller
    {
        // GET: ExBondVehicleTicket
        #region Context Declaration
        BondContext context = new BondContext();

        public static String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
        SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);

        #endregion

        #region Index Page
        //[Authorize(Roles = "ExBondVTIndex")]
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
           
            //....end    
            DateTime fromdate = Convert.ToDateTime(System.Web.HttpContext.Current.Session["SDATE"]).Date;
            DateTime todate = Convert.ToDateTime(System.Web.HttpContext.Current.Session["EDATE"]).Date;

            return View();
        }
        #endregion

        #region Get Table Data
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            using (var e = new BondEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                var data = e.pr_Search_Bond_Vehicle_Ticket(param.sSearch, Convert.ToInt32(Request["iSortCol_0"]), Request["sSortDir_0"], param.iDisplayStart, param.iDisplayStart + param.iDisplayLength,
                    totalRowsCount, filteredRowsCount, Convert.ToInt32(Session["compyid"]), Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]));
                var aaData = data.Select(d => new string[] { d.VTDATE.Value.ToString("dd/MM/yyyy"), d.VTDNO.ToString(),  d.EBNDDNO, d.BNDDNO, d.PRDTGDESC.ToString(), d.VHLNO, d.DRIVERNAME, d.VTQTY.ToString(), d.VTDID.ToString() }).ToArray();
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

        #region Redirect to Form
        //[Authorize(Roles = "ExBondVehicleTicketEdit")]
        public void Edit(string id)
        {
            Response.Redirect("/ExBondVehicleTicket/Form/" + id);
        }
        #endregion

        #region VT Cargo form
        //[Authorize(Roles = "ExBondVehicleTicketCreate")]
        public ActionResult Form(string id = "0")
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            ExBondVehicleTicket tab = new ExBondVehicleTicket();

            var VTDID = 0;
            if (id != "0")
            {
                VTDID = Convert.ToInt32(id);
            }
            else
            {
                tab.VTDATE = DateTime.Now.Date;
                tab.VTTIME = DateTime.Now;
            }

            tab.VTDID = 0;

            var mtqry = context.Database.SqlQuery<pr_get_BondMaster_Status_Result>("exec pr_Get_ExBond_Nos_VT 0 ").ToList();
            ViewBag.EBNDID = new SelectList(mtqry.OrderBy(x => x.dtxt), "dval", "dtxt").ToList();
            ViewBag.CONTNRSID = new SelectList(context.containersizemasters.Where(m => m.CONTNRSID > 1).Where(x => x.DISPSTATUS == 0), "CONTNRSID", "CONTNRSDESC");
            if (VTDID != 0)//Edit Mode
            {
                tab = context.exbondvtdtls.Find(VTDID); 
                
                //-----------Getting Gate_In Details-----------------//

                var query = context.Database.SqlQuery<string>("select CONTNRNO from VW_EXPORT_VEHICLE_TICKET_MOD_ASSGN where VTDID=" + VTDID).ToList();

                mtqry = context.Database.SqlQuery<pr_get_BondMaster_Status_Result>("exec pr_Get_ExBond_Nos_VT " + VTDID).ToList();
                ViewBag.EBNDID = new SelectList(mtqry.OrderBy(x=>x.dtxt), "dval", "dtxt",tab.EBNDID).ToList();
                ViewBag.CONTNRSID = new SelectList(context.containersizemasters.Where(m => m.CONTNRSID > 1).Where(x => x.DISPSTATUS == 0), "CONTNRSID", "CONTNRSDESC",tab.CONTNRSID);
                var query1 = (from m in context.bondinfodtls
                              join e in context.exbondinfodtls on m.BNDID equals e.BNDID
                              join c in context.categorymasters on m.CHAID equals c.CATEID
                              join i in context.categorymasters on m.IMPRTID equals i.CATEID
                              where (e.EBNDID == tab.EBNDID && c.CATETID == 4 && i.CATETID == 1)
                              select new {  CHANAME=c.CATENAME, m.CHAID, m.BNDIGMNO, m.BNDNO, m.BNDID, e.EBNDID,e.EBNDDNO,m.PRDTDESC, m.IMPRTID, IMPRTRNAME = i.CATENAME  }
                            ).ToList();

                if (query1.Count > 0)
                {
                    ViewBag.PRDTDESC = query1[0].PRDTDESC.ToString();
                    ViewBag.BNDIGMNO = query1[0].BNDIGMNO.ToString();
                    ViewBag.BNDNO = query1[0].BNDNO.ToString();
                    ViewBag.CHANAME = query1[0].CHANAME.ToString();
                    ViewBag.IMPRTRNAME = query1[0].IMPRTRNAME.ToString();
                    ViewBag.EBNDDNO = query1[0].EBNDDNO.ToString();
                    
                }
                
                
            }
            return View(tab);
        }
        #endregion

        #region Autocomplete ExBond Details
        public JsonResult AutoExBondNo(string term)
        {


            var result = context.Database.SqlQuery<pr_get_BondMaster_Status_Result>("exec pr_Get_ExBond_Nos_VT @vtid=0, @term='" + term +"'").ToList();
            
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region savedata
        public void savedata(ExBondVehicleTicket tab)
        {
            using (context = new BondContext())
            {

                try
                {
                    using (var trans = context.Database.BeginTransaction())
                    {   
                        tab.COMPYID = Convert.ToInt32(Session["compyid"]);
                        tab.SDPTID = 10;
                        tab.DISPSTATUS = 0;
                        tab.PRCSDATE = DateTime.Now;

                        string nop = Convert.ToString(tab.VTQTY);

                        if (nop == "" || nop == null)
                        { tab.VTQTY = 0; }
                        else { tab.VTQTY = Convert.ToDecimal(nop); }

                        string EBNDID = Convert.ToString(tab.EBNDID);

                        if (EBNDID == "" || EBNDID == null)
                        { tab.EBNDID = 0; }
                        else { tab.EBNDID = Convert.ToInt32(EBNDID); }

                        string CONTNRSID = Convert.ToString(tab.CONTNRSID);

                        if (CONTNRSID == "" || CONTNRSID == null)
                        { tab.CONTNRSID = 0; }
                        else { tab.CONTNRSID = Convert.ToInt32(CONTNRSID); }

                        string EBVTNOC = Convert.ToString(tab.EBVTNOC);

                        if (EBVTNOC == "" || EBVTNOC == null)
                        { tab.EBVTNOC = 0; }
                        else { tab.EBVTNOC = Convert.ToDecimal(EBVTNOC); }

                        tab.VTSTYPE = 0;
                        tab.VTTYPE = 0;

                        string indate = Convert.ToString(tab.VTDATE);
                        if (indate != null || indate != "")
                        {
                            tab.VTDATE = Convert.ToDateTime(indate).Date;
                        }
                        else { tab.VTDATE = DateTime.Now.Date; }

                        string intime = Convert.ToString(tab.VTTIME);
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

                                    tab.VTTIME = Convert.ToDateTime(in_datetime);
                                }
                                else { tab.VTTIME = DateTime.Now; }
                            }
                            else { tab.VTTIME = DateTime.Now; }
                        }
                        else { tab.VTTIME = DateTime.Now; }

                        
                        string userId = Session["CUSRID"]?.ToString() ?? "";
                        
                        if ((tab.VTDID).ToString() != "0")
                        {
                            // Capture before state for edit logging
                            ExBondVehicleTicket before = null;
                            try
                            {
                                before = context.exbondvtdtls.AsNoTracking().FirstOrDefault(x => x.VTDID == tab.VTDID);
                                if (before != null)
                                {
                                    EnsureBaselineVersionZero(before, userId);
                                }
                            }
                            catch { /* ignore if baseline creation fails */ }

                            tab.LMUSRID = userId;
                            context.Entry(tab).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();

                            // Log changes after successful save - use tab directly since it has the updated values after SaveChanges
                            if (before != null)
                            {
                                try
                                {
                                    LogExBondVehicleTicketEdits(before, tab, userId);
                                }
                                catch (Exception ex)
                                {
                                    // Log error for debugging but don't fail the save
                                    System.Diagnostics.Debug.WriteLine($"ExBondVehicleTicket edit logging failed: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            tab.VTNO = Convert.ToInt32(Autonumber.autonum("BONDVEHICLETICKETDETAIL", "VTNO", "COMPYID=" + Convert.ToInt32(Session["compyid"]) + " and SDPTID=10 and VTSTYPE=0").ToString());
                            tab.CUSRID = userId;
                            int ano = tab.VTNO;
                            string prfx = string.Format("{0:D5}", ano);
                            tab.VTDNO = prfx.ToString();
                            context.exbondvtdtls.Add(tab);
                            context.SaveChanges();

                            // Create baseline for new record
                            try
                            {
                                var newRecord = context.exbondvtdtls.AsNoTracking().FirstOrDefault(x => x.VTDID == tab.VTDID);
                                if (newRecord != null)
                                {
                                    EnsureBaselineVersionZero(newRecord, userId);
                                }
                            }
                            catch { /* ignore baseline creation errors */ }
                        }
                        
                        trans.Commit(); Response.Redirect("Index");
                    }
                }
                catch
                {
                    //trans.Rollback();
                    Response.Redirect("/Error/AccessDenied");
                }
            }
        }
        #endregion


        #region DeleteVTInfo        
        //[Authorize(Roles = "ExBondVTDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");
            // Code Modified for validating the by Rajesh / Yamuna on 16-Jul-2021 <Start>
            String temp = Delete_fun.delete_check1(fld, id);
            //var d = context.Database.SqlQuery<int>("Select count(GIDID) as 'Cnt' from AUTHORIZATIONSLIPDETAIL (nolock) where GIDID=" + Convert.ToInt32(id)).ToList();


            //if (d[0] == 0 || d[0] == null )
            if (temp.Equals("PROCEED"))
            // Code Modified for validating the by Rajesh / Yamuna on 16-Jul-2021 <End>
            {
                ExBondVehicleTicket exbondvt = context.exbondvtdtls.Find(Convert.ToInt32(id));
                context.exbondvtdtls.Remove(exbondvt);
                context.SaveChanges();

                Response.Write("Deleted Successfully ...");
            }
            else
            {                
                Response.Write("Record already exists, deletion is not possible!");                
            }

        }
        #endregion

        public void PrintView(int? id = 0)
        {
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "BondGateIn", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                CrystalDecisions.CrystalReports.Engine.Tables CrTables;

                cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "ExBond_VT.rpt");
                cryRpt.RecordSelectionFormula = "{VW_EXBOND_VT_PRINT_RPT.KUSRID} ='" + Session["CUSRID"].ToString() + "' and {VW_EXBOND_VT_PRINT_RPT.VTDID} =" + id;

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

        #region Edit Log Pages
        public ActionResult EditLogVehicleTicket(int? vtdid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var list = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                // Get actual VTDNO string from database to handle leading zeros
                string vtdnoString = null;
                if (vtdid.HasValue)
                {
                    try
                    {
                        var vehicleTicket = context.exbondvtdtls.AsNoTracking().FirstOrDefault(x => x.VTDID == vtdid.Value);
                        if (vehicleTicket != null && !string.IsNullOrEmpty(vehicleTicket.VTDNO))
                        {
                            vtdnoString = vehicleTicket.VTDNO;
                        }
                        else
                        {
                            vtdnoString = vtdid.Value.ToString();
                        }
                    }
                    catch { vtdnoString = vtdid.Value.ToString(); }
                }

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"SELECT TOP 2000 [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [Modules] = 'ExBondVehicleTicket'
                                                  AND (@VTDNO_STR IS NULL OR CAST([GIDNO] AS NVARCHAR(50)) = @VTDNO_STR OR CAST([GIDNO] AS NVARCHAR(50)) = CAST(@VTDID AS NVARCHAR(50)))
                                                  AND (@FROM IS NULL OR [ChangedOn] >= @FROM)
                                                  AND (@TO   IS NULL OR [ChangedOn] <  DATEADD(day, 1, @TO))
                                                  AND (@USER IS NULL OR [ChangedBy] LIKE @USERPAT)
                                                  AND (@FIELD IS NULL OR [FieldName] LIKE @FIELDPAT)
                                                  AND (@VERSION IS NULL OR [Version] LIKE @VERPAT)
                                                  AND NOT (RTRIM(LTRIM([Version])) IN ('0','V0') OR LEFT(RTRIM(LTRIM([Version])),3) IN ('v0-','V0-'))
                                                ORDER BY [ChangedOn] DESC, [GIDNO] DESC", sql))
                {
                    cmd.Parameters.Add("@VTDID", System.Data.SqlDbType.Int);
                    cmd.Parameters.Add("@VTDNO_STR", System.Data.SqlDbType.NVarChar, 50);
                    cmd.Parameters["@VTDID"].Value = vtdid.HasValue ? (object)vtdid.Value : DBNull.Value;
                    cmd.Parameters["@VTDNO_STR"].Value = !string.IsNullOrEmpty(vtdnoString) ? (object)vtdnoString : DBNull.Value;
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
                var dictContainerSize = context.containersizemasters.Where(c => c.DISPSTATUS == 0)
                    .GroupBy(x => x.CONTNRSID)
                    .ToDictionary(g => g.Key, g => g.First().CONTNRSDESC);

                string Map(string field, string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return raw;
                    int ival;
                    switch (field?.ToUpperInvariant())
                    {
                        case "CONTNRSID":
                            return int.TryParse(raw, out ival) && dictContainerSize.ContainsKey(ival) ? dictContainerSize[ival] : raw;
                        case "VTTYPE":
                            if (raw == "1") return "Empty";
                            if (raw == "2") return "Loaded";
                            return raw;
                        case "VTSTYPE":
                            if (raw == "1") return "In";
                            if (raw == "2") return "Out";
                            return raw;
                        case "DISPSTATUS":
                            return raw == "1" ? "Disabled" : raw == "0" ? "Enabled" : raw;
                        default:
                            return raw;
                    }
                }

                string Friendly(string field)
                {
                    if (string.IsNullOrWhiteSpace(field)) return field;
                    var f = field.Trim();
                    switch (f.ToUpperInvariant())
                    {
                        case "VTDATE": return "Date";
                        case "VTTIME": return "Time";
                        case "VTNO": return "Ticket No";
                        case "VTDNO": return "Ticket Detail No";
                        case "VHLNO": return "Vehicle No";
                        case "DRVNAME": return "Driver Name";
                        case "VTDESC": return "Description";
                        case "VTQTY": return "NOP";
                        case "VTTYPE": return "Type";
                        case "VTSTYPE": return "Status Type";
                        case "VTREMRKS": return "Remarks";
                        case "EBNDID": return "ExBond ID";
                        case "CONTNRSID": return "Container Size";
                        case "EBVTNOC": return "No.of Containers";
                        case "VTAREA": return "Space";
                        case "DISPSTATUS": return "Status";
                        default: return field;
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

            ViewBag.Module = "ExBondVehicleTicket";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        // Compare two versions for a given VTDNO
        public ActionResult EditLogVehicleTicketCompare(int? vtdid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            if (vtdid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide VTDID, Version A and Version B to compare.";
                return RedirectToAction("EditLogVehicleTicket", new { vtdid = vtdid });
            }

            // Get VTDNO from VTDID
            string vtdnoString = vtdid.Value.ToString();
            var vehicleTicket = context.exbondvtdtls.AsNoTracking().FirstOrDefault(x => x.VTDID == vtdid.Value);
            if (vehicleTicket != null && !string.IsNullOrEmpty(vehicleTicket.VTDNO))
            {
                vtdnoString = vehicleTicket.VTDNO;
            }

            // Normalize version strings
            versionA = (versionA ?? string.Empty).Trim();
            versionB = (versionB ?? string.Empty).Trim();
            
            // Map '0' or 'v0'/'V0' to 'v0-<VTDNO>' for baseline comparisons
            if (vtdid.HasValue)
            {
                var baseLabel = "v0-" + vtdnoString;
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
                                                WHERE [Modules] = 'ExBondVehicleTicket'
                                                  AND (CAST([GIDNO] AS NVARCHAR(50)) = @VTDNO_STR OR CAST([GIDNO] AS NVARCHAR(50)) = CAST(@VTDID AS NVARCHAR(50)))
                                                  AND RTRIM(LTRIM([Version])) = @V", sql))
                {
                    cmd.Parameters.Add("@VTDID", System.Data.SqlDbType.Int);
                    cmd.Parameters.Add("@VTDNO_STR", System.Data.SqlDbType.NVarChar, 50);
                    cmd.Parameters.Add("@V", System.Data.SqlDbType.NVarChar, 100);

                    sql.Open();
                    cmd.Parameters["@VTDID"].Value = vtdid.Value;
                    cmd.Parameters["@VTDNO_STR"].Value = vtdnoString;
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

            // Map technical field names to friendly form labels
            try
            {
                var dictContainerSize = context.containersizemasters.Where(c => c.DISPSTATUS == 0)
                    .GroupBy(x => x.CONTNRSID)
                    .ToDictionary(g => g.Key, g => g.First().CONTNRSDESC);

                string Map(string field, string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return raw;
                    int ival;
                    switch (field?.ToUpperInvariant())
                    {
                        case "CONTNRSID":
                            return int.TryParse(raw, out ival) && dictContainerSize.ContainsKey(ival) ? dictContainerSize[ival] : raw;
                        case "VTTYPE":
                            if (raw == "1") return "Empty";
                            if (raw == "2") return "Loaded";
                            return raw;
                        case "VTSTYPE":
                            if (raw == "1") return "In";
                            if (raw == "2") return "Out";
                            return raw;
                        case "DISPSTATUS":
                            return raw == "1" ? "Disabled" : raw == "0" ? "Enabled" : raw;
                        default:
                            return raw;
                    }
                }

                string Friendly(string field)
                {
                    if (string.IsNullOrWhiteSpace(field)) return field;
                    var f = field.Trim();
                    switch (f.ToUpperInvariant())
                    {
                        case "VTDATE": return "Date";
                        case "VTTIME": return "Time";
                        case "VTNO": return "Ticket No";
                        case "VTDNO": return "Ticket Detail No";
                        case "VHLNO": return "Vehicle No";
                        case "DRVNAME": return "Driver Name";
                        case "VTDESC": return "Description";
                        case "VTQTY": return "NOP";
                        case "VTTYPE": return "Type";
                        case "VTSTYPE": return "Status Type";
                        case "VTREMRKS": return "Remarks";
                        case "EBNDID": return "ExBond ID";
                        case "CONTNRSID": return "Container Size";
                        case "EBVTNOC": return "No.of Containers";
                        case "VTAREA": return "Space";
                        case "DISPSTATUS": return "Status";
                        default: return field;
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
            catch { /* Best-effort mapping for compare page */ }

            ViewBag.Module = "ExBondVehicleTicket";
            ViewBag.VTDID = vtdid.Value;
            ViewBag.VersionA = versionA;
            ViewBag.VersionB = versionB;
            ViewBag.RowsA = rowsA;
            ViewBag.RowsB = rowsB;

            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        // ========================= Edit Logging Helper Methods =========================
        private void LogExBondVehicleTicketEdits(ExBondVehicleTicket before, ExBondVehicleTicket after, string userId)
        {
            if (before == null || after == null)
            {
                System.Diagnostics.Debug.WriteLine($"LogExBondVehicleTicketEdits: before={before != null}, after={after != null}");
                return;
            }
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine("LogExBondVehicleTicketEdits: No SCFSERP_EditLog connection string found");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(after.VTDNO))
            {
                System.Diagnostics.Debug.WriteLine($"LogExBondVehicleTicketEdits: VTDNO is null or empty for VTDID={after.VTDID}");
                return;
            }

            // Exclude system or noisy fields
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "VTDID", "PRCSDATE", "LMUSRID", "CUSRID",
                "COMPYID", "SDPTID"
            };

            // Compute the next version
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
                    WHERE [GIDNO] = @VTDNO AND [Modules] = 'ExBondVehicleTicket'", sql))
                {
                    cmd.Parameters.AddWithValue("@VTDNO", after.VTDNO);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        nextVersion = Convert.ToInt32(obj);
                }
            }
            catch { }

            var props = typeof(ExBondVehicleTicket).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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

                var versionLabel = $"V{nextVersion}-{after.VTDNO}";
                InsertEditLogRow(cs.ConnectionString, after.VTDNO, p.Name, os, ns, userId, versionLabel, "ExBondVehicleTicket");
            }
        }

        private void EnsureBaselineVersionZero(ExBondVehicleTicket snapshot, string userId)
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
                if (string.IsNullOrWhiteSpace(snapshot.VTDNO)) return;

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[GateInDetailEditLog] WHERE [GIDNO]=@VTDNO AND [Modules]='ExBondVehicleTicket' AND (RTRIM(LTRIM([Version]))=@VLower OR RTRIM(LTRIM([Version]))=@VUpper OR RTRIM(LTRIM([Version]))='0' OR RTRIM(LTRIM([Version]))='V0')", sql))
                {
                    cmd.Parameters.AddWithValue("@VTDNO", snapshot.VTDNO);
                    var baselineVerLower = "v0-" + snapshot.VTDNO;
                    var baselineVerUpper = "V0-" + snapshot.VTDNO;
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

        private void InsertBaselineSnapshot(ExBondVehicleTicket snapshot, string userId)
        {
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
            if (string.IsNullOrWhiteSpace(snapshot.VTDNO)) return;
            var baselineVer = "v0-" + snapshot.VTDNO;

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "VTDID", "PRCSDATE", "LMUSRID", "CUSRID", "COMPYID", "SDPTID"
            };

            var props = typeof(ExBondVehicleTicket).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                if (p.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
                {
                    var baseName = p.Name.Substring(0, p.Name.Length - 2);
                    var nameProp = props.FirstOrDefault(q => q.PropertyType == typeof(string) && (
                        q.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase) ||
                        q.Name.Equals(baseName + "NAME", StringComparison.OrdinalIgnoreCase)
                    ));
                    if (nameProp != null) continue;
                }

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
                else if (type == typeof(double) || type == typeof(float))
                {
                    var d = Convert.ToDouble(valObj ?? 0.0);
                    if (Math.Abs(d) < 1e-9) continue;
                }

                var newVal = FormatValForLogging(p.Name, valObj);
                InsertEditLogRow(cs.ConnectionString, snapshot.VTDNO, p.Name, null, newVal, userId, baselineVer, "ExBondVehicleTicket");
            }
        }

        private string FormatValForLogging(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            if (string.IsNullOrEmpty(formattedValue)) return formattedValue;

            try
            {
                if (fieldName.Equals("CONTNRSID", StringComparison.OrdinalIgnoreCase))
                {
                    int containerSizeId;
                    if (int.TryParse(formattedValue, out containerSizeId) && containerSizeId > 0)
                    {
                        var containerSize = context.containersizemasters.FirstOrDefault(c => c.CONTNRSID == containerSizeId && c.DISPSTATUS == 0);
                        if (containerSize != null && !string.IsNullOrEmpty(containerSize.CONTNRSDESC))
                            return containerSize.CONTNRSDESC;
                    }
                }
                else if (fieldName.Equals("VTTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "1") return "Empty";
                    if (formattedValue == "2") return "Loaded";
                }
                else if (fieldName.Equals("VTSTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "1") return "In";
                    if (formattedValue == "2") return "Out";
                }
                else if (fieldName.Equals("DISPSTATUS", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "1") return "Disabled";
                    if (formattedValue == "0") return "Enabled";
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

        private static void InsertEditLogRow(string connectionString, string vtdno, string fieldName, string oldValue, string newValue, string changedBy, string versionLabel, string modules)
        {
            try
            {
                using (var sql = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(@"
                    INSERT INTO [dbo].[GateInDetailEditLog] ([GIDNO], [FieldName], [OldValue], [NewValue], [ChangedBy], [ChangedOn], [Version], [Modules])
                    VALUES (@GIDNO, @FieldName, @OldValue, @NewValue, @ChangedBy, GETDATE(), @Version, @Modules)", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", vtdno);
                    cmd.Parameters.AddWithValue("@FieldName", fieldName);
                    cmd.Parameters.AddWithValue("@OldValue", (object)oldValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", (object)newValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Version", (object)versionLabel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Modules", modules ?? string.Empty);
                    sql.Open();
                    cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"InsertEditLogRow: Successfully inserted log for GIDNO={vtdno}, Field={fieldName}, Version={versionLabel}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InsertEditLogRow: Failed to insert log - GIDNO={vtdno}, Field={fieldName}, Error={ex.Message}");
            }
        }
        #endregion

    }
}