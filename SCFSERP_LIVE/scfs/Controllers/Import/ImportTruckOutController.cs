using scfs.Data;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using scfs_erp.Context;
using scfs_erp.Helper;
using scfs_erp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using System.Data.Entity;

namespace scfs_erp.Controllers.Import
{
    [SessionExpire]
    public class ImportTruckOutController : Controller
    {
        // GET: ImportTruckOut

        #region Context declaration
        SCFSERPContext context = new SCFSERPContext();
        public static String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
        SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);
        #endregion

        #region Index Screen
        [Authorize(Roles = "ImportTruckOutTOIndex")]
        public ActionResult TOIndex()
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
            DateTime sd = Convert.ToDateTime(System.Web.HttpContext.Current.Session["SDATE"]).Date;
            DateTime ed = Convert.ToDateTime(System.Web.HttpContext.Current.Session["EDATE"]).Date;
            return View();
        }
        #endregion
                    

        #region GetAjaxData
        public JsonResult TOGetAjaxData(JQueryDataTableParamModel param)/*model 22.edmx*/
        {
            using (var e = new CFSImportEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                var data = e.pr_Search_Import_TruckOut(param.sSearch, Convert.ToInt32(Request["iSortCol_0"]), Request["sSortDir_0"], param.iDisplayStart, param.iDisplayStart + param.iDisplayLength,
                    totalRowsCount, filteredRowsCount, Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]), Convert.ToInt32(Session["compyid"]));
                var aaData = data.Select(d => new string[] { d.GODATE.Value.ToString("dd/MM/yyyy"), d.GOTIME.Value.ToString("hh:mm tt"), d.GODNO.ToString(), d.VHLNO, d.GODID.ToString() }).ToArray();
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

        #region Edit Form
        [Authorize(Roles = "ImportTruckOutEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/ImportTruckOut/Form/" + id);
        }
        #endregion

        #region View Form
        [Authorize(Roles = "ImportTruckOutEdit")]
        public void TIEdit(int id)
        {
            Response.Redirect("/ImportTruckOut/TIForm/" + id);
        }
        #endregion

        #region Truck Out Form

        [Authorize(Roles = "ImportTruckOutForm")]
        public ActionResult TIForm(int? id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            GateInDetail tab = new GateInDetail();
            
            if (id != 0)//Edit Mode
            {

                //  var query = context.Database.SqlQuery<VW_EXPORT_GATEOUT_MOD_ASSGN>("select * from VW_EXPORT_GATEOUT_MOD_ASSGN where GODID=" + id).ToList();
                tab = context.gateindetails.Find(id);

                var query = context.Database.SqlQuery<GateInDetail>("select * from GATEINDETAIL where GIDID=" + tab.GIDID).ToList();
                if (query.Count > 0)
                {
                    ViewBag.GIDATE = query[0].GIDATE.ToString("dd/MM/yyyy");
                    ViewBag.STMRNAME = query[0].STMRNAME;
                    ViewBag.IMPRTNAME = query[0].IMPRTNAME;
                }
              




            }
            return View(tab);
        }

        #endregion

        #region Truck Out Form
        [Authorize(Roles = "ImportTruckOutForm")]
        public ActionResult Form(int? id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            GateOutDetail tab = new GateOutDetail();
           
            tab.GODID = 0;
            tab.GODATE = DateTime.Now;
            tab.GOTIME = DateTime.Now;

            if (id != 0)//Edit Mode
            {

                //  var query = context.Database.SqlQuery<VW_EXPORT_GATEOUT_MOD_ASSGN>("select * from VW_EXPORT_GATEOUT_MOD_ASSGN where GODID=" + id).ToList();
                tab = context.gateoutdetail.Find(id);
                
                var query = context.Database.SqlQuery<GateInDetail>("select * from GATEINDETAIL where GIDID=" + tab.GIDID).ToList();
                if (query.Count > 0)
                {
                    ViewBag.GIDATE = query[0].GIDATE.ToString("dd/MM/yyyy");
                    ViewBag.STMRNAME = query[0].STMRNAME;
                    ViewBag.IMPRTNAME = query[0].IMPRTNAME;
                }
               

            }
            return View(tab);
        }

        #endregion

        public JsonResult AutoVehicle(string term)/*model2.edmx*/
        {

            var result = (from r in context.VW_IMPORT_GATEOUT_TRUCKNO_CBX_ASSGN
                          where r.AVHLNO.ToLower().Contains(term.ToLower())
                          select new { r.AVHLNO }).Distinct();                          
            return Json(result, JsonRequestBehavior.AllowGet);

        }
        public JsonResult Detail(string id)
        {

            var query = context.Database.SqlQuery<VW_IMPORT_EMPTYGATEIN_CONTAINER_DEATILS_ASSGN>("select TOP 1 * from VW_IMPORT_EMPTYGATEIN_CONTAINER_DEATILS_ASSGN where VHLNO = '" + Convert.ToString(id) + "' ").ToList();
            return Json(query, JsonRequestBehavior.AllowGet);

            //var query = context.Database.SqlQuery<VW_IMPORT_GATEOUT_CONTAINER_CBX_CHNG_ASSGN>("select * from VW_IMPORT_GATEOUT_CONTAINER_CBX_CHNG_ASSGN where GIDID=" + id).ToList();
            //return Json(query, JsonRequestBehavior.AllowGet);
        }

        #region Insert/Modify 
        public void savedata(GateOutDetail tab)
        {
            string todaydt = Convert.ToString(DateTime.Now);
            string todayd = Convert.ToString(DateTime.Now.Date);

            string userId = Session["CUSRID"]?.ToString() ?? "System";
            GateOutDetail before = null;

            // Capture before state for edit logging
            if (tab.GODID != 0)
            {
                before = context.gateoutdetail.AsNoTracking().FirstOrDefault(x => x.GODID == tab.GODID);
            }

            tab.COMPYID = Convert.ToInt32(Session["compyid"]);
            tab.SDPTID = 1;
            tab.REGSTRID = 3;
            tab.TRANDID = 0;
            tab.GOBTYPE = 1;
            tab.LSEALNO = null;
            tab.SSEALNO = null;
            if (tab.CUSRID == null || (tab.GODID).ToString() == "0")
                tab.CUSRID = Session["CUSRID"].ToString();
            tab.LMUSRID = Session["CUSRID"].ToString();
            tab.PRCSDATE = DateTime.Now;
            tab.EHIDATE = DateTime.Now;
            tab.EHITIME = DateTime.Now;

            string indate = Convert.ToString(tab.GODATE);
            if (indate != null || indate != "")
            {
                tab.GODATE = Convert.ToDateTime(indate).Date;
            }
            else { tab.GODATE = DateTime.Now.Date; }

            if (tab.GODATE > Convert.ToDateTime(todayd))
            {
                tab.GODATE = Convert.ToDateTime(todayd);
            }

            string intime = Convert.ToString(tab.GOTIME);
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

                        tab.GOTIME = Convert.ToDateTime(in_datetime);
                    }
                    else { tab.GOTIME = DateTime.Now; }
                }
                else { tab.GOTIME = DateTime.Now; }
            }
            else { tab.GOTIME = DateTime.Now; }

            if (tab.GOTIME > Convert.ToDateTime(todaydt))
            {
                tab.GOTIME = Convert.ToDateTime(todaydt);
            }

            //var ASLDID = Request.Form.Get("ASLDID");
            //var CSEALNO = Request.Form.Get("CSEAL");
            //var ASEALNO = Request.Form.Get("ASEAL");

            if ((tab.GODID).ToString() != "0")
            {
                context.Entry(tab).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();

                // Log changes after save
                try
                {
                    if (before != null)
                    {
                        EnsureBaselineVersionZero(before, userId);
                        LogTruckOutEdits(before, tab, userId);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error for debugging but don't fail the save
                    System.Diagnostics.Debug.WriteLine($"ImportTruckOut edit logging failed: {ex.Message}");
                }
            }
            else
            {
                tab.GONO = Convert.ToInt32(Autonumber.autonum("GateOutDetail", "GONO", "COMPYID=" + Convert.ToInt32(Session["compyid"]) + " and SDPTID=1").ToString());
                int ano = tab.GONO;
                string prfx = string.Format("{0:D5}", ano);
                tab.GODNO = ano.ToString();
                context.gateoutdetail.Add(tab);
                context.SaveChanges();

                // Ensure baseline version exists for new records
                try
                {
                    EnsureBaselineVersionZero(tab, userId);
                }
                catch (Exception ex)
                {
                    // Log the error for debugging but don't fail the save
                    System.Diagnostics.Debug.WriteLine($"ImportTruckOut baseline logging failed: {ex.Message}");
                }

                //AuthorizationSlipDetail ad = context.authorizationslipdetail.Find(Convert.ToInt32(ASLDID));
                //context.Entry(ad).Entity.CSEALNO = CSEALNO;
                //context.Entry(ad).Entity.ASEALNO = ASEALNO;
                //context.SaveChanges();


            }
            Response.Redirect("TOIndex");
            //Response.Redirect("Save");
        }
        #endregion

        //..........................Printview...
        [Authorize(Roles = "ImportTruckOutPrint")]
        public void PrintView(int? id = 0)
        {

            //  ........delete TMPRPT...//
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "IMPORTTRUCKOUT", Convert.ToInt32(id), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;



                // cryRpt.Load(Server.MapPath("~/") + "//Reports//RPT_0302.rpt");


                //........Get TRANPCOUNT...//
                //var Query = context.Database.SqlQuery<int>("select TRANPCOUNT from transactionmaster where TRANMID=" + id).ToList();
                //var PCNT = 0;

                //if (Query.Count() != 0) { PCNT = Query[0]; }
                //var TRANPCOUNT = ++PCNT;
                //// Response.Write(++PCNT);
                //// Response.End();

                //context.Database.ExecuteSqlCommand("UPDATE transactionmaster SET TRANPCOUNT=" + TRANPCOUNT + " WHERE TRANMID=" + id);


                cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "IMPORT_TruckOut.RPT");

                cryRpt.RecordSelectionFormula = "{VW_IMPORT_TRUCK_Out_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_IMPORT_TRUCK_Out_PRINT_ASSGN.GODID} = " + id;



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

        #region delete 
        //[Authorize(Roles = "ImportTruckOutDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");
            
            String temp = Delete_fun.delete_check1(fld, id);
            if (temp.Equals("PROCEED"))
            {

                GateOutDetail god = new GateOutDetail();
                god = context.gateoutdetail.Find(Convert.ToInt32(id));
                context.gateoutdetail.Remove(god);
                context.SaveChanges();

                Response.Write("Deleted Successfully ...");               

                
            }
            else
                Response.Write(temp);
        }
        #endregion

        // ========================= Edit Log Pages =========================
        #region Edit Log Pages
        public ActionResult EditLogTruckOut(int? godid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
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
                                    WHERE [Modules] = 'ImportTruckOut'";
                    
                    if (godid.HasValue)
                    {
                        // Find GODNO from GODID
                        var truckOutRecord = context.gateoutdetail.AsNoTracking().FirstOrDefault(x => x.GODID == godid.Value);
                        if (truckOutRecord != null && !string.IsNullOrEmpty(truckOutRecord.GODNO))
                        {
                            query += " AND [GIDNO] = @GODNO";
                        }
                        else
                        {
                            query += " AND CAST([GIDNO] AS INT) = @GODID";
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
                        if (godid.HasValue)
                        {
                            var truckOutRecord = context.gateoutdetail.AsNoTracking().FirstOrDefault(x => x.GODID == godid.Value);
                            if (truckOutRecord != null && !string.IsNullOrEmpty(truckOutRecord.GODNO))
                            {
                                cmd.Parameters.AddWithValue("@GODNO", truckOutRecord.GODNO);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@GODID", godid.Value);
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
                    switch (field.ToUpper())
                    {
                        case "GODATE": return "Gate Out Date";
                        case "GOTIME": return "Gate Out Time";
                        case "GODNO": return "Gate Out Number";
                        case "VHLNO": return "Vehicle Number";
                        case "GDRVNAME": return "Driver Name";
                        case "CHASNAME": return "CHA Name";
                        case "CHAID": return "CHA";
                        case "GIDID": return "Gate In Detail";
                        case "GOREMRKS": return "Remarks";
                        case "GOQTY": return "Quantity";
                        case "GOPLCNAME": return "Place Name";
                        case "GOBKGNO": return "Booking Number";
                        case "GOEGMNO": return "EGM Number";
                        case "OVSLNAME": return "Vessel Name";
                        case "OVOYNO": return "Voyage Number";
                        case "EBLNO": return "E-BL Number";
                        case "LSEALNO": return "Liner Seal Number";
                        case "SSEALNO": return "SANCO Seal Number";
                        case "GOTTYPE": return "Gate Out T Type";
                        case "GOOTYPE": return "Gate Out O Type";
                        case "GOCTYPE": return "Gate Out C Type";
                        case "DISPSTATUS": return "Status";
                        case "ESSBLNO": return "ES SB L Number";
                        case "ESSBLDT": return "ES SB L Date";
                        case "ESINVNO": return "ES Invoice Number";
                        case "ESINVDT": return "ES Invoice Date";
                        case "ESTOPLACE": return "ES Top Place";
                        case "ESPORT": return "ES Port";
                        case "GOTRNSPRTNAME": return "Transport Name";
                        case "OTDNO": return "Out Type Detail Number";
                        case "OVSLID": return "Out Vessel ID";
                        default: return field;
                    }
                };

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrEmpty(val)) return val;
                    try
                    {
                        if (field.Equals("CHAID", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(val, out int chaid) && chaid > 0)
                            {
                                var cha = context.categorymasters.FirstOrDefault(c => c.CATETID == 4 && c.CATEID == chaid && c.DISPSTATUS == 0);
                                if (cha != null && !string.IsNullOrEmpty(cha.CATENAME))
                                    return cha.CATENAME;
                            }
                        }
                        else if (field.Equals("DISPSTATUS", StringComparison.OrdinalIgnoreCase))
                        {
                            if (val == "1") return "CANCELLED";
                            if (val == "0") return "INBOOKS";
                        }
                    }
                    catch { }
                    return val;
                };

                // Filter out EHI Date and EHI Time fields before processing
                list = list.Where(row => {
                    if (row.FieldName == null) return true;
                    var fieldNameLocal = row.FieldName.Trim();
                    // Skip EHIDATE and EHITIME fields
                    if (fieldNameLocal.Equals("EHIDATE", StringComparison.OrdinalIgnoreCase) || 
                        fieldNameLocal.Equals("EHITIME", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("EHI Date", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("EHI Time", StringComparison.OrdinalIgnoreCase))
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

            ViewBag.GODID = godid;
            ViewBag.Module = "ImportTruckOut";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        // Compare two versions for a given GODNO
        public ActionResult EditLogTruckOutCompare(int? godid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            // Handle tab characters and other whitespace in parameters
            if (godid == null)
            {
                // Try to parse from query string if godid is null
                var godidStr = Request["godid"];
                if (!string.IsNullOrWhiteSpace(godidStr))
                {
                    var cleaned = godidStr.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "");
                    int tmp;
                    if (int.TryParse(cleaned, out tmp))
                    {
                        godid = tmp;
                    }
                }
            }

            // Normalize version strings (remove tabs and other whitespace)
            if (versionA != null)
            {
                versionA = versionA.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "");
            }
            if (versionB != null)
            {
                versionB = versionB.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "");
            }

            if (godid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide GODID, Version A and Version B to compare.";
                return RedirectToAction("EditLogTruckOut", new { godid = godid });
            }

            // Get actual GODNO string from database to handle leading zeros and proper matching
            var truckOutRecord = context.gateoutdetail.AsNoTracking().FirstOrDefault(x => x.GODID == godid.Value);
            string godno = truckOutRecord?.GODNO ?? godid.Value.ToString();
            
            // Normalize version strings (trim whitespace) and support baseline shortcuts
            // Map '0' or 'v0'/'V0' to 'v0-<GODNO>' for baseline comparisons
            if (godid.HasValue)
            {
                var baseLabel = "v0-" + godno;
                if (string.Equals(versionA, "0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionA, "V0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionA, "v0", StringComparison.OrdinalIgnoreCase))
                {
                    versionA = baseLabel;
                }
                if (string.Equals(versionB, "0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionB, "V0", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(versionB, "v0", StringComparison.OrdinalIgnoreCase))
                {
                    versionB = baseLabel;
                }
            }
            
            // Final trim to ensure no whitespace
            versionA = (versionA ?? string.Empty).Trim();
            versionB = (versionB ?? string.Empty).Trim();

            var rowsA = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var rowsB = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];

            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                // Use the same pattern as ImportGateInCompare - two separate queries with RTRIM(LTRIM) for version matching
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    
                    // Query for Version A
                    using (var cmd = new SqlCommand(@"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                    FROM [dbo].[GateInDetailEditLog]
                                                    WHERE [Modules] = 'ImportTruckOut' 
                                                      AND (CAST([GIDNO] AS VARCHAR)=@GODNO_STR OR [GIDNO]=@GODNO) 
                                                      AND LOWER(RTRIM(LTRIM([Version])))=LOWER(@V)", sql))
                    {
                        cmd.Parameters.Add("@GODNO", System.Data.SqlDbType.Int);
                        cmd.Parameters.Add("@GODNO_STR", System.Data.SqlDbType.NVarChar, 50);
                        cmd.Parameters.Add("@V", System.Data.SqlDbType.NVarChar, 100);
                        
                        cmd.Parameters["@GODNO"].Value = godid.Value;
                        cmd.Parameters["@GODNO_STR"].Value = godno;
                        cmd.Parameters["@V"].Value = versionA.Trim();
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rowsA.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = godno,
                                    FieldName = reader["FieldName"] == DBNull.Value ? null : Convert.ToString(reader["FieldName"]),
                                    OldValue = reader["OldValue"] == DBNull.Value ? null : Convert.ToString(reader["OldValue"]),
                                    NewValue = reader["NewValue"] == DBNull.Value ? null : Convert.ToString(reader["NewValue"]),
                                    ChangedBy = reader["ChangedBy"] == DBNull.Value ? null : Convert.ToString(reader["ChangedBy"]),
                                    ChangedOn = reader["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(reader["ChangedOn"]) : DateTime.MinValue,
                                    Version = versionA,
                                    Modules = reader["Modules"] == DBNull.Value ? null : Convert.ToString(reader["Modules"])
                                });
                            }
                        }
                    }

                    // Query for Version B
                    using (var cmd = new SqlCommand(@"SELECT [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                    FROM [dbo].[GateInDetailEditLog]
                                                    WHERE [Modules] = 'ImportTruckOut' 
                                                      AND (CAST([GIDNO] AS VARCHAR)=@GODNO_STR OR [GIDNO]=@GODNO) 
                                                      AND LOWER(RTRIM(LTRIM([Version])))=LOWER(@V)", sql))
                    {
                        cmd.Parameters.Add("@GODNO", System.Data.SqlDbType.Int);
                        cmd.Parameters.Add("@GODNO_STR", System.Data.SqlDbType.NVarChar, 50);
                        cmd.Parameters.Add("@V", System.Data.SqlDbType.NVarChar, 100);
                        
                        cmd.Parameters["@GODNO"].Value = godid.Value;
                        cmd.Parameters["@GODNO_STR"].Value = godno;
                        cmd.Parameters["@V"].Value = versionB.Trim();
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rowsB.Add(new scfs_erp.Models.GateInDetailEditLogRow
                                {
                                    GIDNO = godno,
                                    FieldName = reader["FieldName"] == DBNull.Value ? null : Convert.ToString(reader["FieldName"]),
                                    OldValue = reader["OldValue"] == DBNull.Value ? null : Convert.ToString(reader["OldValue"]),
                                    NewValue = reader["NewValue"] == DBNull.Value ? null : Convert.ToString(reader["NewValue"]),
                                    ChangedBy = reader["ChangedBy"] == DBNull.Value ? null : Convert.ToString(reader["ChangedBy"]),
                                    ChangedOn = reader["ChangedOn"] != DBNull.Value ? Convert.ToDateTime(reader["ChangedOn"]) : DateTime.MinValue,
                                    Version = versionB,
                                    Modules = reader["Modules"] == DBNull.Value ? null : Convert.ToString(reader["Modules"])
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
                    switch (field.ToUpper())
                    {
                        case "GODATE": return "Gate Out Date";
                        case "GOTIME": return "Gate Out Time";
                        case "GODNO": return "Gate Out Number";
                        case "VHLNO": return "Vehicle Number";
                        case "GDRVNAME": return "Driver Name";
                        case "CHASNAME": return "CHA Name";
                        case "CHAID": return "CHA";
                        case "GIDID": return "Gate In Detail";
                        case "GOREMRKS": return "Remarks";
                        case "GOQTY": return "Quantity";
                        case "GOPLCNAME": return "Place Name";
                        case "GOBKGNO": return "Booking Number";
                        case "GOEGMNO": return "EGM Number";
                        case "OVSLNAME": return "Vessel Name";
                        case "OVOYNO": return "Voyage Number";
                        case "EBLNO": return "E-BL Number";
                        case "LSEALNO": return "Liner Seal Number";
                        case "SSEALNO": return "SANCO Seal Number";
                        case "GOTTYPE": return "Gate Out T Type";
                        case "GOOTYPE": return "Gate Out O Type";
                        case "GOCTYPE": return "Gate Out C Type";
                        case "DISPSTATUS": return "Status";
                        case "ESSBLNO": return "ES SB L Number";
                        case "ESSBLDT": return "ES SB L Date";
                        case "ESINVNO": return "ES Invoice Number";
                        case "ESINVDT": return "ES Invoice Date";
                        case "ESTOPLACE": return "ES Top Place";
                        case "ESPORT": return "ES Port";
                        case "GOTRNSPRTNAME": return "Transport Name";
                        case "OTDNO": return "Out Type Detail Number";
                        case "OVSLID": return "Out Vessel ID";
                        default: return field;
                    }
                };

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrEmpty(val)) return val;
                    try
                    {
                        if (field.Equals("CHAID", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(val, out int chaid) && chaid > 0)
                            {
                                var cha = context.categorymasters.FirstOrDefault(c => c.CATETID == 4 && c.CATEID == chaid && c.DISPSTATUS == 0);
                                if (cha != null && !string.IsNullOrEmpty(cha.CATENAME))
                                    return cha.CATENAME;
                            }
                        }
                        else if (field.Equals("DISPSTATUS", StringComparison.OrdinalIgnoreCase))
                        {
                            if (val == "1") return "CANCELLED";
                            if (val == "0") return "INBOOKS";
                        }
                    }
                    catch { }
                    return val;
                };

                // Filter out EHI Date and EHI Time fields before processing
                rowsA = rowsA.Where(row => {
                    if (row.FieldName == null) return true;
                    var fieldNameLocal = row.FieldName.Trim();
                    // Skip EHIDATE and EHITIME fields
                    if (fieldNameLocal.Equals("EHIDATE", StringComparison.OrdinalIgnoreCase) || 
                        fieldNameLocal.Equals("EHITIME", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("EHI Date", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("EHI Time", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }).ToList();
                
                rowsB = rowsB.Where(row => {
                    if (row.FieldName == null) return true;
                    var fieldNameLocal = row.FieldName.Trim();
                    // Skip EHIDATE and EHITIME fields
                    if (fieldNameLocal.Equals("EHIDATE", StringComparison.OrdinalIgnoreCase) || 
                        fieldNameLocal.Equals("EHITIME", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("EHI Date", StringComparison.OrdinalIgnoreCase) ||
                        fieldNameLocal.Equals("EHI Time", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }).ToList();

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

            ViewBag.GIDNO = godid.Value;
            ViewBag.VersionA = versionA.Trim();
            ViewBag.VersionB = versionB.Trim();
            ViewBag.RowsA = rowsA;
            ViewBag.RowsB = rowsB;
            ViewBag.Module = "ImportTruckOut";

            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        private void LogTruckOutEdits(GateOutDetail before, GateOutDetail after, string userId)
        {
            if (before == null || after == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            // Exclude only truly system/internal fields
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GODID",  // Primary key
                "GONO",   // Internal number (but GODNO is logged)
                "PRCSDATE", // System timestamp
                "LMUSRID",  // Audit field
                "CUSRID",   // Audit field
                "COMPYID",  // System field
                "SDPTID",   // System field
                "REGSTRID", // System field
                "TRANDID",  // System field
                "GOBTYPE",  // Always 1 for Truck Out
                "EHIDATE",  // EHI Date - removed from display
                "EHITIME"   // EHI Time - removed from display
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
                    WHERE [GIDNO] = @GODNO AND [Modules] = 'ImportTruckOut'", sql))
                {
                    cmd.Parameters.AddWithValue("@GODNO", after.GODNO ?? after.GODID.ToString());
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        nextVersion = Convert.ToInt32(obj);
                }
            }
            catch { }

            var props = typeof(GateOutDetail).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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

                var versionLabel = $"V{nextVersion}-{after.GODNO ?? after.GODID.ToString()}";
                InsertEditLogRow(cs.ConnectionString, after.GODNO ?? after.GODID.ToString(), p.Name, os, ns, userId, versionLabel, "ImportTruckOut");
            }
        }

        private void EnsureBaselineVersionZero(GateOutDetail snapshot, string userId)
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
                string godno = snapshot.GODNO ?? snapshot.GODID.ToString();
                if (string.IsNullOrWhiteSpace(godno)) return;

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[GateInDetailEditLog] WHERE [GIDNO]=@GODNO AND [Modules]='ImportTruckOut' AND (RTRIM(LTRIM([Version]))=@VLower OR RTRIM(LTRIM([Version]))=@VUpper OR RTRIM(LTRIM([Version]))='0' OR RTRIM(LTRIM([Version]))='V0')", sql))
                {
                    cmd.Parameters.AddWithValue("@GODNO", godno);
                    var baselineVerLower = "v0-" + godno;
                    var baselineVerUpper = "V0-" + godno;
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

        private void InsertBaselineSnapshot(GateOutDetail snapshot, string userId)
        {
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
            string godno = snapshot.GODNO ?? snapshot.GODID.ToString();
            if (string.IsNullOrWhiteSpace(godno)) return;
            var baselineVer = "v0-" + godno;

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GODID",  // Primary key
                "GONO",   // Internal number
                "PRCSDATE", // System timestamp
                "LMUSRID",  // Audit field
                "CUSRID",   // Audit field
                "COMPYID",  // System field
                "SDPTID",   // System field
                "REGSTRID", // System field
                "TRANDID",  // System field
                "GOBTYPE",  // Always 1
                "EHIDATE",  // EHI Date - removed from display
                "EHITIME"   // EHI Time - removed from display
            };

            var props = typeof(GateOutDetail).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                var valObj = p.GetValue(snapshot, null);
                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                // Log all fields except truly empty ones
                if (type == typeof(string))
                {
                    var s = (Convert.ToString(valObj) ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(s)) continue;
                }

                var newVal = FormatValForLogging(p.Name, valObj);
                InsertEditLogRow(cs.ConnectionString, godno, p.Name, null, newVal, userId, baselineVer, "ImportTruckOut");
            }
        }

        private string FormatValForLogging(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            if (string.IsNullOrEmpty(formattedValue)) return formattedValue;

            try
            {
                int lookupId;
                if (fieldName.Equals("CHAID", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(formattedValue, out lookupId) && lookupId > 0)
                    {
                        var cha = context.categorymasters.FirstOrDefault(c => c.CATETID == 4 && c.CATEID == lookupId && c.DISPSTATUS == 0);
                        if (cha != null && !string.IsNullOrEmpty(cha.CATENAME))
                            return cha.CATENAME;
                    }
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

        private static void InsertEditLogRow(string connectionString, string godno, string fieldName, string oldValue, string newValue, string changedBy, string versionLabel, string modules)
        {
            try
            {
                using (var sql = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(@"
                    INSERT INTO [dbo].[GateInDetailEditLog] ([GIDNO], [FieldName], [OldValue], [NewValue], [ChangedBy], [ChangedOn], [Version], [Modules])
                    VALUES (@GIDNO, @FieldName, @OldValue, @NewValue, @ChangedBy, GETDATE(), @Version, @Modules)", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", godno);
                    cmd.Parameters.AddWithValue("@FieldName", fieldName);
                    cmd.Parameters.AddWithValue("@OldValue", (object)oldValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NewValue", (object)newValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Version", versionLabel);
                    cmd.Parameters.AddWithValue("@Modules", modules);
                    sql.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
        #endregion
        //end
    }
}