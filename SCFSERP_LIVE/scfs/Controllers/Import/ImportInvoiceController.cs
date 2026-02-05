using scfs.Data;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using scfs_erp.Context;
using scfs_erp.Helper;
using scfs_erp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Data;
using ClosedXML.Excel;
using System.Data.Entity;
using System.Reflection;

namespace scfs_erp.Controllers.Import
{
    [SessionExpire]
    public class ImportInvoiceController : Controller
    {
        //
        // GET: /ImportInvoice/
        SCFSERPContext context = new SCFSERPContext();
        public static String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
        SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);
        [Authorize(Roles = "ImportInvoiceIndex")]
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
            if (Request.Form.Get("TRANBTYPE") != null)
            {
                Session["TRANBTYPE"] = Request.Form.Get("TRANBTYPE");
                Session["REGSTRID"] = Request.Form.Get("REGSTRID");
            }
            else
            {
                Session["TRANBTYPE"] = "1";
                Session["REGSTRID"] = "1";
            }


            //...........Bill type......//
            List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
            if (Convert.ToInt32(Session["TRANBTYPE"]) == 1)
            {
                SelectListItem selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
                selectedBILLYPE.Add(selectedItemGPTY);
                selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
                selectedBILLYPE.Add(selectedItemGPTY);
                //selectedItemGPTY = new SelectListItem { Text = "SEZ", Value = "6", Selected = false };
                //selectedBILLYPE.Add(selectedItemGPTY);
            }
            else if (Convert.ToInt32(Session["TRANBTYPE"]) == 2)
            {
                SelectListItem selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = false };
                selectedBILLYPE.Add(selectedItemGPTY);
                selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = true };
                selectedBILLYPE.Add(selectedItemGPTY);
                //selectedItemGPTY = new SelectListItem { Text = "SEZ", Value = "6", Selected = false };
                //selectedBILLYPE.Add(selectedItemGPTY);
            }
            else //if (Convert.ToInt32(Session["TRANBTYPE"]) == 6)
            {
                SelectListItem selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = false };
                selectedBILLYPE.Add(selectedItemGPTY);
                selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
                selectedBILLYPE.Add(selectedItemGPTY);
                //selectedItemGPTY = new SelectListItem { Text = "SEZ", Value = "6", Selected = true };
                //selectedBILLYPE.Add(selectedItemGPTY);
            }
            ViewBag.TRANBTYPE = selectedBILLYPE;
            //....end

            //............Billed to....//
            ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == 1 || x.REGSTRID == 2 || x.REGSTRID == 6 || x.REGSTRID == 70), "REGSTRID", "REGSTRDESC", Convert.ToInt32(Session["REGSTRID"]));
            //.....end


            DateTime sd = Convert.ToDateTime(System.Web.HttpContext.Current.Session["SDATE"]).Date;

            DateTime ed = Convert.ToDateTime(System.Web.HttpContext.Current.Session["EDATE"]).Date;
            return View();
            // return View(context.transactionmaster.Where(x => x.TRANDATE >= sd).Where(x => x.TRANDATE <= ed).ToList());
        }//...End of index grid

        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            using (var e = new CFSImportEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));
                var tranbtype = Convert.ToInt32(Session["TRANBTYPE"]);
                var regstrid = Convert.ToInt32(Session["REGSTRID"]);
                var data = e.pr_Search_Import_Invoice(param.sSearch, Convert.ToInt32(Request["iSortCol_0"]), Request["sSortDir_0"], param.iDisplayStart, param.iDisplayStart + param.iDisplayLength,
                    totalRowsCount, filteredRowsCount, Convert.ToInt32(Session["compyid"]), Convert.ToInt32(tranbtype), Convert.ToInt32(regstrid), Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]));
                var aaData = data.Select(d => new string[] { d.TRANDATE.Value.ToString("dd/MM/yyyy"), d.TRANTIME.Value.ToString("hh:mm tt"), d.TRANDNO.ToString(), d.TRANREFNAME, d.TRANNAMT.ToString(), d.GSTAMT.ToString(), d.DISPSTATUS, d.ACKNO, d.dono.ToString(), d.TRANMID.ToString() }).ToArray();
                return Json(new
                {
                    sEcho = param.sEcho,
                    aaData = aaData,
                    iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                    iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "ImportInvoiceIndex")]
        public ActionResult ImpBillBoSRptXL()
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
            if (Request.Form.Get("TRANBTYPE") != null)
            {
                Session["TRANBTYPE"] = Request.Form.Get("TRANBTYPE");
                Session["REGSTRID"] = Request.Form.Get("REGSTRID");
            }
            else
            {
                Session["TRANBTYPE"] = "1";
                Session["REGSTRID"] = "1";
            }


            //...........Bill type......//
            var mtqry = context.Database.SqlQuery<pr_get_BondMaster_Status_Result>("exec PR_GET_REPORT_TRANBTYPE ").ToList();
            ViewBag.TRANBTYPE = new SelectList(mtqry, "dval", "dtxt").ToList();

            //...........Billed To......//
            mtqry = context.Database.SqlQuery<pr_get_BondMaster_Status_Result>("exec PR_GET_REPORT_IMPORT_REGISTER ").ToList();
            ViewBag.REGSTRID = new SelectList(mtqry, "dval", "dtxt").ToList();
            
            

            DateTime sd = Convert.ToDateTime(System.Web.HttpContext.Current.Session["SDATE"]).Date;

            DateTime ed = Convert.ToDateTime(System.Web.HttpContext.Current.Session["EDATE"]).Date;
            return View();
            // return View(context.transactionmaster.Where(x => x.TRANDATE >= sd).Where(x => x.TRANDATE <= ed).ToList());
        }//...End of index grid

        public void GenerateBOSXLReport()
        {
            Session["SDATE"] = Request.Form.Get("from");
            Session["EDATE"] = Request.Form.Get("to");
            Session["TRANBTYPE"] = Request.Form.Get("TRANBTYPE");
            Session["REGSTRID"] = Request.Form.Get("REGSTRID");

            int compyid = Convert.ToInt32(Session["compyid"]);
            string filename = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            string fdt = Convert.ToDateTime(Session["SDATE"]).ToString("yyyy-MM-dd");
            string tdt = Convert.ToDateTime(Session["EDATE"]).ToString("yyyy-MM-dd");
            filename = filename + ".xlsx";
            string query = "";
            int trnbtyp = Convert.ToInt32(Session["TRANBTYPE"]);
            int regid = Convert.ToInt32(Session["REGSTRID"]);
            
            
            query = "exec pr_Report_Import_Invoice_Detail @PCompyId = " + compyid + ",@PTRANBTYPE=" + trnbtyp + "";
            query = query + ", @PREGSTRID=" + regid + "";
            query = query + ", @PSDate='" + fdt + "'";
            query = query + ", @PEDate='" + tdt + "'";
            DataTable dt = new DataTable();
            
            String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
            SqlConnection con = new SqlConnection(constring);

            con.Open();
            SqlDataAdapter da = new SqlDataAdapter(query, con);
            da.Fill(dt);
            con.Close();
            



            using (XLWorkbook wb = new XLWorkbook())
            {
                dt.TableName = "Import_Bill_BoS_Details";
                wb.Worksheets.Add(dt);
                

                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;
                wb.Style.Border = null;

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename= " + filename);// Collection_Details.xlsx");

                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    //Response.End();
                }
            }
        }
        [Authorize(Roles = "ImportInvoiceEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/ImportInvoice/GSTForm/" + id);
        }
        [Authorize(Roles = "ImportInvoiceEdit")]
        public void DOEdit(int id)
        {
            Response.Redirect("/ImportInvoice/DOGSTForm/" + id);
        }
        //......................Form data....................//
        [Authorize(Roles = "ImportInvoiceCreate")]
        public ActionResult Form(int id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            TransactionMaster tab = new TransactionMaster();
            TransactionMD vm = new TransactionMD();

            //..........................................Dropdown data.........................//
            ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            // ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
            ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.SBMDATE = DateTime.Now;
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");
            ViewBag.TRANLMID = new SelectList("");
            ViewBag.TRANOTYPE = new SelectList(context.ImportDestuffSlipOperation, "OPRTYPE", "OPRTYPEDESC");
            ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC");

            ViewBag.TARIFFMID = new SelectList("");
            //...........Bill type......//
            List<SelectListItem> selectedBILLTYPE = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
            selectedBILLTYPE.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
            selectedBILLTYPE.Add(selectedItemDSP);
            ViewBag.TRANBTYPE = selectedBILLTYPE;
            //....end

            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst = new List<SelectListItem>();
            SelectListItem selectedItemtax = new SelectListItem { Text = "YES", Value = "1", Selected = true };
            selectedtaxlst.Add(selectedItemtax);
            selectedItemtax = new SelectListItem { Text = "NO", Value = "0", Selected = false };
            selectedtaxlst.Add(selectedItemtax);
            ViewBag.STAX = selectedtaxlst;

            ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == 1 || x.REGSTRID == 2 || x.REGSTRID == 6), "REGSTRID", "REGSTRDESC");
            //.....end

            //........display status.........//
            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDISP = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDISP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;
            //....end

            if (id != 0)//....Edit Mode
            {
                tab = context.transactionmaster.Find(id);//find selected record

                //...................................Selected dropdown value..................................//
                ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME", tab.LCATEID);
                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", tab.BANKMID);
                ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", tab.TRANMODE);
                ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == tab.REGSTRID), "REGSTRID", "REGSTRDESC", tab.REGSTRID);


                //.................Display status.................//
                List<SelectListItem> selectedDISP = new List<SelectListItem>();
                if (Convert.ToInt32(tab.DISPSTATUS) == 1)
                {
                    SelectListItem selectedItemDIS = new SelectListItem { Text = "In Books", Value = "0", Selected = false };
                    selectedDISP.Add(selectedItemDIS);
                    selectedItemDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = true };
                    selectedDISP.Add(selectedItemDIS);
                    ViewBag.DISPSTATUS = selectedDISP;
                }
                else
                {
                    SelectListItem selectedItemDIS = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
                    selectedDISP.Add(selectedItemDIS);
                    selectedItemDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = false };
                    selectedDISP.Add(selectedItemDIS);
                    ViewBag.DISPSTATUS = selectedDISP;
                }//....end

                //....................Bill type.................//
                List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANBTYPE) == 1)
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    // selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
                    // selectedBILLYPE.Add(selectedItemGPTY);

                }
                else
                {

                    //selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = false };
                    //  selectedBILLYPE.Add(selectedItemGPTY);
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                ViewBag.TRANBTYPE = selectedBILLYPE;
                //..........end

                vm.masterdata = context.transactionmaster.Where(det => det.TRANMID == id).ToList();
                vm.detaildata = context.transactiondetail.Where(det => det.TRANMID == id).ToList();
                vm.costfactor = context.transactionmasterfactor.Where(det => det.TRANMID == id).ToList();
                vm.impinvcedata = context.Database.SqlQuery<pr_Import_Invoice_IGMNO_Grid_Assgn_Result>("EXEC pr_Import_Invoice_IGMNO_Grid_Assgn @PIGMNO='-',@PLNO='-',@PTRANMID=" + id + "").ToList();//........procedure  for edit mode details data

                var billemid = Convert.ToInt32(vm.impinvcedata[0].BILLEMID);
                var sealcnt = context.Database.SqlQuery<int>("select NOC from VW_IMPORT_BILL_OPEN_SHEET_SEAL_COUNT_ASSGN where BILLEMID=" + billemid).ToList();
                if (sealcnt.Count > 0)
                    ViewBag.NOC = sealcnt[0];
                else
                    ViewBag.NOC = 0;

                ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
                var tariffmid = Convert.ToInt32(vm.detaildata[0].TARIFFMID);
                var sql = context.Database.SqlQuery<int>("select TGID from TariffMaster where TARIFFMID=" + tariffmid).ToList();
                ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC", Convert.ToInt32(sql[0]));
            }

            return View(vm);
        }//........End of form


        // GST FORM DATA

        [Authorize(Roles = "ImportInvoiceCreate")]
        public ActionResult GSTForm(int id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            TransactionMaster tab = new TransactionMaster();
            TransactionMD vm = new TransactionMD();

            //..........................................Dropdown data.........................//
            ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            // ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
            ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.SBMDATE = DateTime.Now;
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");
            ViewBag.TRANLMID = new SelectList("");
           
            ViewBag.TRANOTYPE = new SelectList(context.import_slab_handling_type_masters, "HTYPE", "HTYPEDESC");
            //ViewBag.TRANOTYPE = new SelectList(context.ImportDestuffSlipOperation, "OPRTYPE", "OPRTYPEDESC");
            ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC");

            ViewBag.TARIFFMID = new SelectList("");
            //...........Bill type......//
            List<SelectListItem> selectedBILLTYPE = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
            selectedBILLTYPE.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
            selectedBILLTYPE.Add(selectedItemDSP);
            ViewBag.TRANBTYPE = selectedBILLTYPE;
            //....end

            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst = new List<SelectListItem>();
            SelectListItem selectedItemtax = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
            selectedtaxlst.Add(selectedItemtax);
            selectedItemtax = new SelectListItem { Text = "No", Value = "0", Selected = true };
            selectedtaxlst.Add(selectedItemtax);
            ViewBag.STAX = selectedtaxlst;

            //.........storage calc type...//
            List<SelectListItem> selectedscalclst = new List<SelectListItem>();
            SelectListItem selectedscalc = new SelectListItem { Text = "Yes", Value = "0", Selected = true };
            selectedscalclst.Add(selectedscalc);
            selectedscalc = new SelectListItem { Text = "No", Value = "1", Selected = false };
            selectedscalclst.Add(selectedscalc);
            ViewBag.SCALC = selectedscalclst;

            ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == 1 || x.REGSTRID == 2 || x.REGSTRID == 6 || x.REGSTRID == 70), "REGSTRID", "REGSTRDESC");
            //.....end

            //........display status.........//
            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDISP = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDISP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;
            //....end

            if (id != 0)//....Edit Mode
            {
                tab = context.transactionmaster.Find(id);//find selected record

                //...................................Selected dropdown value..................................//
                ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME", tab.LCATEID);
                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", tab.BANKMID);
                ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", tab.TRANMODE);
                ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == tab.REGSTRID), "REGSTRID", "REGSTRDESC", tab.REGSTRID);


                //.................Display status.................//
                List<SelectListItem> selectedDISP = new List<SelectListItem>();
                if (Convert.ToInt32(tab.DISPSTATUS) == 1)
                {
                    SelectListItem selectedItemDIS = new SelectListItem { Text = "In Books", Value = "0", Selected = false };
                    selectedDISP.Add(selectedItemDIS);
                    selectedItemDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = true };
                    selectedDISP.Add(selectedItemDIS);
                    ViewBag.DISPSTATUS = selectedDISP;
                }
                else
                {
                    SelectListItem selectedItemDIS = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
                    selectedDISP.Add(selectedItemDIS);
                    selectedItemDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = false };
                    selectedDISP.Add(selectedItemDIS);
                    ViewBag.DISPSTATUS = selectedDISP;
                }//....end

                List<SelectListItem> selectedTaxDISP = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANLSID) == 1)
                {
                    SelectListItem selectedTaxDIS = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
                    selectedTaxDISP.Add(selectedTaxDIS);
                    //selectedTaxDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = true };
                    // selectedTaxDISP.Add(selectedTaxDIS);
                    ViewBag.STAX = selectedTaxDISP;
                }
                else
                {
                    SelectListItem selectedTaxDIS = new SelectListItem { Text = "No", Value = "0", Selected = true };
                    selectedTaxDISP.Add(selectedTaxDIS);
                    //selectedTaxDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = false };
                    //selectedTaxDISP.Add(selectedTaxDIS);
                    ViewBag.STAX = selectedTaxDISP;
                }//....end


                //....................Bill type.................//
                List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANBTYPE) == 1)
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    // selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
                    // selectedBILLYPE.Add(selectedItemGPTY);

                }
                else
                {

                    //selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = false };
                    //  selectedBILLYPE.Add(selectedItemGPTY);
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                ViewBag.TRANBTYPE = selectedBILLYPE;
                //..........end

                //Storage Calc Type Drop Down List Start
                //var scalclst = context.Database.SqlQuery<decimal>("select sum(TRANDSAMT) from TransactionDetail where TRANMID = " + id).ToList();
                //var scaltype = 0;
                //if (scalclst.Count > 0)
                //{
                //    scaltype = 0;
                //}
                //else
                //{
                //    scaltype = 1;
                //}

                List<SelectListItem> selectedSCALDISP = new List<SelectListItem>();
                if (tab.TRAN_PULSE_STRG_TYPE == 0)
                {
                    SelectListItem selectedSCALC = new SelectListItem { Text = "Yes", Value = "0", Selected = true };
                    selectedSCALDISP.Add(selectedSCALC);
                    selectedSCALC = new SelectListItem { Text = "No", Value = "1", Selected = false };
                    selectedSCALDISP.Add(selectedSCALC);
                    ViewBag.SCALC = selectedSCALDISP;
                }
                else
                {
                    SelectListItem selectedSCALC = new SelectListItem { Text = "Yes", Value = "0", Selected = false };
                    selectedSCALDISP.Add(selectedSCALC);
                    selectedSCALC = new SelectListItem { Text = "No", Value = "1", Selected = true };
                    selectedSCALDISP.Add(selectedSCALC);
                    ViewBag.SCALC = selectedSCALDISP;
                }//....end

                //End

                vm.masterdata = context.transactionmaster.Where(det => det.TRANMID == id).ToList();
                vm.detaildata = context.transactiondetail.Where(det => det.TRANMID == id).ToList();
                vm.costfactor = context.transactionmasterfactor.Where(det => det.TRANMID == id).ToList();
                vm.impinvcedata = context.Database.SqlQuery<pr_Import_Invoice_IGMNO_Grid_Assgn_Result>("EXEC pr_Import_Invoice_IGMNO_Grid_Assgn @PIGMNO='-',@PLNO='-',@PTRANMID=" + id + "").ToList();//........procedure  for edit mode details data
                var billemid = Convert.ToInt32(vm.impinvcedata[0].BILLEMID);
                var sealcnt = context.Database.SqlQuery<int>("select NOC from VW_IMPORT_BILL_OPEN_SHEET_SEAL_COUNT_ASSGN where BILLEMID=" + billemid).ToList();
                if (sealcnt.Count > 0)
                    ViewBag.NOC = sealcnt[0];
                else
                    ViewBag.NOC = 0;

                ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
                var tariffmid = Convert.ToInt32(vm.detaildata[0].TARIFFMID);
                var sql = context.Database.SqlQuery<int>("select TGID from TariffMaster where TARIFFMID=" + tariffmid).ToList();
                ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC", Convert.ToInt32(sql[0]));
            }

            return View(vm);
        }//........End of form

        [Authorize(Roles = "ImportInvoiceCreate")]
        public ActionResult DOGSTForm(int id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            TransactionMaster tab = new TransactionMaster();
            TransactionMD vm = new TransactionMD();

            //..........................................Dropdown data.........................//
            ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME");
            // ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
            ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.SBMDATE = DateTime.Now;
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");
            ViewBag.TRANLMID = new SelectList("");

            ViewBag.TRANOTYPE = new SelectList(context.import_slab_handling_type_masters, "HTYPE", "HTYPEDESC");
            //ViewBag.TRANOTYPE = new SelectList(context.ImportDestuffSlipOperation, "OPRTYPE", "OPRTYPEDESC");
            ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC");

            ViewBag.TARIFFMID = new SelectList("");
            //...........Bill type......//
            List<SelectListItem> selectedBILLTYPE = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
            selectedBILLTYPE.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
            selectedBILLTYPE.Add(selectedItemDSP);
            ViewBag.TRANBTYPE = selectedBILLTYPE;
            //....end

            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst = new List<SelectListItem>();
            SelectListItem selectedItemtax = new SelectListItem { Text = "Yes", Value = "1", Selected = false };
            selectedtaxlst.Add(selectedItemtax);
            selectedItemtax = new SelectListItem { Text = "No", Value = "0", Selected = true };
            selectedtaxlst.Add(selectedItemtax);
            ViewBag.STAX = selectedtaxlst;

            ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == 1 || x.REGSTRID == 2 || x.REGSTRID == 6), "REGSTRID", "REGSTRDESC");
            //.....end

            //........display status.........//
            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDISP = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDISP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;
            //....end

            if (id != 0)//....Edit Mode
            {
                tab = context.transactionmaster.Find(id);//find selected record

                //...................................Selected dropdown value..................................//
                ViewBag.LCATEID = new SelectList(context.categorymasters.Where(m => m.CATETID == 6).Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CATENAME), "CATEID", "CATENAME", tab.LCATEID);
                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", tab.BANKMID);
                ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", tab.TRANMODE);
                ViewBag.REGSTRID = new SelectList(context.Export_Invoice_Register.Where(x => x.REGSTRID == tab.REGSTRID), "REGSTRID", "REGSTRDESC", tab.REGSTRID);


                //.................Display status.................//
                List<SelectListItem> selectedDISP = new List<SelectListItem>();
                if (Convert.ToInt32(tab.DISPSTATUS) == 1)
                {
                    SelectListItem selectedItemDIS = new SelectListItem { Text = "In Books", Value = "0", Selected = false };
                    selectedDISP.Add(selectedItemDIS);
                    selectedItemDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = true };
                    selectedDISP.Add(selectedItemDIS);
                    ViewBag.DISPSTATUS = selectedDISP;
                }
                else
                {
                    SelectListItem selectedItemDIS = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
                    selectedDISP.Add(selectedItemDIS);
                    selectedItemDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = false };
                    selectedDISP.Add(selectedItemDIS);
                    ViewBag.DISPSTATUS = selectedDISP;
                }//....end

                List<SelectListItem> selectedTaxDISP = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANLSID) == 1)
                {
                    SelectListItem selectedTaxDIS = new SelectListItem { Text = "Yes", Value = "1", Selected = true };
                    selectedTaxDISP.Add(selectedTaxDIS);
                    //selectedTaxDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = true };
                    // selectedTaxDISP.Add(selectedTaxDIS);
                    ViewBag.STAX = selectedTaxDISP;
                }
                else
                {
                    SelectListItem selectedTaxDIS = new SelectListItem { Text = "No", Value = "0", Selected = true };
                    selectedTaxDISP.Add(selectedTaxDIS);
                    //selectedTaxDIS = new SelectListItem { Text = "Cancelled", Value = "1", Selected = false };
                    //selectedTaxDISP.Add(selectedTaxDIS);
                    ViewBag.STAX = selectedTaxDISP;
                }//....end


                //....................Bill type.................//
                List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANBTYPE) == 1)
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    // selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = false };
                    // selectedBILLYPE.Add(selectedItemGPTY);

                }
                else
                {

                    //selectedItemGPTY = new SelectListItem { Text = "LOAD", Value = "1", Selected = false };
                    //  selectedBILLYPE.Add(selectedItemGPTY);
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "DESTUFF", Value = "2", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                ViewBag.TRANBTYPE = selectedBILLYPE;
                //..........end

                vm.masterdata = context.transactionmaster.Where(det => det.TRANMID == id).ToList();
                vm.detaildata = context.transactiondetail.Where(det => det.TRANMID == id).ToList();
                vm.costfactor = context.transactionmasterfactor.Where(det => det.TRANMID == id).ToList();
                vm.impinvcedata = context.Database.SqlQuery<pr_Import_Invoice_IGMNO_Grid_Assgn_Result>("EXEC pr_Import_Invoice_IGMNO_Grid_Assgn @PIGMNO='-',@PLNO='-',@PTRANMID=" + id + "").ToList();//........procedure  for edit mode details data
                var billemid = Convert.ToInt32(vm.impinvcedata[0].BILLEMID);
                var sealcnt = context.Database.SqlQuery<int>("select NOC from VW_IMPORT_BILL_OPEN_SHEET_SEAL_COUNT_ASSGN where BILLEMID=" + billemid).ToList();
                if (sealcnt.Count > 0)
                    ViewBag.NOC = sealcnt[0];
                else
                    ViewBag.NOC = 0;

                ViewBag.TARIFFMID = new SelectList(context.tariffmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TARIFFMDESC), "TARIFFMID", "TARIFFMDESC");
                var tariffmid = Convert.ToInt32(vm.detaildata[0].TARIFFMID);
                var sql = context.Database.SqlQuery<int>("select TGID from TariffMaster where TARIFFMID=" + tariffmid).ToList();
                ViewBag.TARIFFGID = new SelectList(context.tariffgroupmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.TGDESC), "TGID", "TGDESC", Convert.ToInt32(sql[0]));
            }

            return View(vm);
        }//........End of form


        //.................Insert/update values into database.............//
        public void savedata(FormCollection F_Form)
        {
            using (SCFSERPContext context = new SCFSERPContext())
            {
                using (var trans = context.Database.BeginTransaction())
                {
                    try
                    {
                        string todaydt = Convert.ToString(DateTime.Now);
                        string todayd = Convert.ToString(DateTime.Now.Date);

                        TransactionMaster transactionmaster = new TransactionMaster();
                        TransactionDetail transactiondetail = new TransactionDetail();
                        //-------Getting Primarykey field--------
                        Int32 TRANMID = Convert.ToInt32(F_Form["masterdata[0].TRANMID"]);
                        Int32 TRANDID = 0;
                        string DELIDS = "";
                        //-----End


                        // Capture before state for edit logging
                        TransactionMaster before = null;
                        List<TransactionDetail> beforeDetails = null;
                        List<TransactionMasterFactor> beforeFactors = null;
                        string beforeDPAIDNO = null;
                        if (TRANMID != 0)
                        {
                            transactionmaster = context.transactionmaster.Find(TRANMID);
                            try
                            {
                                before = context.transactionmaster.AsNoTracking().FirstOrDefault(x => x.TRANMID == TRANMID);
                                if (before != null)
                                {
                                    EnsureBaselineVersionZero(before, Session["CUSRID"]?.ToString() ?? "");
                                    // Capture TransactionDetail before state
                                    beforeDetails = context.transactiondetail.AsNoTracking().Where(x => x.TRANMID == TRANMID).ToList();
                                    // Capture TransactionMasterFactor before state
                                    beforeFactors = context.transactionmasterfactor.AsNoTracking().Where(x => x.TRANMID == TRANMID).ToList();
                                }
                            }
                            catch { /* ignore if baseline creation fails */ }
                        }

                        //...........transaction master.............//
                        transactionmaster.TRANMID = TRANMID;
                        transactionmaster.COMPYID = Convert.ToInt32(Session["compyid"]);
                        transactionmaster.SDPTID = 1;
                        transactionmaster.TRANTID = 2;
                        if (Convert.ToInt32(F_Form["CUSTGID"]) == 6)
                        {
                            transactionmaster.CUSTGID = Convert.ToInt32(F_Form["CUSTGID"]);
                            transactionmaster.REGSTRID = 70;// Convert.ToInt16(F_Form["REGSTRID"]);
                        }
                        else
                        {
                            transactionmaster.CUSTGID = 1;
                            transactionmaster.REGSTRID = Convert.ToInt16(F_Form["REGSTRID"]);
                        }
                        transactionmaster.TRANLMID = Convert.ToInt32(F_Form["TRANLMID"]);
                        transactionmaster.TRANLSID = Convert.ToInt16(F_Form["STAX"]); //0;
                        transactionmaster.TRANLSNO = null;
                        transactionmaster.TRANLMNO = F_Form["TRANLMNO"].ToString();
                        if (F_Form["TRANLMDATE"] == "")
                            transactionmaster.TRANLMDATE = Convert.ToDateTime(DateTime.Now).Date; //;
                        else
                            transactionmaster.TRANLMDATE = Convert.ToDateTime(F_Form["TRANLMDATE"]).Date; //DateTime.Now;
                        transactionmaster.TRANLSDATE = DateTime.Now;
                        transactionmaster.TRANNARTN = null;
                        if (TRANMID == 0)
                            transactionmaster.CUSRID = Session["CUSRID"].ToString();
                        transactionmaster.LMUSRID = Session["CUSRID"].ToString();
                        transactionmaster.DISPSTATUS = Convert.ToInt16(F_Form["DISPSTATUS"]);
                        transactionmaster.PRCSDATE = DateTime.Now;

                        string indate = Convert.ToString(F_Form["masterdata[0].TRANDATE"]);

                        if (indate != null || indate != "")
                        {
                            transactionmaster.TRANDATE = Convert.ToDateTime(indate).Date;
                        }
                        else { transactionmaster.TRANDATE = DateTime.Now.Date; }

                        if (transactionmaster.TRANDATE > Convert.ToDateTime(todayd))
                        {
                            transactionmaster.TRANDATE = Convert.ToDateTime(todayd);
                        }

                        string intime = Convert.ToString(F_Form["masterdata[0].TRANTIME"]);

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

                                    transactionmaster.TRANTIME = Convert.ToDateTime(in_datetime);
                                }
                                else { transactionmaster.TRANTIME = DateTime.Now; }
                            }
                            else { transactionmaster.TRANTIME = DateTime.Now; }
                        }
                        else { transactionmaster.TRANTIME = DateTime.Now; }


                        if (transactionmaster.TRANTIME > Convert.ToDateTime(todaydt))
                        {
                            transactionmaster.TRANTIME = Convert.ToDateTime(todaydt);
                        }

                        //transactionmaster.TRANDATE = Convert.ToDateTime(F_Form["masterdata[0].TRANTIME"]).Date;
                        //transactionmaster.TRANTIME = Convert.ToDateTime(F_Form["masterdata[0].TRANTIME"]);

                        transactionmaster.TRANREFID = Convert.ToInt32(F_Form["masterdata[0].TRANREFID"]);//chaid
                        transactionmaster.TRANREFNAME = F_Form["masterdata[0].TRANREFNAME"].ToString();//chaname
                        transactionmaster.LCATEID = Convert.ToInt32(F_Form["LCATEID"]);
                        transactionmaster.TRANBTYPE = Convert.ToInt16(F_Form["TRANBTYPE"]);
                        
                        transactionmaster.TRANMODE = Convert.ToInt16(F_Form["TRANMODE"]);
                        transactionmaster.TRANMODEDETL = (F_Form["masterdata[0].TRANMODEDETL"]);
                        transactionmaster.TRANGAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANGAMT"]);
                        transactionmaster.TRANNAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANNAMT"]);
                        transactionmaster.TRANROAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANROAMT"]);
                        transactionmaster.TRANREFAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANREFAMT"]);
                        transactionmaster.TRANRMKS = (F_Form["masterdata[0].TRANRMKS"]).ToString();
                        transactionmaster.TRANAMTWRDS = AmtInWrd.ConvertNumbertoWords(F_Form["masterdata[0].TRANNAMT"]);
                        transactionmaster.TRANINOC1 = Convert.ToDecimal(F_Form["masterdata[0].TRANINOC1"]);
                        transactionmaster.TRANINOC2 = Convert.ToDecimal(F_Form["masterdata[0].TRANINOC2"]);
                        transactionmaster.TRANSAMT = Convert.ToDecimal(F_Form["TRANSAMT"]);
                        transactionmaster.TRANAAMT = Convert.ToDecimal(F_Form["TRANAAMT"]);
                        transactionmaster.TRANADONAMT = Convert.ToDecimal(F_Form["TRANADONAMT"]);
                        transactionmaster.TRANHAMT = Convert.ToDecimal(F_Form["TRANHAMT"]);
                        transactionmaster.TRANEAMT = Convert.ToDecimal(F_Form["TRANEAMT"]);
                        transactionmaster.TRANFAMT = Convert.ToDecimal(F_Form["TRANFAMT"]);
                        transactionmaster.TRANTCAMT = Convert.ToDecimal(F_Form["TRANTCAMT"]);
                        transactionmaster.TRAN_COVID_DISC_AMT = Convert.ToDecimal(F_Form["masterdata[0].TRAN_COVID_DISC_AMT"]);

                        transactionmaster.STRG_HSNCODE = F_Form["STRG_HSN_CODE"].ToString();
                        transactionmaster.HANDL_HSNCODE = F_Form["HANDL_HSN_CODE"].ToString();

                        transactionmaster.STRG_TAXABLE_AMT = Convert.ToDecimal(F_Form["STRG_TAXABLE_AMT"]);
                        transactionmaster.HANDL_TAXABLE_AMT = Convert.ToDecimal(F_Form["HANDL_TAXABLE_AMT"]);

                        transactionmaster.STRG_CGST_EXPRN = Convert.ToDecimal(F_Form["STRG_CGST_EXPRN"]);
                        transactionmaster.STRG_SGST_EXPRN = Convert.ToDecimal(F_Form["STRG_SGST_EXPRN"]);
                        transactionmaster.STRG_IGST_EXPRN = Convert.ToDecimal(F_Form["STRG_IGST_EXPRN"]);
                        transactionmaster.STRG_CGST_AMT = Convert.ToDecimal(F_Form["STRG_CGST_AMT"]);
                        transactionmaster.STRG_SGST_AMT = Convert.ToDecimal(F_Form["STRG_SGST_AMT"]);
                        transactionmaster.STRG_IGST_AMT = Convert.ToDecimal(F_Form["STRG_IGST_AMT"]);

                        transactionmaster.HANDL_CGST_EXPRN = Convert.ToDecimal(F_Form["HANDL_CGST_EXPRN"]);
                        transactionmaster.HANDL_SGST_EXPRN = Convert.ToDecimal(F_Form["HANDL_SGST_EXPRN"]);
                        transactionmaster.HANDL_IGST_EXPRN = Convert.ToDecimal(F_Form["HANDL_IGST_EXPRN"]);
                        transactionmaster.HANDL_CGST_AMT = Convert.ToDecimal(F_Form["HANDL_CGST_AMT"]);
                        transactionmaster.HANDL_SGST_AMT = Convert.ToDecimal(F_Form["HANDL_SGST_AMT"]);
                        transactionmaster.HANDL_IGST_AMT = Convert.ToDecimal(F_Form["HANDL_IGST_AMT"]);

                        transactionmaster.TRANCGSTAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANCGSTAMT"]);
                        transactionmaster.TRANSGSTAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANSGSTAMT"]);
                        transactionmaster.TRANIGSTAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANIGSTAMT"]);

                        transactionmaster.TRANNARTN = Convert.ToString(F_Form["masterdata[0].TRANNARTN"]);

                        transactionmaster.TRAN_PULSE_STRG_TYPE = Convert.ToInt32(F_Form["SCALC"]);

                        var tranmode = Convert.ToInt16(F_Form["TRANMODE"]);
                        if (tranmode != 2 && tranmode != 3)
                        {
                            transactionmaster.TRANREFNO = "";
                            transactionmaster.TRANREFBNAME = "";
                            transactionmaster.BANKMID = 0;
                            transactionmaster.TRANREFDATE = DateTime.Now;
                        }
                        else
                        {
                            transactionmaster.TRANREFNO = (F_Form["masterdata[0].TRANREFNO"]).ToString();
                            transactionmaster.TRANREFBNAME = (F_Form["masterdata[0].TRANREFBNAME"]).ToString();
                            transactionmaster.BANKMID = Convert.ToInt32(F_Form["BANKMID"]);
                            transactionmaster.TRANREFDATE = Convert.ToDateTime(F_Form["masterdata[0].TRANREFDATE"]).Date;

                        }


                        //.................Autonumber............//
                        var regsid = Convert.ToInt32(F_Form["REGSTRID"]);
                        var btype = Convert.ToInt32(F_Form["TRANBTYPE"]);
                        var custgid = Convert.ToInt32(F_Form["CUSTGID"]);
                        string format = "";
                        string billformat = "";

                        if (TRANMID == 0)
                        {
                            //transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.newgstautonum("transactionmaster", "TRANNO", regsid.ToString(), Session["compyid"].ToString(), btype.ToString(), Convert.ToInt32(transactionmaster.TRANREFID),Convert.ToInt32(custgid)).ToString()); --DELETE
                            if (custgid == 6)
                            {
                                transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.sezgstautonum("transactionmaster", "TRANNO", "70", Session["compyid"].ToString(), btype.ToString(), Convert.ToInt32(transactionmaster.TRANREFID)).ToString());
                            }
                            else
                            {
                                transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.gstautonum("transactionmaster", "TRANNO", regsid.ToString(), Session["compyid"].ToString(), btype.ToString(), Convert.ToInt32(transactionmaster.TRANREFID)).ToString());
                            }
                            int ano = transactionmaster.TRANNO;
                            if (regsid == 1 && btype == 1)
                            {
                                billformat = "STL/LD/CU/";
                            }
                            else if (regsid == 1 && btype == 2)
                            {
                                billformat = "STL/DS/CU/";
                            }

                            else if (regsid == 2 && btype == 1)
                            {
                                billformat = "STL/LD/CH/";
                            }

                            else if (regsid == 2 && btype == 2)
                            {
                                billformat = "STL/DS/CH/";
                            }
                            else if (regsid == 6 && btype == 1)
                            {
                                billformat = "ZB/LD/";
                            }
                            else if (regsid == 6 && btype == 2)
                            {
                                billformat = "ZB/DS/";
                            }

                            if (custgid == 6)
                            {
                                billformat = "STL/LD/SEZ/";
                            }

                                //........end of autonumber
                                //format = "SUD/IMP/";
                            string btyp = auto_numbr_invoice.GetCateBillType(Convert.ToInt32(transactionmaster.TRANREFID)).ToString();
                            if (btyp == "")
                            {
                                if (custgid == 6)
                                {
                                    format = "SEZ/" + Session["GPrxDesc"] + "/";
                                }
                                else
                                {
                                    format = "IMP/" + Session["GPrxDesc"] + "/";
                                }
                                    
                            }
                             else
                                format = "IMP" + Session["GPrxDesc"] + btyp;
                           

                            string prfx = string.Format(format + "{0:D5}", ano);
                            string billprfx = string.Format(billformat + "{0:D5}", ano);
                            transactionmaster.TRANDNO = prfx.ToString();
                            transactionmaster.TRANBILLREFNO = billprfx.ToString();
                            context.transactionmaster.Add(transactionmaster);
                            context.SaveChanges();
                            TRANMID = transactionmaster.TRANMID;
                            
                            // Baseline will be created after TransactionDetail records are saved (see below)
                        }
                        else
                        {
                            //transactionmaster.REGSTRID = Convert.ToInt16(F_Form["masterdata[0].REGSTRID"]);
                            //transactionmaster.TRANMODE = Convert.ToInt16(F_Form["TRANMODE"]);


                            //int ano = transactionmaster.TRANNO;
                            //if (regsid == 1 && btype == 1)
                            //{
                            //    billformat = "STL/LD/CU/";
                            //}
                            //else if (regsid == 1 && btype == 2)
                            //{
                            //    billformat = "STL/DS/CU/";
                            //}

                            //else if (regsid == 2 && btype == 1)
                            //{
                            //    billformat = "STL/LD/CH/";
                            //}

                            //else if (regsid == 2 && btype == 2)
                            //{
                            //    billformat = "STL/DS/CH/";
                            //}
                            //else if (regsid == 6 && btype == 1)
                            //{
                            //    billformat = "ZB/LD/";
                            //}
                            //else if (regsid == 6 && btype == 2)
                            //{
                            //    billformat = "ZB/DS/";
                            //}
                            ////........end of autonumber
                            ////format = "SUD/IMP/";
                            //string btyp = auto_numbr_invoice.GetCateBillType(Convert.ToInt32(transactionmaster.TRANREFID)).ToString();
                            //if (btyp == "")
                            //{
                            //    format = "IMP/" + Session["GPrxDesc"] + "/";
                            //}
                            //else
                            //    format = "IMP" + Session["GPrxDesc"] + btyp;


                            //string prfx = string.Format(format + "{0:D5}", ano);
                            //string billprfx = string.Format(billformat + "{0:D5}", ano);
                            //transactionmaster.TRANDNO = prfx.ToString();
                            //transactionmaster.TRANBILLREFNO = billprfx.ToString();
                            context.Entry(transactionmaster).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();
                        }


                        //-------------transaction Details
                        string[] F_TRANDID = F_Form.GetValues("TRANDID");
                        string[] TRANDREFNO = F_Form.GetValues("TRANDREFNO");
                        string[] TRANDREFNAME = F_Form.GetValues("TRANDREFNAME");
                        string[] TRANIDATE = F_Form.GetValues("TRANIDATE");
                        string[] TRANSDATE = F_Form.GetValues("TRANSDATE");
                        string[] TRANEDATE = F_Form.GetValues("TRANEDATE");
                        string[] STFDIDS = F_Form.GetValues("STFDIDS");
                        string[] STFDID = F_Form.GetValues("STFDID");
                        string[] boolSTFDIDS = F_Form.GetValues("boolSTFDIDS");
                        string[] TRANDHAMT = F_Form.GetValues("TRANDHAMT");
                        string[] TRANDEAMT = F_Form.GetValues("TRANDEAMT");
                        string[] TRANDFAMT = F_Form.GetValues("TRANDFAMT");
                        string[] TRANDAAMT = F_Form.GetValues("TRANDAAMT");
                        string[] TRANDADONAMT = F_Form.GetValues("TRANDADONAMT");
                        string[] TRANDNAMT = F_Form.GetValues("TRANDNAMT");
                        string[] TRANDSAMT = F_Form.GetValues("TRANDSAMT");
                        string[] TRANDNOP = F_Form.GetValues("TRANDNOP");
                        string[] TRANDQTY = F_Form.GetValues("TRANDQTY");
                        string[] TRANDREFID = F_Form.GetValues("TRANDREFID");
                        string[] RAMT1 = F_Form.GetValues("RAMT1");
                        string[] RAMT2 = F_Form.GetValues("RAMT2");
                        string[] RAMT3 = F_Form.GetValues("RAMT3");
                        string[] RAMT4 = F_Form.GetValues("RAMT4");
                        string[] RAMT5 = F_Form.GetValues("RAMT5");
                        string[] RAMT6 = F_Form.GetValues("RAMT6");
                        string[] RCAMT1 = F_Form.GetValues("RCAMT1");
                        string[] RCAMT2 = F_Form.GetValues("RCAMT2");
                        string[] RCAMT3 = F_Form.GetValues("RCAMT3");
                        string[] RCAMT4 = F_Form.GetValues("RCAMT4");
                        string[] RCAMT5 = F_Form.GetValues("RCAMT5");
                        string[] RCAMT6 = F_Form.GetValues("RCAMT6");
                        string[] RCOL1 = F_Form.GetValues("RCOL1");
                        string[] RCOL2 = F_Form.GetValues("RCOL2");
                        string[] RCOL3 = F_Form.GetValues("RCOL3");
                        string[] RCOL4 = F_Form.GetValues("RCOL4");
                        string[] RCOL5 = F_Form.GetValues("RCOL5");
                        string[] RCOL6 = F_Form.GetValues("RCOL6");
                        string[] days = F_Form.GetValues("days");
                        string[] NOD = F_Form.GetValues("NOD");//days
                        string[] TRANDRATE = F_Form.GetValues("TRANDRATE");
                        string[] BILLEDID = F_Form.GetValues("BILLEDID"); string[] F_BILLEMID = F_Form.GetValues("BILLEMID"); string[] TRANDWGHT = F_Form.GetValues("TRANDWGHT");
                        string[] TRANOTYPE = F_Form.GetValues("detaildata[0].TRANOTYPE");
                        string[] TRAND_COVID_DISC_AMT = F_Form.GetValues("TRAND_COVID_DISC_AMT");

                        var BILLEMID = 0;
                        for (int count = 0; count < F_TRANDID.Count(); count++)
                        {
                            if (boolSTFDIDS[count] == "true")
                            {
                                TRANDID = Convert.ToInt32(F_TRANDID[count]); BILLEMID = Convert.ToInt32(F_BILLEMID[count]);
                                // Capture DPAIDNO before state for edit logging (only once)
                                if (BILLEMID > 0 && beforeDPAIDNO == null)
                                {
                                    try
                                    {
                                        var result = context.Database.SqlQuery<string>("SELECT DPAIDNO FROM BILLENTRYMASTER WHERE BILLEMID=" + BILLEMID).FirstOrDefault();
                                        beforeDPAIDNO = result ?? "";
                                    }
                                    catch { /* ignore if capture fails */ }
                                }
                                //  var boolSTFDID = Convert.ToString(boolSTFDIDS[count]);
                                if (TRANDID != 0)
                                {
                                    transactiondetail = context.transactiondetail.Find(TRANDID);
                                }
                                transactiondetail.TRANMID = transactionmaster.TRANMID;
                                transactiondetail.TRANDREFNO = (TRANDREFNO[count]).ToString();
                                transactiondetail.TRANDREFNAME = (TRANDREFNAME[count]).ToString();
                                transactiondetail.TRANDREFID = Convert.ToInt32(TRANDREFID[count]);//GIDID
                                transactiondetail.TRANIDATE = Convert.ToDateTime(TRANIDATE[count]);
                                transactiondetail.TRANSDATE = Convert.ToDateTime(TRANSDATE[count]);
                                transactiondetail.TRANEDATE = Convert.ToDateTime(TRANEDATE[count]);
                                transactiondetail.TRANVDATE = Convert.ToDateTime(F_Form["detaildata[0].TRANVDATE"]);
                                transactiondetail.TRANDSAMT = Convert.ToDecimal(TRANDSAMT[count]);
                                transactiondetail.TRANDHAMT = Convert.ToDecimal(TRANDHAMT[count]);
                                transactiondetail.TRANDEAMT = Convert.ToDecimal(TRANDEAMT[count]);
                                transactiondetail.TRANDFAMT = Convert.ToDecimal(TRANDFAMT[count]);
                                transactiondetail.TRANDAAMT = Convert.ToDecimal(TRANDAAMT[count]);
                                transactiondetail.TRANDADONAMT = Convert.ToDecimal(TRANDADONAMT[count]);
                                transactiondetail.TRANDNAMT = Convert.ToDecimal(TRANDNAMT[count]);
                                //  transactiondetail.TRANDNOP = Convert.ToDecimal(TRANDNOP[count]);
                                transactiondetail.TRANDQTY = Convert.ToDecimal(NOD[count]);//NO.OF DAYS
                                transactiondetail.TARIFFMID = Convert.ToInt32(F_Form["detaildata[0].TARIFFMID"]);
                                //  transactiondetail.TRANDRATE = 0;
                                transactiondetail.TRANDRATE = Convert.ToDecimal(TRANDRATE[count]);//TRANSPORT CHARGE
                                transactiondetail.TRANOTYPE = Convert.ToInt16(F_Form["detaildata[0].TRANOTYPE"]);
                                transactiondetail.TRANDGAMT = Convert.ToDecimal(TRANDNAMT[count]);
                                transactiondetail.BILLEDID = Convert.ToInt32(BILLEDID[count]);
                                transactiondetail.RCOL1 = Convert.ToDecimal(RCOL1[count]);
                                transactiondetail.RCOL2 = Convert.ToDecimal(RCOL2[count]);
                                transactiondetail.RCOL3 = Convert.ToDecimal(RCOL3[count]);
                                transactiondetail.RCOL4 = Convert.ToDecimal(RCOL4[count]);
                                transactiondetail.RCOL5 = Convert.ToDecimal(RCOL5[count]);
                                transactiondetail.RCOL6 = Convert.ToDecimal(RCOL6[count]);
                                transactiondetail.RCOL7 = 0;
                                transactiondetail.RAMT1 = Convert.ToDecimal(RAMT1[count]);
                                transactiondetail.RAMT2 = Convert.ToDecimal(RAMT2[count]);
                                transactiondetail.RAMT3 = Convert.ToDecimal(RAMT3[count]);
                                transactiondetail.RAMT4 = Convert.ToDecimal(RAMT4[count]);
                                transactiondetail.RAMT5 = Convert.ToDecimal(RAMT5[count]);
                                transactiondetail.RAMT6 = Convert.ToDecimal(RAMT6[count]);
                                transactiondetail.RCAMT1 = Convert.ToDecimal(RCAMT1[count]);
                                transactiondetail.RCAMT2 = Convert.ToDecimal(RCAMT2[count]);
                                transactiondetail.RCAMT3 = Convert.ToDecimal(RCAMT3[count]);
                                transactiondetail.RCAMT4 = Convert.ToDecimal(RCAMT4[count]);
                                transactiondetail.RCAMT5 = Convert.ToDecimal(RCAMT5[count]);
                                transactiondetail.RCAMT6 = Convert.ToDecimal(RCAMT6[count]);
                                transactiondetail.SLABTID = 0;
                                transactiondetail.TRANYTYPE = 0;
                                transactiondetail.TRANDWGHT = Convert.ToDecimal(TRANDWGHT[count]);
                                transactiondetail.TRANDAID = 0;
                                transactiondetail.SBDID = 0;
                                transactiondetail.TRAND_COVID_DISC_AMT = Convert.ToDecimal(TRAND_COVID_DISC_AMT[count]);

                                if (Convert.ToInt32(TRANDID) == 0)
                                {
                                    context.transactiondetail.Add(transactiondetail);
                                    context.SaveChanges();
                                    TRANDID = transactiondetail.TRANDID;
                                }
                                else
                                {
                                    transactiondetail.TRANDID = TRANDID;
                                    context.Entry(transactiondetail).State = System.Data.Entity.EntityState.Modified;
                                    context.SaveChanges();
                                }//..............end
                                DELIDS = DELIDS + "," + TRANDID.ToString();
                            }
                        }
                        context.Database.ExecuteSqlCommand("UPDATE BILLENTRYMASTER SET DPAIDNO='" + F_Form["DPAIDNO"] + "',DPAIDAMT='" + Convert.ToDecimal(F_Form["DPAIDAMT"]) + "'  WHERE BILLEMID=" + BILLEMID);
                        //-------delete transaction master factor-------//
                        context.Database.ExecuteSqlCommand("DELETE FROM transactionmasterfactor WHERE tranmid=" + TRANMID);

                        //Transaction Type Master-------//

                        TransactionMasterFactor transactionmasterfactors = new TransactionMasterFactor();
                        string[] DEDEXPRN = F_Form.GetValues("CFEXPR");
                        string[] TAX1 = F_Form.GetValues("TAX");
                        string[] DEDMODE = F_Form.GetValues("CFMODE");
                        string[] DEDTYPE = F_Form.GetValues("CFTYPE");
                        string[] DORDRID = F_Form.GetValues("DORDRID");
                        string[] DEDNOS = F_Form.GetValues("DEDNOS");
                        string[] DEDVALUE = F_Form.GetValues("CFAMOUNT");
                        string[] CFAMOUNT = F_Form.GetValues("CFAMOUNT");
                        string[] CFDESC = F_Form.GetValues("CFDESC");

                        if (DORDRID != null)
                        {
                            for (int count2 = 0; count2 < DORDRID.Count(); count2++)
                            {

                                transactionmasterfactors.TRANMID = transactionmaster.TRANMID;
                                transactionmasterfactors.DORDRID = Convert.ToInt16(DORDRID[count2]);
                                transactionmasterfactors.DEDMODE = DEDMODE[count2].ToString();
                                transactionmasterfactors.DEDVALUE = Convert.ToDecimal(DEDVALUE[count2]);
                                transactionmasterfactors.DEDTYPE = Convert.ToInt16(DEDTYPE[count2]);
                                transactionmasterfactors.DEDEXPRN = Convert.ToDecimal(DEDEXPRN[count2]);
                                transactionmasterfactors.CFID = Convert.ToInt32(TAX1[count2]);
                                transactionmasterfactors.DEDCFDESC = CFDESC[count2];
                                transactionmasterfactors.DEDNOS = Convert.ToDecimal(DEDNOS[count2]);
                                transactionmasterfactors.CFOPTN = 0;
                                transactionmasterfactors.DEDORDR = 0;
                                context.transactionmasterfactor.Add(transactionmasterfactors);
                                context.SaveChanges();
                            }
                        }
                        context.Database.ExecuteSqlCommand("DELETE FROM transactiondetail  WHERE TRANMID=" + TRANMID + " and  TRANDID NOT IN(" + DELIDS.Substring(1) + ")");
                        //  Response.Redirect("Index");
                        trans.Commit();
                        
                        // Ensure V0 baseline exists before logging changes (for both new and existing records)
                        try
                        {
                            var currentRecord = context.transactionmaster.AsNoTracking().FirstOrDefault(x => x.TRANMID == TRANMID);
                            if (currentRecord != null)
                            {
                                // Ensure baseline exists (will only create if it doesn't exist)
                                EnsureBaselineVersionZero(currentRecord, Session["CUSRID"]?.ToString() ?? "");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to ensure baseline exists: {ex.Message}");
                        }
                        
                        // Log changes after successful save (only for existing records that were edited)
                        if (before != null && TRANMID != 0)
                        {
                            try
                            {
                                var after = context.transactionmaster.AsNoTracking().FirstOrDefault(x => x.TRANMID == TRANMID);
                                if (after != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"=== LogTransactionEdits called for TRANMID={TRANMID}, beforeDetails.Count={beforeDetails?.Count ?? 0} ===");
                                    LogTransactionEdits(before, after, Session["CUSRID"]?.ToString() ?? "", context, beforeDetails);
                                    System.Diagnostics.Debug.WriteLine($"=== LogTransactionEdits completed for TRANMID={TRANMID} ===");
                                    
                                    // Log DPAIDNO changes from BILLENTRYMASTER
                                    if (BILLEMID > 0)
                                    {
                                        try
                                        {
                                            var afterDPAIDNO = context.Database.SqlQuery<string>("SELECT DPAIDNO FROM BILLENTRYMASTER WHERE BILLEMID=" + BILLEMID).FirstOrDefault() ?? "";
                                            if (beforeDPAIDNO != afterDPAIDNO)
                                            {
                                                var gidno = TRANMID.ToString();
                                                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                                                if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
                                                {
                                                    // Get version label
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
                                                            WHERE [GIDNO] = @GIDNO AND [Modules] = 'ImportInvoice'", sql))
                                                        {
                                                            cmd.Parameters.AddWithValue("@GIDNO", gidno);
                                                            sql.Open();
                                                            var obj = cmd.ExecuteScalar();
                                                            if (obj != null && obj != DBNull.Value)
                                                                nextVersion = Convert.ToInt32(obj);
                                                        }
                                                    }
                                                    catch { /* ignore version errors */ }
                                                    
                                                    var versionLabel = $"V{nextVersion}-{gidno}";
                                                    InsertEditLogRow(cs.ConnectionString, gidno, "DPAIDNO", beforeDPAIDNO ?? "", afterDPAIDNO, Session["CUSRID"]?.ToString() ?? "", versionLabel, "ImportInvoice");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Failed to log DPAIDNO changes: {ex.Message}");
                                        }
                                    }
                                    
                                    // Log TransactionMasterFactor (CFID) changes
                                    try
                                    {
                                        var afterFactors = context.transactionmasterfactor.AsNoTracking().Where(x => x.TRANMID == TRANMID).ToList();
                                        LogTransactionMasterFactorEdits(beforeFactors, afterFactors, TRANMID.ToString(), Session["CUSRID"]?.ToString() ?? "", context);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Failed to log TransactionMasterFactor changes: {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to log changes: {ex.Message}");
                            }
                        }
                        
                        Response.Redirect("Index");
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        Response.Write("Sorry!!An Error Ocurred..." + ex.Message);
                        // Response.Redirect("/Error/AccessDenied");
                    }
                }
            }

        }


        //...........cost factor with default value
        public JsonResult DefaultCF()
        {
            //DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(2,5,6,77,65,90,4) order by CFID DESC");

            DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(2,6,65,69,77,90,4) and DISPSTATUS=0 order by CFID DESC");

            DbSqlQuery<CostFactorMaster> data2 = context.costfactormasters.SqlQuery("select * from costfactormaster  where DISPSTATUS=0 and CFID  in(96,97) and DISPSTATUS=0 order by CFID");
            return Json(data.Concat(data2), JsonRequestBehavior.AllowGet);

        }//....end
        public string defCostFactor()
        {

            //DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(5,6,77,65,90) and DISPSTATUS=0 order by CFID DESC");

            DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(6,65,69,77,90) and DISPSTATUS=0 order by CFID DESC");


            string html = "";
            //<select id='TAX' class='TAX' name='TAX' onchange='sel_text(this,&quot;CFDESC&quot;);'  > ";

            string first = "";
            string f_order = "";
            string f_expr = "";
            string mod = "";
            string expr = "";
            string first_id = "0";

            int i = 0;

            foreach (var cost in data)
            {

                first_id = cost.CFID.ToString();




                //if (i == 0)
                //{
                first = cost.CFDESC;
                f_order = cost.DORDRID.ToString();
                f_expr = cost.CFEXPR.ToString();
                if (cost.CFMODE != 0)
                    mod = "selected";
                if (cost.CFTYPE != 0)
                    expr = "selected";
                else expr = "";
                //   }

                // html = html + "<option value='" + cost.CFID + "'>" + cost.CFDESC + "</option>";
                html = html + "<tr class='item-row'><Td> <button href='#'   type='button' onclick='del_factor(this)' class='btn btn-danger dfact btn-xs'><i class=glyphicon-minus></i> </button>  </td> <td><input type=text name=TAX id='TAX'  class='hide TAX' value='" + cost.CFID + "'><input type=text name=CFDESC id='CFDESC' class='hide CFDESC' value='" + cost.CFDESC + "'>" + cost.CFDESC + "";
                html = html + "</td> <tD class='col-lg-1' > <select id='CFTYPE' name='CFTYPE' class='CFTYPE' onchange='totalonchange(this)'> <option value='0' >Value </option><option value='1' " + expr + "  >  %</option> </select></td> <td class='col-lg-1'><input type='text' id='DEDNOS' class='DEDNOS' name='DEDNOS' onchange='totalonchange(this)' value='0' style='width:50px'></td><td class='col-lg-1' > <input onchange='totalonchange(this)' type=text value='" + cost.CFEXPR + "' class='CFEXPR' name='CFEXPR' id='CFEXPR'> </td><td><select onchange='totalonchange(this)' class='CFMODE' id='CFMODE' name='CFMODE'> <option value='0' >  +</option><option value='1' " + mod + " >-</option> </select><input type='text' id='DORDRID'   value='" + cost.DORDRID + "' class='DORDRID' style='display:none'  name='DORDRID' >  <input  type=text value='0' style='display:none' name=TRANMFID id='TRANMFID' class='TRANMFID' >";
                html = html + "<input  type=text value='0' style='display:none' name=DEDORDR id='DEDORDR' class='DEDORDR' ><input  type=text value='0' style='display:none' name=TMPCFVAL id='TMPCFVAL' class='TMPCFVAL' ></td><td><input  type=text value='' name=CFAMOUNT id='CFAMOUNT' class='CFAMOUNT' readonly='readonly'> </td>  </TD></tr>";

                i++;

                //do something with cust
            }
            DbSqlQuery<CostFactorMaster> data1 = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(4) and DISPSTATUS=0 order by CFID DESC");
            foreach (var cost in data1)
            {

                first_id = cost.CFID.ToString();




                //if (i == 0)
                //{
                first = cost.CFDESC;
                f_order = cost.DORDRID.ToString();
                f_expr = cost.CFEXPR.ToString();
                if (cost.CFMODE != 0)
                    mod = "selected";
                if (cost.CFTYPE != 0)
                    expr = "selected";
                else expr = "";
                //   }

                // html = html + "<option value='" + cost.CFID + "'>" + cost.CFDESC + "</option>";
                html = html + "<tr class='item-row'><Td> <button href='#'   type='button' onclick='del_factor(this)' class='btn btn-danger dfact btn-xs'><i class=glyphicon-minus></i> </button>  </td> <td><input type=text name=TAX id='TAX'  class='hide TAX' value='" + cost.CFID + "'><input type=text name=CFDESC id='CFDESC' class='hide CFDESC' value='" + cost.CFDESC + "'>" + cost.CFDESC + "";
                html = html + "</td> <tD class='col-lg-1' > <select id='CFTYPE' name='CFTYPE' class='CFTYPE' onchange='totalonchange(this)'><option value='0'  >Value </option>  <option value='1'  " + expr + " >  %</option></select></td> <td class='col-lg-1'><input type='text' id='DEDNOS' class='DEDNOS' name='DEDNOS' value='0' onchange='totalonchange(this)' style='width:50px'></td><td class='col-lg-1' > <input onchange='totalonchange(this)' type=text value='" + cost.CFEXPR + "' class='CFEXPR' name='CFEXPR' id='CFEXPR'> </td><td><select onchange='totalonchange(this)' class='CFMODE' id='CFMODE' name='CFMODE'> <option value='0' >  +</option><option value='1' " + mod + " >-</option> </select><input type='text' id='DORDRID'   value='" + cost.DORDRID + "' class='DORDRID' style='display:none'  name='DORDRID' >  <input  type=text value='0' style='display:none' name=TRANMFID id='TRANMFID' class='TRANMFID' >";
                html = html + "<input  type=text value='0' style='display:none' name=DEDORDR id='DEDORDR' class='DEDORDR' ><input  type=text value='0' style='display:none' name=TMPCFVAL id='TMPCFVAL' class='TMPCFVAL' ></td><td><input  type=text value='' name=CFAMOUNT id='CFAMOUNT' class='CFAMOUNT' readonly='readonly'> </td>  </TD></tr>";


                //do something with cust
            }
            DbSqlQuery<CostFactorMaster> data2 = context.costfactormasters.SqlQuery("select * from costfactormaster  where DISPSTATUS=0 and CFID  in(2,96,97)");
            foreach (var cost in data2)
            {

                first_id = cost.CFID.ToString();


                //if (i == 0)
                //{
                first = cost.CFDESC;
                f_order = cost.DORDRID.ToString();
                f_expr = cost.CFEXPR.ToString();
                if (cost.CFMODE != 0)
                    mod = "selected";
                if (cost.CFTYPE != 0)
                    expr = "selected";
                else expr = "";
                //   }

                // html = html + "<option value='" + cost.CFID + "'>" + cost.CFDESC + "</option>";
                html = html + "<tr class='item-row'><Td> <button href='#'   type='button' onclick='del_factor(this)' class='btn btn-danger dfact btn-xs'><i class=glyphicon-minus></i> </button>  </td> <td><input type=text name=TAX id='TAX'  class='hide TAX' value='" + cost.CFID + "'><input type=text name=CFDESC id='CFDESC' class='hidden CFDESC' value='" + cost.CFDESC + "'>" + cost.CFDESC + "";
                html = html + "</td> <tD class='col-lg-1' > <select id='CFTYPE' name='CFTYPE' class='CFTYPE' onchange='totalonchange(this)'><option value='0' >Value </option> <option value='1' " + expr + "  >  %</option> </select></td> <td class='col-lg-1'><input type='text' id='DEDNOS' class='DEDNOS' name='DEDNOS' value='0' onchange='totalonchange(this)'  style='width:50px'></td><td class='col-lg-1' > <input onchange='totalonchange(this)' type=text value='" + cost.CFEXPR + "' class='CFEXPR' name='CFEXPR' id='CFEXPR'> </td><td><select onchange='totalonchange(this)' class='CFMODE' id='CFMODE' name='CFMODE'> <option value='0' >  +</option><option value='1' " + mod + " >-</option> </select><input type='text' id='DORDRID'   value='" + cost.DORDRID + "' class='DORDRID' style='display:none'  name='DORDRID' >  <input  type=text value='0' style='display:none' name=TRANMFID id='TRANMFID' class='TRANMFID' >";
                html = html + "<input  type=text value='0' style='display:none' name=DEDORDR id='DEDORDR' class='DEDORDR' ><input  type=text value='0' style='display:none' name=TMPCFVAL id='TMPCFVAL' class='TMPCFVAL' ></td><td><input  type=text value='' name=CFAMOUNT id='CFAMOUNT' class='CFAMOUNT' readonly='readonly'> </td>  </TD></tr>";

                i++;

                //do something with cust
            }
            return html;


        }

        //gst def costfactor
        public string gstdefCostFactor()
        {


            //DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(5,6,77,65,90) and DISPSTATUS=0 order by CFID DESC");
            DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(6,77,65,69,90,126,127) and DISPSTATUS=0 order by CFID DESC");


            string html = "";
            //<select id='TAX' class='TAX' name='TAX' onchange='sel_text(this,&quot;CFDESC&quot;);'  > ";

            string first = "";
            string f_order = "";
            string f_expr = "";
            string mod = "";
            string expr = "";
            string first_id = "0";

            int i = 0;

            foreach (var cost in data)
            {

                first_id = cost.CFID.ToString();




                //if (i == 0)
                //{
                first = cost.CFDESC;
                f_order = cost.DORDRID.ToString();
                f_expr = cost.CFEXPR.ToString();
                if (cost.CFMODE != 0)
                    mod = "selected";
                if (cost.CFTYPE != 0)
                    expr = "selected";
                else expr = "";
                //   }

                // html = html + "<option value='" + cost.CFID + "'>" + cost.CFDESC + "</option>";
                html = html + "<tr class='item-row'><Td> <button href='#'   type='button' onclick='del_factor(this)' class='btn btn-danger dfact btn-xs'><i class=glyphicon-minus></i> </button>  </td> <td><input type=text name=TAX id='TAX'  class='hide TAX' value='" + cost.CFID + "'><input type=text name=CFDESC id='CFDESC' class='hide CFDESC' value='" + cost.CFDESC + "'>" + cost.CFDESC + "";
                html = html + "</td> <tD class='col-lg-1' > <select id='CFTYPE' name='CFTYPE' class='CFTYPE' onchange='totalonchange(this)'> <option value='0' >Value </option><option value='1' " + expr + "  >  %</option> </select></td> <td class='col-lg-1'><input type='text' id='DEDNOS' class='DEDNOS' name='DEDNOS' onchange='totalonchange(this)' value='0' style='width:50px'></td><td class='col-lg-1' > <input onchange='totalonchange(this)' type=text value='" + cost.CFEXPR + "' class='CFEXPR' name='CFEXPR' id='CFEXPR'> </td><td><select onchange='totalonchange(this)' class='CFMODE' id='CFMODE' name='CFMODE'> <option value='0' >  +</option><option value='1' " + mod + " >-</option> </select><input type='text' id='DORDRID'   value='" + cost.DORDRID + "' class='DORDRID' style='display:none'  name='DORDRID' >  <input  type=text value='0' style='display:none' name=TRANMFID id='TRANMFID' class='TRANMFID' >";
                html = html + "<input  type=text value='0' style='display:none' name=DEDORDR id='DEDORDR' class='DEDORDR' ><input  type=text value='0' style='display:none' name=TMPCFVAL id='TMPCFVAL' class='TMPCFVAL' ></td><td><input  type=text value='' name=CFAMOUNT id='CFAMOUNT' class='CFAMOUNT' readonly='readonly'> </td>  </TD></tr>";

                i++;

                //do something with cust
            }
            DbSqlQuery<CostFactorMaster> data1 = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(4) and DISPSTATUS=0 ");
            foreach (var cost in data1)
            {

                first_id = cost.CFID.ToString();




                //if (i == 0)
                //{
                first = cost.CFDESC;
                f_order = cost.DORDRID.ToString();
                f_expr = cost.CFEXPR.ToString();
                if (cost.CFMODE != 0)
                    mod = "selected";
                if (cost.CFTYPE != 0)
                    expr = "selected";
                else expr = "";
                //   }

                // html = html + "<option value='" + cost.CFID + "'>" + cost.CFDESC + "</option>";
                html = html + "<tr class='item-row'><Td> <button href='#'   type='button' onclick='del_factor(this)' class='btn btn-danger dfact btn-xs'><i class=glyphicon-minus></i> </button>  </td> <td><input type=text name=TAX id='TAX'  class='hide TAX' value='" + cost.CFID + "'><input type=text name=CFDESC id='CFDESC' class='hide CFDESC' value='" + cost.CFDESC + "'>" + cost.CFDESC + "";
                html = html + "</td> <tD class='col-lg-1' > <select id='CFTYPE' name='CFTYPE' class='CFTYPE' onchange='totalonchange(this)'><option value='0'  >Value </option>  <option value='1'  " + expr + " >  %</option></select></td> <td class='col-lg-1'><input type='text' id='DEDNOS' class='DEDNOS' name='DEDNOS' value='0' onchange='totalonchange(this)' style='width:50px'></td><td class='col-lg-1' > <input onchange='totalonchange(this)' type=text value='" + cost.CFEXPR + "' class='CFEXPR' name='CFEXPR' id='CFEXPR'> </td><td><select onchange='totalonchange(this)' class='CFMODE' id='CFMODE' name='CFMODE'> <option value='0' >  +</option><option value='1' " + mod + " >-</option> </select><input type='text' id='DORDRID'   value='" + cost.DORDRID + "' class='DORDRID' style='display:none'  name='DORDRID' >  <input  type=text value='0' style='display:none' name=TRANMFID id='TRANMFID' class='TRANMFID' >";
                html = html + "<input  type=text value='0' style='display:none' name=DEDORDR id='DEDORDR' class='DEDORDR' ><input  type=text value='0' style='display:none' name=TMPCFVAL id='TMPCFVAL' class='TMPCFVAL' ></td><td><input  type=text value='' name=CFAMOUNT id='CFAMOUNT' class='CFAMOUNT' readonly='readonly'> </td>  </TD></tr>";


                //do something with cust
            }
            //DbSqlQuery<CostFactorMaster> data2 = context.costfactormasters.SqlQuery("select * from costfactormaster  where DISPSTATUS=0 and CFID  in(2,96,97) ");
            //foreach (var cost in data2)
            //{

            //    first_id = cost.CFID.ToString();




            //    //if (i == 0)
            //    //{
            //    first = cost.CFDESC;
            //    f_order = cost.DORDRID.ToString();
            //    f_expr = cost.CFEXPR.ToString();
            //    if (cost.CFMODE != 0)
            //        mod = "selected";
            //    if (cost.CFTYPE != 0)
            //        expr = "selected";
            //    else expr = "";
            //    //   }

            //    // html = html + "<option value='" + cost.CFID + "'>" + cost.CFDESC + "</option>";
            //    html = html + "<tr class='item-row'><Td> <button href='#'   type='button' onclick='del_factor(this)' class='btn btn-danger dfact btn-xs'><i class=glyphicon-minus></i> </button>  </td> <td><input type=text name=TAX id='TAX'  class='hide TAX' value='" + cost.CFID + "'><input type=text name=CFDESC id='CFDESC' class='hidden CFDESC' value='" + cost.CFDESC + "'>" + cost.CFDESC + "";
            //    html = html + "</td> <tD class='col-lg-1' > <select id='CFTYPE' name='CFTYPE' class='CFTYPE' onchange='totalonchange(this)'><option value='0' >Value </option> <option value='1' " + expr + "  >  %</option> </select></td> <td class='col-lg-1'><input type='text' id='DEDNOS' class='DEDNOS' name='DEDNOS' value='0' onchange='totalonchange(this)'  style='width:50px'></td><td class='col-lg-1' > <input onchange='totalonchange(this)' type=text value='" + cost.CFEXPR + "' class='CFEXPR' name='CFEXPR' id='CFEXPR'> </td><td><select onchange='totalonchange(this)' class='CFMODE' id='CFMODE' name='CFMODE'> <option value='0' >  +</option><option value='1' " + mod + " >-</option> </select><input type='text' id='DORDRID'   value='" + cost.DORDRID + "' class='DORDRID' style='display:none'  name='DORDRID' >  <input  type=text value='0' style='display:none' name=TRANMFID id='TRANMFID' class='TRANMFID' >";
            //    html = html + "<input  type=text value='0' style='display:none' name=DEDORDR id='DEDORDR' class='DEDORDR' ><input  type=text value='0' style='display:none' name=TMPCFVAL id='TMPCFVAL' class='TMPCFVAL' ></td><td><input  type=text value='' name=CFAMOUNT id='CFAMOUNT' class='CFAMOUNT' readonly='readonly'> </td>  </TD></tr>";

            //    i++;

            //    //do something with cust
            //}
            return html;


        }
        //--------Autocomplete CHA Name
        public JsonResult AutoCha(string term)
        {
            var result = (from r in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public JsonResult DODetail(int id)
        {
            var query = context.Database.SqlQuery<pr_Import_DO_Invoice_IGMNO_Grid_Assgn_Result>("EXEC pr_Import_DO_Invoice_IGMNO_Grid_Assgn @PDONO=" + id).ToList();

            return Json(query, JsonRequestBehavior.AllowGet);
        }
        public string Detail(string id)
        {
            int tariffmid = 0;
            var param = id.Split('~');
            var igmno = (param[0]); var gplno = param[1]; var TRANMID = Convert.ToInt32(param[2]);
            if (param[3] != "") { tariffmid = Convert.ToInt32((param[3])); } else { tariffmid = 0; };

            var query = context.Database.SqlQuery<pr_Import_Invoice_IGMNO_Grid_Assgn_Result>("EXEC pr_Import_Invoice_IGMNO_Grid_Assgn @PIGMNO='" + igmno + "',@PLNO='" + gplno + "',@PTRANMID=" + TRANMID).ToList();

            var tabl = "";
            var count = 0;

            foreach (var rslt in query)
            {

                if (rslt.GODATE == null) rslt.GODATE = DateTime.Now.Date;
                if (rslt.GOTIME == null) rslt.GOTIME = DateTime.Now;
                if (tariffmid > 6)
                {
                    var st = ""; var bt = "";

                    if (rslt.TRANDID != 0) { st = "checked"; bt = "true"; }
                    else { bt = "false"; st = ""; }
                                        
                    tabl = tabl + "<tr><td class='hidden1'><input type=checkbox id=STFDIDS class=STFDIDS name=STFDIDS checked='"+ st + "' onchange=total() style='width:30px'>";
                    tabl = tabl + "<input type=text id=boolSTFDIDS class='hide boolSTFDIDS' name=boolSTFDIDS value='" + bt + "'></td><td>";
                    tabl = tabl + "<input type=text id=TRANDREFNO value=" + rslt.BOENO + "  class='TRANDREFNO' readonly='readonly' name=TRANDREFNO style='width:56px'></td>";
                    tabl = tabl + "<td class='col-lg-0'><input type='text' id='TRANDREFNAME' value='" + rslt.CONTNRNO + "'  class='TRANDREFNAME' readonly='readonly' name='TRANDREFNAME' ></td>";
                    tabl = tabl + "<td><input type=text id=SIZE value='" + rslt.CONTNRSDESC + "' class=SIZE name=SIZE style='width:40px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANIDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANIDATE name=TRANIDATE style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANSDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANSDATE name=TRANSDATE readonly='readonly' style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id='TRANEDATE' value='" + rslt.GODATE.Value.ToString("dd/MM/yyyy") + "' class='TRANEDATE datepicker' readonly='readonly' name='TRANEDATE' style='width:70px' onchange='calculation()'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDWGHT value=" + rslt.WGHT + " class=TRANDWGHT name=TRANDWGHT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDSAMT value='0' class=TRANDSAMT name=TRANDSAMT style='width:70px' readonly='readonly'>";
                    tabl = tabl + "<input type=text id=TRAND_COVID_DISC_AMT value='0' class='TRAND_COVID_DISC_AMT hide' name=TRAND_COVID_DISC_AMT style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDHAMT value='0' class=TRANDHAMT name=TRANDHAMT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td class='hide'><input type=text value='0' id=TRANDEAMT class=TRANDEAMT name=TRANDEAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDFAMT class=TRANDFAMT name=TRANDFAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDAAMT value='0' class=TRANDAAMT name=TRANDAAMT   readonly=readonly style='width:70px'></td> ";
                    tabl = tabl + "<td><input type=text id=TRANDADONAMT value='0' class=TRANDADONAMT name=TRANDADONAMT   style='width:70px' onchange='total()'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDNAMT class=TRANDNAMT name=TRANDNAMT readonly='readonly' style='width:70px'> ";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDVDATE class=TRANDVDATE name=TRANDVDATE value='" + rslt.DODATE.Value.ToString("dd/MM/yyyy") + "'>";
                    tabl = tabl + "<input type=text id=F_DPAIDNO value='" + rslt.DPAIDNO + "'  class='F_DPAIDNO' name=F_DPAIDNO >";
                    tabl = tabl + "<input type=text id=F_DPAIDAMT value=" + rslt.DPAIDAMT + "  class=F_DPAIDAMT name=F_DPAIDAMT ></td>  ";
                    tabl = tabl + "<td class=hide><input type=text id=CONTNRTID value=0  class=CONTNRTID name=CONTNRTID >";
                    tabl = tabl + "<input type=text id=CONTNRSID value=" + rslt.CONTNRSID + "  class=CONTNRSID name=CONTNRSID >";
                    tabl = tabl + "<input type=text id=TRANDREFID value=" + rslt.GIDID + "  class=TRANDREFID name=TRANDREFID >";
                    tabl = tabl + "<input type=text id=BILLEDID value=" + rslt.BILLEDID + "  class=BILLEDID name=BILLEDID >";
                    tabl = tabl + "<input type=text id=BILLEMID value=" + rslt.BILLEMID + "  class=BILLEMID name=BILLEMID >";
                    tabl = tabl + "<input type=text id=F_TARIFFMID value=" + rslt.TARIFFMID + "  class=F_TARIFFMID name=F_TARIFFMID >";
                    tabl = tabl + "<input type=text id=F_TRANDOTYPE value=" + rslt.TRANDOTYPE + "  class=F_TRANDOTYPE name=F_TRANDOTYPE >";
                    tabl = tabl + "<input type=text id=F_TRANBTYPE value='" + rslt.TRANBTYPE + "'  class=F_TRANBTYPE name=F_TRANBTYPE>";
                    tabl = tabl + "<input type=text id=F_CHANAME value='" + rslt.TRANREFNAME + "'  class=F_CHANAME name=F_CHANAME >";
                    tabl = tabl + "<input type=text id=F_STMRNAME value='" + rslt.STMRNAME + "'  class=F_STMRNAME name=F_STMRNAME >";
                    tabl = tabl + "<input type=text id=F_CHAID value=" + rslt.TRANREFID + "  class=F_CHAID name=F_CHAID >";
                    tabl = tabl + "<input type=text id=F_STMRID value=" + rslt.STMRID + "  class=F_STMRID name=F_STMRID ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDID value=0  class=TRANDID name=TRANDID ></td>";
                    tabl = tabl + "<td class='hide'><input type=text id=days value=0  class=days name=days ><input type=text id=NOD value='" + rslt.NOD + "'  class=NOD name=NOD >";
                    tabl = tabl + "<input type=text id=TRANDRATE value='" + rslt.TRANDRATE + "'  class=TRANDRATE name=TRANDRATE ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RAMT7 value=" + Convert.ToDecimal(rslt.RAMT7) + "  class=RAMT7 name=RAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT1 value=" + Convert.ToDecimal(rslt.RAMT1) + "  class=RAMT1 name=RAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT2 value=" + Convert.ToDecimal(rslt.RAMT2) + "  class=RAMT2 name=RAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT3 value=" + Convert.ToDecimal(rslt.RAMT3) + "  class=RAMT3 name=RAMT3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT4 value=" + Convert.ToDecimal(rslt.RAMT4) + "  class=RAMT4 name=RAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT5 value=" + Convert.ToDecimal(rslt.RAMT5) + "  class=RAMT5 name=RAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT6 value=" + Convert.ToDecimal(rslt.RAMT6) + " class=RAMT6 name=RAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=SLABMIN6 value='0'  class=SLABMIN6 name=SLABMIN6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX6 value='0'  class=SLABMAX6 name=SLABMAX6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN value='0'  class=SLABMIN name=SLABMIN style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX value='0'  class=SLABMAX name=SLABMAX style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN1 value='0'  class=SLABMIN1 name=SLABMIN1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX1 value='0'  class=SLABMAX1 name=SLABMAX1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN2 value='0'  class=SLABMIN2 name=SLABMIN2 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX2 value='0'  class=SLABMAX2 name=SLABMAX2 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN3 value='0'  class=SLABMIN3 name=SLABMIN3 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX3 value='0'  class=SLABMAX3 name=SLABMAX3 style='display:none' >  ";
                    tabl = tabl + "<input type=text id=SLABMIN4 value='0'  class=SLABMIN4 name=SLABMIN4 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX4 value='0'  class=SLABMAX4 name=SLABMAX4 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN5 value='0'  class=SLABMIN5 name=SLABMIN5 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX5 value='0'  class=SLABMAX5 name=SLABMAX5 style='display:none' > </td>";
                    tabl = tabl + "<td class=hide> <input type=text id=RCAMT7 value=value=" + Convert.ToDecimal(rslt.RCAMT7) + "  class=RCAMT7 name=RCAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT1 value=value=" + Convert.ToDecimal(rslt.RCAMT1) + "  class=RCAMT1 name=RCAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT2 value=" + Convert.ToDecimal(rslt.RCAMT2) + "  class=RCAMT2 name=RCAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT3 value=" + Convert.ToDecimal(rslt.RCAMT3) + "  class=RCAMT3 name=RCAMT3 style='display:none'>";
                    tabl = tabl + "<input type=text id=RCAMT4 value=" + Convert.ToDecimal(rslt.RCAMT4) + "  class=RCAMT4 name=RCAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT5 value=" + Convert.ToDecimal(rslt.RCAMT5) + "  class=RCAMT5 name=RCAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT6 value=" + Convert.ToDecimal(rslt.RCAMT6) + "  class=RCAMT6 name=RCAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RCOL7 value=" + Convert.ToDecimal(rslt.RCOL7) + "  class=RCOL7 name=RCOL7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL1 value=" + Convert.ToDecimal(rslt.RCOL1) + "  class=RCOL1 name=RCOL1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL2 value=" + Convert.ToDecimal(rslt.RCOL2) + " class=RCOL2 name=RCOL2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL3 value=" + Convert.ToDecimal(rslt.RCOL3) + "  class=RCOL3 name=RCOL3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL4 value=" + Convert.ToDecimal(rslt.RCOL4) + "  class=RCOL4 name=RCOL4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL5 value=" + Convert.ToDecimal(rslt.RCOL5) + " class=RCOL5 name=RCOL5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL6 value=" + Convert.ToDecimal(rslt.RCOL6) + "  class=RCOL6 name=RCOL6 style='display:none' ></td>";
                    tabl = tabl + "<td class='hide'><input type='text' value=" + rslt.GPAAMT + " id='GPAAMT' class='GPAAMT' name='GPAAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPEAMT + " id='GPEAMT' class='GPEAMT' name='GPEAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPETYPE + " id='GPETYPE' class='GPETYPE' name='GPETYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSTYPE + " id='GPCSTYPE' class='GPCSTYPE' name='GPCSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSAMT + " id='GPCSAMT' class='GPCSAMT' name='GPCSAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBTYPE + " id='GPLBTYPE' class='GPLBTYPE' name='GPLBTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBAMT + " id='GPLBAMT' class='GPLBAMT' name='GPLBAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSTYPE + " id='GPSTYPE' class='GPSTYPE' name='GPSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPWTYPE + " id='GPWTYPE' class='GPWTYPE' name='GPWTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSCNTYPE + " id='GPSCNTYPE' class='GPSCNTYPE' name='GPSCNTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.PRDTGID + " id='F_PRDTGID' class='F_PRDTGID' name='F_PRDTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.CUSTGID + " id='F_CUSTGID' class='F_CUSTGID' name='F_CUSTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.STATETYPE + " id='F_STATETYPE' class='F_STATETYPE' name='F_STATETYPE'></td></tr>";

                }
                else
                {
                    var st = ""; var bt = "";

                    if (rslt.TRANDID != 0) { st = "checked"; bt = "true"; }
                    else { bt = "false"; st = ""; }


                    tabl = tabl + "<tr><td class='hidden1'><input type=checkbox id=STFDIDS class=STFDIDS name=STFDIDS value='" + st + "' checked='"+bt+"' onchange=total() style='width:30px'>";
                    tabl = tabl + "<input type=text id=boolSTFDIDS class='hide boolSTFDIDS' name=boolSTFDIDS value='" + bt + "'></td><td>";
                    tabl = tabl + "<input type=text id=TRANDREFNO value=" + rslt.BOENO + "  class='TRANDREFNO' readonly='readonly' name=TRANDREFNO style='width:56px'></td>";
                    tabl = tabl + "<td class='col-lg-0'><input type='text' id='TRANDREFNAME' value='" + rslt.CONTNRNO + "'  class='TRANDREFNAME' readonly='readonly' name='TRANDREFNAME' ></td>";
                    tabl = tabl + "<td><input type=text id=SIZE value='" + rslt.CONTNRSDESC + "' class=SIZE name=SIZE style='width:40px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANIDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANIDATE name=TRANIDATE style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANSDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANSDATE name=TRANSDATE readonly='readonly' style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id='TRANEDATE' value='" + rslt.GODATE.Value.ToString("dd/MM/yyyy") + "' class='TRANEDATE datepicker' readonly='readonly' name='TRANEDATE' style='width:70px' onchange='calculation()'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDWGHT value=" + rslt.WGHT + " class=TRANDWGHT name=TRANDWGHT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDSAMT value='0' class=TRANDSAMT name=TRANDSAMT style='width:70px' readonly='readonly'>";
                    tabl = tabl + "<input type=text id=TRAND_COVID_DISC_AMT value='0' class='TRAND_COVID_DISC_AMT hide' name=TRAND_COVID_DISC_AMT style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDHAMT value='0' class=TRANDHAMT name=TRANDHAMT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td class='hide'><input type=text value='0' id=TRANDEAMT class=TRANDEAMT name=TRANDEAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDFAMT class=TRANDFAMT name=TRANDFAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDAAMT value='0' class=TRANDAAMT name=TRANDAAMT   readonly=readonly style='width:70px'></td> ";
                    tabl = tabl + "<td><input type=text id=TRANDADONAMT value='0' class=TRANDADONAMT name=TRANDADONAMT   style='width:70px' onchange='total()'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDNAMT class=TRANDNAMT name=TRANDNAMT readonly='readonly' style='width:70px'> ";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDVDATE class=TRANDVDATE name=TRANDVDATE value='" + rslt.DODATE.Value.ToString("dd/MM/yyyy") + "'>";
                    tabl = tabl + "<input type=text id=F_DPAIDNO value='" + rslt.DPAIDNO + "'  class='F_DPAIDNO' name=F_DPAIDNO >";
                    tabl = tabl + "<input type=text id=F_DPAIDAMT value=" + rslt.DPAIDAMT + "  class=F_DPAIDAMT name=F_DPAIDAMT ></td>  ";
                    tabl = tabl + "<td class=hide><input type=text id=CONTNRTID value=0  class=CONTNRTID name=CONTNRTID >";
                    tabl = tabl + "<input type=text id=CONTNRSID value=" + rslt.CONTNRSID + "  class=CONTNRSID name=CONTNRSID >";
                    tabl = tabl + "<input type=text id=TRANDREFID value=" + rslt.GIDID + "  class=TRANDREFID name=TRANDREFID >";
                    tabl = tabl + "<input type=text id=BILLEDID value=" + rslt.BILLEDID + "  class=BILLEDID name=BILLEDID >";
                    tabl = tabl + "<input type=text id=BILLEMID value=" + rslt.BILLEMID + "  class=BILLEMID name=BILLEMID >";
                    tabl = tabl + "<input type=text id=F_TARIFFMID value=" + rslt.TARIFFMID + "  class=F_TARIFFMID name=F_TARIFFMID >";
                    tabl = tabl + "<input type=text id=F_TRANDOTYPE value=" + rslt.TRANDOTYPE + "  class=F_TRANDOTYPE name=F_TRANDOTYPE >";
                    tabl = tabl + "<input type=text id=F_TRANBTYPE value='" + rslt.TRANBTYPE + "'  class=F_TRANBTYPE name=F_TRANBTYPE>";
                    tabl = tabl + "<input type=text id=F_CHANAME value='" + rslt.TRANREFNAME + "'  class=F_CHANAME name=F_CHANAME >";
                    tabl = tabl + "<input type=text id=F_STMRNAME value='" + rslt.STMRNAME + "'  class=F_STMRNAME name=F_STMRNAME >";
                    tabl = tabl + "<input type=text id=F_CHAID value=" + rslt.TRANREFID + "  class=F_CHAID name=F_CHAID >";
                    tabl = tabl + "<input type=text id=F_STMRID value=" + rslt.STMRID + "  class=F_STMRID name=F_STMRID ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDID value=0  class=TRANDID name=TRANDID ></td>";
                    tabl = tabl + "<td class='hide'><input type=text id=days value=0  class=days name=days ><input type=text id=NOD value='" + rslt.NOD + "'  class=NOD name=NOD >";
                    tabl = tabl + "<input type=text id=TRANDRATE value='" + rslt.TRANDRATE + "'  class=TRANDRATE name=TRANDRATE ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RAMT7 value=" + Convert.ToDecimal(rslt.RAMT7) + "  class=RAMT7 name=RAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT1 value=" + Convert.ToDecimal(rslt.RAMT1) + "  class=RAMT1 name=RAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT2 value=" + Convert.ToDecimal(rslt.RAMT2) + "  class=RAMT2 name=RAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT3 value=" + Convert.ToDecimal(rslt.RAMT3) + "  class=RAMT3 name=RAMT3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT4 value=" + Convert.ToDecimal(rslt.RAMT4) + "  class=RAMT4 name=RAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT5 value=" + Convert.ToDecimal(rslt.RAMT5) + "  class=RAMT5 name=RAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT6 value=" + Convert.ToDecimal(rslt.RAMT6) + " class=RAMT6 name=RAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=SLABMIN6 value='0'  class=SLABMIN6 name=SLABMIN6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX6 value='0'  class=SLABMAX6 name=SLABMAX6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN value='0'  class=SLABMIN name=SLABMIN style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX value='0'  class=SLABMAX name=SLABMAX style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN1 value='0'  class=SLABMIN1 name=SLABMIN1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX1 value='0'  class=SLABMAX1 name=SLABMAX1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN2 value='0'  class=SLABMIN2 name=SLABMIN2 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX2 value='0'  class=SLABMAX2 name=SLABMAX2 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN3 value='0'  class=SLABMIN3 name=SLABMIN3 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX3 value='0'  class=SLABMAX3 name=SLABMAX3 style='display:none' >  ";
                    tabl = tabl + "<input type=text id=SLABMIN4 value='0'  class=SLABMIN4 name=SLABMIN4 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX4 value='0'  class=SLABMAX4 name=SLABMAX4 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN5 value='0'  class=SLABMIN5 name=SLABMIN5 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX5 value='0'  class=SLABMAX5 name=SLABMAX5 style='display:none' > </td>";
                    tabl = tabl + "<td class=hide> <input type=text id=RCAMT7 value=value=" + Convert.ToDecimal(rslt.RCAMT7) + "  class=RCAMT7 name=RCAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT1 value=value=" + Convert.ToDecimal(rslt.RCAMT1) + "  class=RCAMT1 name=RCAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT2 value=" + Convert.ToDecimal(rslt.RCAMT2) + "  class=RCAMT2 name=RCAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT3 value=" + Convert.ToDecimal(rslt.RCAMT3) + "  class=RCAMT3 name=RCAMT3 style='display:none'>";
                    tabl = tabl + "<input type=text id=RCAMT4 value=" + Convert.ToDecimal(rslt.RCAMT4) + "  class=RCAMT4 name=RCAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT5 value=" + Convert.ToDecimal(rslt.RCAMT5) + "  class=RCAMT5 name=RCAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT6 value=" + Convert.ToDecimal(rslt.RCAMT6) + "  class=RCAMT6 name=RCAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RCOL7 value=" + Convert.ToDecimal(rslt.RCOL7) + "  class=RCOL7 name=RCOL7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL1 value=" + Convert.ToDecimal(rslt.RCOL1) + "  class=RCOL1 name=RCOL1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL2 value=" + Convert.ToDecimal(rslt.RCOL2) + " class=RCOL2 name=RCOL2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL3 value=" + Convert.ToDecimal(rslt.RCOL3) + "  class=RCOL3 name=RCOL3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL4 value=" + Convert.ToDecimal(rslt.RCOL4) + "  class=RCOL4 name=RCOL4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL5 value=" + Convert.ToDecimal(rslt.RCOL5) + " class=RCOL5 name=RCOL5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL6 value=" + Convert.ToDecimal(rslt.RCOL6) + "  class=RCOL6 name=RCOL6 style='display:none' ></td>";
                    tabl = tabl + "<td class='hide'><input type='text' value=" + rslt.GPAAMT + " id='GPAAMT' class='GPAAMT' name='GPAAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPEAMT + " id='GPEAMT' class='GPEAMT' name='GPEAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPETYPE + " id='GPETYPE' class='GPETYPE' name='GPETYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSTYPE + " id='GPCSTYPE' class='GPCSTYPE' name='GPCSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSAMT + " id='GPCSAMT' class='GPCSAMT' name='GPCSAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBTYPE + " id='GPLBTYPE' class='GPLBTYPE' name='GPLBTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBAMT + " id='GPLBAMT' class='GPLBAMT' name='GPLBAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSTYPE + " id='GPSTYPE' class='GPSTYPE' name='GPSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPWTYPE + " id='GPWTYPE' class='GPWTYPE' name='GPWTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSCNTYPE + " id='GPSCNTYPE' class='GPSCNTYPE' name='GPSCNTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.PRDTGID + " id='F_PRDTGID' class='F_PRDTGID' name='F_PRDTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.CUSTGID + " id='F_CUSTGID' class='F_CUSTGID' name='F_CUSTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.STATETYPE + " id='F_STATETYPE' class='F_STATETYPE' name='F_STATETYPE'></td></tr>";
                }

                count++;
            }
            tabl = tabl + "</Table>";

            return tabl;

        }


        public string Detail2(string id)
        {
            int tariffmid = 0;int tranbtype = 0; var igmno = ""; var gplno = ""; var TRANMID = 0;
            var param = id.Split('~');
            //if (!id.Contains("undefined") && !id.Contains("null"))
            //{
                igmno = (param[0]); gplno = param[1]; TRANMID = Convert.ToInt32(param[2]);
                if (param[3] != "") { tariffmid = Convert.ToInt32((param[3])); } else { tariffmid = 0; }
                if (param[4] != "" && param[4] != "null") { tranbtype = Convert.ToInt32((param[4])); } else { tranbtype = 0; }
            //}

            var query = context.Database.SqlQuery<pr_Import_Invoice_IGMNO_Grid_Assgn_Result>("EXEC pr_Import_Invoice_IGMNO_Grid_Assgn @PIGMNO='" + igmno + "',@PLNO='" + gplno + "',@PTRANMID= " + TRANMID + ",@PTRANBTYPE= " + tranbtype).ToList();

            var tabl = "";
            var count = 0;

            foreach (var rslt in query)
            {

                if (rslt.GODATE == null) rslt.GODATE = DateTime.Now.Date;
                if (rslt.GOTIME == null) rslt.GOTIME = DateTime.Now;
                if (tariffmid > 6)
                {
                    var st = ""; var bt = "";

                    if (rslt.TRANDID != 0) { st = "checked"; bt = "true"; }
                    else { bt = "false"; st = ""; }

                    //if (rslt.TRANDID == 0) { st = "checked"; bt = "true"; }
                    //else { bt = "false"; st = ""; }


                    tabl = tabl + "<tr><td class='hidden1'><input type=checkbox id=STFDIDS class=STFDIDS name=STFDIDS checked='" + st + "' onchange=total() style='width:30px'>";
                    tabl = tabl + "<input type=text id=boolSTFDIDS class='hide boolSTFDIDS' name=boolSTFDIDS value='" + bt + "'></td><td>";
                    tabl = tabl + "<input type=text id=TRANDREFNO value=" + rslt.BOENO + "  class='TRANDREFNO' readonly='readonly' name=TRANDREFNO style='width:56px'></td>";
                    tabl = tabl + "<td class='col-lg-0'><input type='text' id='TRANDREFNAME' value='" + rslt.CONTNRNO + "'  class='TRANDREFNAME' readonly='readonly' name='TRANDREFNAME' ></td>";
                    tabl = tabl + "<td><input type=text id=SIZE value='" + rslt.CONTNRSDESC + "' class=SIZE name=SIZE style='width:40px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANIDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANIDATE name=TRANIDATE style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANSDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANSDATE name=TRANSDATE readonly='readonly' style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id='TRANEDATE' value='" + rslt.GODATE.Value.ToString("dd/MM/yyyy") + "' class='TRANEDATE datepicker' readonly='readonly' name='TRANEDATE' style='width:70px' onchange='calculation()'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDWGHT value=" + rslt.WGHT + " class=TRANDWGHT name=TRANDWGHT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDSAMT value='0' class=TRANDSAMT name=TRANDSAMT style='width:70px' readonly='readonly'>";
                    tabl = tabl + "<input type=text id=TRAND_COVID_DISC_AMT value='0' class='TRAND_COVID_DISC_AMT hide' name=TRAND_COVID_DISC_AMT style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDHAMT value='0' class=TRANDHAMT name=TRANDHAMT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td class='hide'><input type=text value='0' id=TRANDEAMT class=TRANDEAMT name=TRANDEAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDFAMT class=TRANDFAMT name=TRANDFAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDAAMT value='0' class=TRANDAAMT name=TRANDAAMT   readonly=readonly style='width:70px'></td> ";
                    tabl = tabl + "<td><input type=text id=TRANDADONAMT value='0' class=TRANDADONAMT name=TRANDADONAMT   style='width:70px' onchange='total()'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDNAMT class=TRANDNAMT name=TRANDNAMT readonly='readonly' style='width:70px'> ";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDVDATE class=TRANDVDATE name=TRANDVDATE value='" + rslt.DODATE.Value.ToString("dd/MM/yyyy") + "'>";
                    tabl = tabl + "<input type=text id=F_DPAIDNO value='" + rslt.DPAIDNO + "'  class='F_DPAIDNO' name=F_DPAIDNO >";
                    tabl = tabl + "<input type=text id=F_DPAIDAMT value=" + rslt.DPAIDAMT + "  class=F_DPAIDAMT name=F_DPAIDAMT ></td>  ";
                    tabl = tabl + "<td class=hide><input type=text id=CONTNRTID value=0  class=CONTNRTID name=CONTNRTID >";
                    tabl = tabl + "<input type=text id=CONTNRSID value=" + rslt.CONTNRSID + "  class=CONTNRSID name=CONTNRSID >";
                    tabl = tabl + "<input type=text id=TRANDREFID value=" + rslt.GIDID + "  class=TRANDREFID name=TRANDREFID >";
                    tabl = tabl + "<input type=text id=BILLEDID value=" + rslt.BILLEDID + "  class=BILLEDID name=BILLEDID >";
                    tabl = tabl + "<input type=text id=BILLEMID value=" + rslt.BILLEMID + "  class=BILLEMID name=BILLEMID >";
                    tabl = tabl + "<input type=text id=F_TARIFFMID value=" + rslt.TARIFFMID + "  class=F_TARIFFMID name=F_TARIFFMID >";
                    tabl = tabl + "<input type=text id=F_TRANDOTYPE value=" + rslt.TRANDOTYPE + "  class=F_TRANDOTYPE name=F_TRANDOTYPE >";
                    tabl = tabl + "<input type=text id=F_TRANBTYPE value='" + rslt.TRANBTYPE + "'  class=F_TRANBTYPE name=F_TRANBTYPE>";
                    tabl = tabl + "<input type=text id=F_CHANAME value='" + rslt.TRANREFNAME + "'  class=F_CHANAME name=F_CHANAME >";
                    tabl = tabl + "<input type=text id=F_STMRNAME value='" + rslt.STMRNAME + "'  class=F_STMRNAME name=F_STMRNAME >";
                    tabl = tabl + "<input type=text id=F_CHAID value=" + rslt.TRANREFID + "  class=F_CHAID name=F_CHAID >";
                    tabl = tabl + "<input type=text id=F_STMRID value=" + rslt.STMRID + "  class=F_STMRID name=F_STMRID ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDID value=0  class=TRANDID name=TRANDID ></td>";
                    tabl = tabl + "<td class='hide'><input type=text id=days value=0  class=days name=days ><input type=text id=NOD value='" + rslt.NOD + "'  class=NOD name=NOD >";
                    tabl = tabl + "<input type=text id=TRANDRATE value='" + rslt.TRANDRATE + "'  class=TRANDRATE name=TRANDRATE ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RAMT7 value=" + Convert.ToDecimal(rslt.RAMT7) + "  class=RAMT7 name=RAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT1 value=" + Convert.ToDecimal(rslt.RAMT1) + "  class=RAMT1 name=RAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT2 value=" + Convert.ToDecimal(rslt.RAMT2) + "  class=RAMT2 name=RAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT3 value=" + Convert.ToDecimal(rslt.RAMT3) + "  class=RAMT3 name=RAMT3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT4 value=" + Convert.ToDecimal(rslt.RAMT4) + "  class=RAMT4 name=RAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT5 value=" + Convert.ToDecimal(rslt.RAMT5) + "  class=RAMT5 name=RAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT6 value=" + Convert.ToDecimal(rslt.RAMT6) + " class=RAMT6 name=RAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=SLABMIN6 value='0'  class=SLABMIN6 name=SLABMIN6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX6 value='0'  class=SLABMAX6 name=SLABMAX6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN value='0'  class=SLABMIN name=SLABMIN style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX value='0'  class=SLABMAX name=SLABMAX style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN1 value='0'  class=SLABMIN1 name=SLABMIN1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX1 value='0'  class=SLABMAX1 name=SLABMAX1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN2 value='0'  class=SLABMIN2 name=SLABMIN2 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX2 value='0'  class=SLABMAX2 name=SLABMAX2 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN3 value='0'  class=SLABMIN3 name=SLABMIN3 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX3 value='0'  class=SLABMAX3 name=SLABMAX3 style='display:none' >  ";
                    tabl = tabl + "<input type=text id=SLABMIN4 value='0'  class=SLABMIN4 name=SLABMIN4 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX4 value='0'  class=SLABMAX4 name=SLABMAX4 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN5 value='0'  class=SLABMIN5 name=SLABMIN5 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX5 value='0'  class=SLABMAX5 name=SLABMAX5 style='display:none' > </td>";
                    tabl = tabl + "<td class=hide> <input type=text id=RCAMT7 value=value=" + Convert.ToDecimal(rslt.RCAMT7) + "  class=RCAMT7 name=RCAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT1 value=value=" + Convert.ToDecimal(rslt.RCAMT1) + "  class=RCAMT1 name=RCAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT2 value=" + Convert.ToDecimal(rslt.RCAMT2) + "  class=RCAMT2 name=RCAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT3 value=" + Convert.ToDecimal(rslt.RCAMT3) + "  class=RCAMT3 name=RCAMT3 style='display:none'>";
                    tabl = tabl + "<input type=text id=RCAMT4 value=" + Convert.ToDecimal(rslt.RCAMT4) + "  class=RCAMT4 name=RCAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT5 value=" + Convert.ToDecimal(rslt.RCAMT5) + "  class=RCAMT5 name=RCAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT6 value=" + Convert.ToDecimal(rslt.RCAMT6) + "  class=RCAMT6 name=RCAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RCOL7 value=" + Convert.ToDecimal(rslt.RCOL7) + "  class=RCOL7 name=RCOL7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL1 value=" + Convert.ToDecimal(rslt.RCOL1) + "  class=RCOL1 name=RCOL1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL2 value=" + Convert.ToDecimal(rslt.RCOL2) + " class=RCOL2 name=RCOL2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL3 value=" + Convert.ToDecimal(rslt.RCOL3) + "  class=RCOL3 name=RCOL3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL4 value=" + Convert.ToDecimal(rslt.RCOL4) + "  class=RCOL4 name=RCOL4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL5 value=" + Convert.ToDecimal(rslt.RCOL5) + " class=RCOL5 name=RCOL5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL6 value=" + Convert.ToDecimal(rslt.RCOL6) + "  class=RCOL6 name=RCOL6 style='display:none' ></td>";
                    tabl = tabl + "<td class='hide'><input type='text' value=" + rslt.GPAAMT + " id='GPAAMT' class='GPAAMT' name='GPAAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPEAMT + " id='GPEAMT' class='GPEAMT' name='GPEAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPETYPE + " id='GPETYPE' class='GPETYPE' name='GPETYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSTYPE + " id='GPCSTYPE' class='GPCSTYPE' name='GPCSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSAMT + " id='GPCSAMT' class='GPCSAMT' name='GPCSAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBTYPE + " id='GPLBTYPE' class='GPLBTYPE' name='GPLBTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBAMT + " id='GPLBAMT' class='GPLBAMT' name='GPLBAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSTYPE + " id='GPSTYPE' class='GPSTYPE' name='GPSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPWTYPE + " id='GPWTYPE' class='GPWTYPE' name='GPWTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSCNTYPE + " id='GPSCNTYPE' class='GPSCNTYPE' name='GPSCNTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.PRDTGID + " id='F_PRDTGID' class='F_PRDTGID' name='F_PRDTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.CUSTGID + " id='F_CUSTGID' class='F_CUSTGID' name='F_CUSTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.STATETYPE + " id='F_STATETYPE' class='F_STATETYPE' name='F_STATETYPE'></td></tr>";
                }
                else
                {
                    var st = ""; var bt = "";

                    if (rslt.TRANDID != 0) { st = "checked"; bt = "true"; }
                    else { bt = "false"; st = ""; }

                    //if (rslt.TRANDID == 0) { st = "checked"; bt = "true"; }
                    //else { bt = "false"; st = ""; }

                    tabl = tabl + "<tr><td class='hidden1'><input type=checkbox id=STFDIDS class=STFDIDS name=STFDIDS value='" + st + "' checked='" + bt + "' onchange=total() style='width:30px'>";
                    tabl = tabl + "<input type=text id=boolSTFDIDS class='hide boolSTFDIDS' name=boolSTFDIDS value='" + bt + "'></td><td>";
                    tabl = tabl + "<input type=text id=TRANDREFNO value=" + rslt.BOENO + "  class='TRANDREFNO' readonly='readonly' name=TRANDREFNO style='width:56px'></td>";
                    tabl = tabl + "<td class='col-lg-0'><input type='text' id='TRANDREFNAME' value='" + rslt.CONTNRNO + "'  class='TRANDREFNAME' readonly='readonly' name='TRANDREFNAME' ></td>";
                    tabl = tabl + "<td><input type=text id=SIZE value='" + rslt.CONTNRSDESC + "' class=SIZE name=SIZE style='width:40px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANIDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANIDATE name=TRANIDATE style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td ><input type=text id=TRANSDATE value='" + rslt.GIDATE.Value.ToString("dd/MM/yyyy") + "' class=TRANSDATE name=TRANSDATE readonly='readonly' style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id='TRANEDATE' value='" + rslt.GODATE.Value.ToString("dd/MM/yyyy") + "' class='TRANEDATE datepicker' readonly='readonly' name='TRANEDATE' style='width:70px' onchange='calculation()'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDWGHT value=" + rslt.WGHT + " class=TRANDWGHT name=TRANDWGHT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDSAMT value='0' class=TRANDSAMT name=TRANDSAMT style='width:70px' readonly='readonly'>";
                    tabl = tabl + "<input type=text id=TRAND_COVID_DISC_AMT value='0' class='TRAND_COVID_DISC_AMT hide' name=TRAND_COVID_DISC_AMT style='width:70px' readonly='readonly'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDHAMT value='0' class=TRANDHAMT name=TRANDHAMT readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td class='hide'><input type=text value='0' id=TRANDEAMT class=TRANDEAMT name=TRANDEAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDFAMT class=TRANDFAMT name=TRANDFAMT  readonly=readonly style='width:70px'></td>";
                    tabl = tabl + "<td><input type=text id=TRANDAAMT value='0' class=TRANDAAMT name=TRANDAAMT   readonly=readonly style='width:70px'></td> ";
                    tabl = tabl + "<td><input type=text id=TRANDADONAMT value='0' class=TRANDADONAMT name=TRANDADONAMT   style='width:70px' onchange='total()'></td>";
                    tabl = tabl + "<td><input type=text value='0' id=TRANDNAMT class=TRANDNAMT name=TRANDNAMT readonly='readonly' style='width:70px'> ";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDVDATE class=TRANDVDATE name=TRANDVDATE value='" + rslt.DODATE.Value.ToString("dd/MM/yyyy") + "'>";
                    tabl = tabl + "<input type=text id=F_DPAIDNO value='" + rslt.DPAIDNO + "'  class='F_DPAIDNO' name=F_DPAIDNO >";
                    tabl = tabl + "<input type=text id=F_DPAIDAMT value=" + rslt.DPAIDAMT + "  class=F_DPAIDAMT name=F_DPAIDAMT ></td>  ";
                    tabl = tabl + "<td class=hide><input type=text id=CONTNRTID value=0  class=CONTNRTID name=CONTNRTID >";
                    tabl = tabl + "<input type=text id=CONTNRSID value=" + rslt.CONTNRSID + "  class=CONTNRSID name=CONTNRSID >";
                    tabl = tabl + "<input type=text id=TRANDREFID value=" + rslt.GIDID + "  class=TRANDREFID name=TRANDREFID >";
                    tabl = tabl + "<input type=text id=BILLEDID value=" + rslt.BILLEDID + "  class=BILLEDID name=BILLEDID >";
                    tabl = tabl + "<input type=text id=BILLEMID value=" + rslt.BILLEMID + "  class=BILLEMID name=BILLEMID >";
                    tabl = tabl + "<input type=text id=F_TARIFFMID value=" + rslt.TARIFFMID + "  class=F_TARIFFMID name=F_TARIFFMID >";
                    tabl = tabl + "<input type=text id=F_TRANDOTYPE value=" + rslt.TRANDOTYPE + "  class=F_TRANDOTYPE name=F_TRANDOTYPE >";
                    tabl = tabl + "<input type=text id=F_TRANBTYPE value='" + rslt.TRANBTYPE + "'  class=F_TRANBTYPE name=F_TRANBTYPE>";
                    tabl = tabl + "<input type=text id=F_CHANAME value='" + rslt.TRANREFNAME + "'  class=F_CHANAME name=F_CHANAME >";
                    tabl = tabl + "<input type=text id=F_STMRNAME value='" + rslt.STMRNAME + "'  class=F_STMRNAME name=F_STMRNAME >";
                    tabl = tabl + "<input type=text id=F_CHAID value=" + rslt.TRANREFID + "  class=F_CHAID name=F_CHAID >";
                    tabl = tabl + "<input type=text id=F_STMRID value=" + rslt.STMRID + "  class=F_STMRID name=F_STMRID ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=TRANDID value=0  class=TRANDID name=TRANDID ></td>";
                    tabl = tabl + "<td class='hide'><input type=text id=days value=0  class=days name=days ><input type=text id=NOD value='" + rslt.NOD + "'  class=NOD name=NOD >";
                    tabl = tabl + "<input type=text id=TRANDRATE value='" + rslt.TRANDRATE + "'  class=TRANDRATE name=TRANDRATE ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RAMT7 value=" + Convert.ToDecimal(rslt.RAMT7) + "  class=RAMT7 name=RAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT1 value=" + Convert.ToDecimal(rslt.RAMT1) + "  class=RAMT1 name=RAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT2 value=" + Convert.ToDecimal(rslt.RAMT2) + "  class=RAMT2 name=RAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT3 value=" + Convert.ToDecimal(rslt.RAMT3) + "  class=RAMT3 name=RAMT3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT4 value=" + Convert.ToDecimal(rslt.RAMT4) + "  class=RAMT4 name=RAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT5 value=" + Convert.ToDecimal(rslt.RAMT5) + "  class=RAMT5 name=RAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RAMT6 value=" + Convert.ToDecimal(rslt.RAMT6) + " class=RAMT6 name=RAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=SLABMIN6 value='0'  class=SLABMIN6 name=SLABMIN6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX6 value='0'  class=SLABMAX6 name=SLABMAX6 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN value='0'  class=SLABMIN name=SLABMIN style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX value='0'  class=SLABMAX name=SLABMAX style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN1 value='0'  class=SLABMIN1 name=SLABMIN1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX1 value='0'  class=SLABMAX1 name=SLABMAX1 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMIN2 value='0'  class=SLABMIN2 name=SLABMIN2 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX2 value='0'  class=SLABMAX2 name=SLABMAX2 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN3 value='0'  class=SLABMIN3 name=SLABMIN3 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX3 value='0'  class=SLABMAX3 name=SLABMAX3 style='display:none' >  ";
                    tabl = tabl + "<input type=text id=SLABMIN4 value='0'  class=SLABMIN4 name=SLABMIN4 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX4 value='0'  class=SLABMAX4 name=SLABMAX4 style='display:none' > ";
                    tabl = tabl + "<input type=text id=SLABMIN5 value='0'  class=SLABMIN5 name=SLABMIN5 style='display:none' >";
                    tabl = tabl + "<input type=text id=SLABMAX5 value='0'  class=SLABMAX5 name=SLABMAX5 style='display:none' > </td>";
                    tabl = tabl + "<td class=hide> <input type=text id=RCAMT7 value=value=" + Convert.ToDecimal(rslt.RCAMT7) + "  class=RCAMT7 name=RCAMT7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT1 value=value=" + Convert.ToDecimal(rslt.RCAMT1) + "  class=RCAMT1 name=RCAMT1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT2 value=" + Convert.ToDecimal(rslt.RCAMT2) + "  class=RCAMT2 name=RCAMT2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT3 value=" + Convert.ToDecimal(rslt.RCAMT3) + "  class=RCAMT3 name=RCAMT3 style='display:none'>";
                    tabl = tabl + "<input type=text id=RCAMT4 value=" + Convert.ToDecimal(rslt.RCAMT4) + "  class=RCAMT4 name=RCAMT4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT5 value=" + Convert.ToDecimal(rslt.RCAMT5) + "  class=RCAMT5 name=RCAMT5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCAMT6 value=" + Convert.ToDecimal(rslt.RCAMT6) + "  class=RCAMT6 name=RCAMT6 style='display:none' ></td>";
                    tabl = tabl + "<td class=hide><input type=text id=RCOL7 value=" + Convert.ToDecimal(rslt.RCOL7) + "  class=RCOL7 name=RCOL7 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL1 value=" + Convert.ToDecimal(rslt.RCOL1) + "  class=RCOL1 name=RCOL1 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL2 value=" + Convert.ToDecimal(rslt.RCOL2) + " class=RCOL2 name=RCOL2 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL3 value=" + Convert.ToDecimal(rslt.RCOL3) + "  class=RCOL3 name=RCOL3 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL4 value=" + Convert.ToDecimal(rslt.RCOL4) + "  class=RCOL4 name=RCOL4 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL5 value=" + Convert.ToDecimal(rslt.RCOL5) + " class=RCOL5 name=RCOL5 style='display:none' >";
                    tabl = tabl + "<input type=text id=RCOL6 value=" + Convert.ToDecimal(rslt.RCOL6) + "  class=RCOL6 name=RCOL6 style='display:none' ></td>";
                    tabl = tabl + "<td class='hide'><input type='text' value=" + rslt.GPAAMT + " id='GPAAMT' class='GPAAMT' name='GPAAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPEAMT + " id='GPEAMT' class='GPEAMT' name='GPEAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPETYPE + " id='GPETYPE' class='GPETYPE' name='GPETYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSTYPE + " id='GPCSTYPE' class='GPCSTYPE' name='GPCSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPCSAMT + " id='GPCSAMT' class='GPCSAMT' name='GPCSAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBTYPE + " id='GPLBTYPE' class='GPLBTYPE' name='GPLBTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPLBAMT + " id='GPLBAMT' class='GPLBAMT' name='GPLBAMT'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSTYPE + " id='GPSTYPE' class='GPSTYPE' name='GPSTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPWTYPE + " id='GPWTYPE' class='GPWTYPE' name='GPWTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.GPSCNTYPE + " id='GPSCNTYPE' class='GPSCNTYPE' name='GPSCNTYPE'>";
                    tabl = tabl + "<input type='text' value=" + rslt.PRDTGID + " id='F_PRDTGID' class='F_PRDTGID' name='F_PRDTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.CUSTGID + " id='F_CUSTGID' class='F_CUSTGID' name='F_CUSTGID'>";
                    tabl = tabl + "<input type='text' value=" + rslt.STATETYPE + " id='F_STATETYPE' class='F_STATETYPE' name='F_STATETYPE'></td></tr>";
                }

                count++;
            }
            tabl = tabl + "</Table>";

            return tabl;

        }

        //.............................storage ,handling,energy,fuel, and PTI amount...........//
        public JsonResult Bill_Detail(string id)
        {
            
            var param = id.Split('-');

            var TARIFFMID = 0;
            var CONTNRSID = 0;
            var STMRID = 0;
            string WGHT = "0";
            var CHRGETYPE = 0;
            if (!id.Contains("undefined") && !id.Contains("null"))
            {
                if (param[0] != "" && param[0] != "Please select") { TARIFFMID = Convert.ToInt32(param[0]); } else { TARIFFMID = 0; }
                if (param[1] != "" || param[1] != "undefined")
                    CHRGETYPE = Convert.ToInt32(param[1]);
                if (param[2] != "") { CONTNRSID = Convert.ToInt32(param[2]); } else { CONTNRSID = 0; }  //var CONTNRSID = Convert.ToInt32(param[2]);
                if (param[3] != "") { STMRID = Convert.ToInt32(param[3]); } else { STMRID = 0; }  //var STMRID = Convert.ToInt32(param[3]);/* INSTEAD OF SLABTID=5 ,,PARAM[5]*/
                                                                                                   //if (param[5] != "") { WGHT = Convert.ToInt32(param[5]); } else { WGHT = 0; };
                if (param[5] != "") { WGHT = param[5]; } else { WGHT = "0"; };

            }

            WGHT = WGHT.Replace(',', '.');
            //Response.Write(strqty);
            decimal aqty = Convert.ToDecimal(WGHT);

            var handlng = 0;
            var htype = 0;
            ////if (param[1] == "1") { handlng = 3; }
            ////if (param[1] == "2") { handlng = 4; }
            if (param[1] != "") { handlng = Convert.ToInt32(param[1]); }
            //if (CHRGETYPE == 2)
            //{
            //    if (param[4] == "3" || param[4] == "4" || param[4] == "5") { htype = 2; } else { htype = Convert.ToInt32(param[4]); }
            //}
            //else
            //{
            //    if (param[4] != "") { htype = Convert.ToInt32(param[4]); } else { htype = 1; }
            //}

            if (param[4] != "") { htype = Convert.ToInt32(param[4]); } else { htype = 1; }

            if (TARIFFMID == 4)
            {
                //var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + handlng + ",14,15,16)and HTYPE=" + Convert.ToInt32(param[4]) + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + "and STMRID=" + STMRID).ToList();
                var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + handlng + ",14,15,16)and HTYPE=" + Convert.ToInt32(htype) + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + "and STMRID=" + STMRID).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + handlng + ",14,15,16)and HTYPE=" + Convert.ToInt32(param[4]) + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " and (SLABMIN <= " + WGHT + " or SLABMIN >= " + WGHT + ")").ToList();
                //var query = context.Database.SqlQuery<PR_NEW_IMPORT_RATECARDMASTER_FLX_ASSGN_Result>("EXEC PR_NEW_IMPORT_RATECARDMASTER_FLX_ASSGN @PTARIFFMID=" + TARIFFMID + ", @PSLABTID=" + handlng + ", @PHTYPE=" + Convert.ToInt32(param[4]) + ", @PCHRGETYPE = " + CHRGETYPE + ", @PCONTNRSID = " + CONTNRSID + ", @PSLABMIN = " + aqty).ToList();
                var query = context.Database.SqlQuery<PR_NEW_IMPORT_RATECARDMASTER_FLX_ASSGN_Result>("EXEC PR_NEW_IMPORT_RATECARDMASTER_FLX_ASSGN @PTARIFFMID=" + TARIFFMID + ", @PSLABTID=" + handlng + ", @PHTYPE=" + Convert.ToInt32(htype) + ", @PCHRGETYPE = " + CHRGETYPE + ", @PCONTNRSID = " + CONTNRSID + ", @PSLABMIN = " + aqty).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }


        }//.....end

        //

        public JsonResult Calc_Bill_Detail(string id)
        {
            var param = id.Split('-');

            var TARIFFMID = 0;
            var CONTNRSID = 0;
            var STMRID = 0;
            var WGHT = 0;
            var CHRGETYPE = 0; var handlng = 0;
            if (!id.Contains("undefined") && !id.Contains("null"))
            {
                if (param[0] != "") { TARIFFMID = Convert.ToInt32(param[0]); } else { TARIFFMID = 0; }

                CHRGETYPE = Convert.ToInt32(param[1]);
                if (param[2] != "") { CONTNRSID = Convert.ToInt32(param[2]); } else { CONTNRSID = 0; }  //var CONTNRSID = Convert.ToInt32(param[2]);
                if (param[3] != "") { STMRID = Convert.ToInt32(param[3]); } else { STMRID = 0; }  //var STMRID = Convert.ToInt32(param[3]);/* INSTEAD OF SLABTID=5 ,,PARAM[5]*/
                if (param[5] != "") { WGHT = Convert.ToInt32(param[5]); } else { WGHT = 0; }

                //if (WGHT > 0)
                //{ WGHT = WGHT / 1000; }

                handlng = 0;
                if (param[1] == "1") { handlng = 3; }
                if (param[1] == "2") { handlng = 4; }
            }

            if (TARIFFMID == 4)
            {
                var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + handlng + ",14,15,16)and HTYPE=" + Convert.ToInt32(param[4]) + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + "and STMRID=" + STMRID).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + handlng + ",14,15,16)and HTYPE=" + Convert.ToInt32(param[4]) + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " and (SLABMIN <= " + WGHT + " or SLABMIN >= " + WGHT + ")").ToList();
                var query = context.Database.SqlQuery<PR_NEW_IMPORT_RATECARDMASTER_FLX_ASSGN_Result>("EXEC PR_NEW_IMPORT_RATECARDMASTER_FLX_ASSGN @PTARIFFMID=" + TARIFFMID + ", @PSLABTID=" + handlng + ", @PHTYPE=" + Convert.ToInt32(param[4]) + ", @PCHRGETYPE = " + CHRGETYPE + ", @PCONTNRSID = " + CONTNRSID + ", @PSLABMIN = " + WGHT).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }


        }//.....end
        //

        //............ratecardmaster.....................
        public JsonResult RATECARD(string id)
        {
            var param = id.Split('-');
            //var TARIFFMID = Convert.ToInt32(param[0]);
            var SLABMIN = 0;
            var TARIFFMID = 0;
            var CONTNRSID = 0;
            var STMRID = 0; 
            var CHRGETYPE = 0;
           
            //var WGHT = 0;
            if (!id.Contains("undefined") && !id.Contains("null"))
            {
                if (param[0] != "") { TARIFFMID = Convert.ToInt32(param[0]); } else { TARIFFMID = 0; }
                CHRGETYPE = Convert.ToInt32(param[1]);
                if (param[2] != "")
                { CONTNRSID = Convert.ToInt32(param[2]); }
                else
                { CONTNRSID = 0; }

                if (param[3] != "")
                { STMRID = Convert.ToInt32(param[3]); }
                else
                { STMRID = 0; }

                //if (param[6] != "")
                //{ WGHT = Convert.ToInt32(param[6]); }
                //else
                //{ WGHT = 0; }


                 SLABMIN = Convert.ToInt32(param[4]);
            }



            if (TARIFFMID == 4)
            {
                var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABAMT,SLABMIN,SLABMAX from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + "and SLABTID=2 and HTYPE=" + Convert.ToInt32(param[5]) + " and SDTYPE=1 and SLABMIN <= " + SLABMIN + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " and STMRID=" + STMRID + " order by SLABMIN").ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {

                var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABAMT,SLABMIN,SLABMAX from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + "and SLABTID=2 and HTYPE=" + Convert.ToInt32(param[5]) + " and SDTYPE=1 and SLABMIN <= " + SLABMIN + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " order by SLABMIN").ToList();
                return Json(query, JsonRequestBehavior.AllowGet);

            }

        } //...end

        public JsonResult COVIDRATECARD(string id)
        {
            var param = id.Split('~');
            //
            var zsdate = ""; 
            var zedate = "";
            var ztariffmid = 0; var zstmrmid = 0; var zchrgtype = 0; var zcontnrsid = 0;var zotype = 0;var zchrgdate = "";
            if (!id.Contains("undefined") && !id.Contains("null"))
            {
                zsdate = param[0];
                zedate = param[1];

                if ((param[2]) != "") { ztariffmid = Convert.ToInt32(param[2]); }
                zstmrmid = Convert.ToInt32(param[3]); zchrgtype = Convert.ToInt32(param[4]);
                zcontnrsid = Convert.ToInt32(param[5]); zotype = Convert.ToInt32(param[7]);
                zchrgdate = param[6];
            }

            
            
             
            
            
            var xcovidsdate = Convert.ToDateTime(Session["COVIDSDATE"]).ToString("dd/MM/yyyy").Split('-');
            if (xcovidsdate.Length == 1)
                xcovidsdate = Convert.ToDateTime(Session["COVIDSDATE"]).ToString("dd/MM/yyyy").Split('/');
            var zcovidsdate = xcovidsdate[1] + '-' + xcovidsdate[0] + '-' + xcovidsdate[2];

            var xcovidedate = Convert.ToDateTime(Session["COVIDEDATE"]).ToString("dd/MM/yyyy").Split('-');
            if (xcovidedate.Length == 1)
                xcovidedate = Convert.ToDateTime(Session["COVIDEDATE"]).ToString("dd/MM/yyyy").Split('/');
            var zcovidedate = xcovidedate[1] + '-' + xcovidedate[0] + '-' + xcovidedate[2];

            using (var e = new CFSImportEntities())
            {
                //var query = context.Database.SqlQuery<z_pr_New_Import_Covid_Slab_Assgn_Result>("z_pr_New_Import_Covid_Slab_Assgn @PKUSRID = '" + Session["CUSRID"] + "', @PSDATE = '" + zsdate + "', @PEDATE = '" + zedate + "', @PTARIFFMID = " + ztariffmid + ", @PSTMRID = " + zstmrmid + ", @PCHRGETYPE = " + zchrgtype + ", @PSLABTID = 2, @PSLABMIN = 0, @PCONTNRSID = " + zcontnrsid + ", @PSLABHTYPE = 0, @PCHRGDATE = '" + zchrgdate + "' @PCDate1 = '" + zcovidsdate + "', @PCDate2 = '" + zcovidedate + "'").ToList();
                var query = context.Database.SqlQuery<z_pr_New_Import_Covid_Slab_Assgn_Result>("z_pr_New_Import_Covid_Slab_Assgn @PKUSRID = '" + Session["CUSRID"].ToString() + "', @PSDATE = '" + zsdate + "', @PEDATE = '" + zedate + "', @PTARIFFMID = " + ztariffmid + ", @PSTMRID = " + zstmrmid + ", @PCHRGETYPE = " + zchrgtype + ", @PSLABTID = 2, @PSLABMIN = 0, @PCONTNRSID = " + zcontnrsid + ", @PSLABHTYPE = " + zotype + ", @PCHRGDATE = '" + zchrgdate + "', @PCDate1 = '" + zcovidsdate + "', @PCDate2 = '" + zcovidedate + "'").ToList();
                //var a = 1;
                return Json(query, JsonRequestBehavior.AllowGet);
            }
        } //...end

        public JsonResult GetHandlingAmt(string id)
        {
            var param = id.Split('-');
            // var TARIFFMID = Convert.ToInt32(param[0]);
            var TARIFFMID = 0;
            var CHRGETYPE = 0; var CONTNRSID =0; var STMRID = 0; var handlng = 0; var WGHT = "0";
            if (!id.Contains("undefined") && !id.Contains("null"))
            {
                if (param[0] != "") { TARIFFMID = Convert.ToInt32(param[0]); } else { TARIFFMID = 0; }
                CHRGETYPE = Convert.ToInt32(param[1]);
                CONTNRSID = Convert.ToInt32(param[2]);
                STMRID = Convert.ToInt32(param[3]);
                if (param[6] != "") { WGHT = param[6]; } else { WGHT = "0"; }
                handlng = 0;
                if (param[1] == "1") { handlng = 3; }
                if (param[1] == "2") { handlng = 4; }
            }
                

            WGHT = WGHT.Replace(',', '.');
            //Response.Write(strqty);
            decimal aqty = Convert.ToDecimal(WGHT);

            if (TARIFFMID == 4)
            {
                var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>("select SLABAMT,SLABMIN,SLABMAX from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + "and SLABTID in (" + handlng + ") and HTYPE=" + Convert.ToInt32(param[4]) + " and SDTYPE=" + Convert.ToInt32(param[5]) + " and  CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " and STMRID=" + STMRID  + " order by SLABMIN").ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var tqry = "select SLABAMT,SLABMIN,SLABMAX from VW_IMPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID = " + TARIFFMID + "and SLABTID in (" + handlng + ") and HTYPE = " + Convert.ToInt32(param[4]) + " and SDTYPE = " + Convert.ToInt32(param[5]) + "  and CHRGETYPE = " + CHRGETYPE + " and CONTNRSID = " + CONTNRSID + " and ((" + aqty + " >= slabmin and " + aqty + " <= slabmax and SLABMAX <> 0) or (" + aqty + " >= slabmin and SLABMAX = 0))   order by SLABMIN";

                var query = context.Database.SqlQuery<VW_IMPORT_RATECARDMASTER_FLX_ASSGN>(tqry).ToList();

                return Json(query, JsonRequestBehavior.AllowGet);


            }

        } //...end


        //............ratecardmaster.....................
        public JsonResult GetImportGSTRATE(string id)
        {
            var param = id.Split('~');
            var StateType = 0;
            if (param[0] != "")
            { StateType = Convert.ToInt32(param[0]); }
            else
            { StateType = 0; }

            var SlabTId = Convert.ToInt32(param[1]);
            if (StateType == 0)
            {
                var query = context.Database.SqlQuery<VW_IMPORT_SLABTYPE_HSN_DETAIL_ASSGN>("select HSNCODE,CGSTEXPRN,SGSTEXPRN,IGSTEXPRN from VW_IMPORT_SLABTYPE_HSN_DETAIL_ASSGN where SLABTID=" + SlabTId + " order by HSNCODE").ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {

                var query = context.Database.SqlQuery<VW_IMPORT_SLABTYPE_HSN_DETAIL_ASSGN>("select HSNCODE,ACGSTEXPRN as CGSTEXPRN,ASGSTEXPRN as SGSTEXPRN,AIGSTEXPRN as IGSTEXPRN from VW_IMPORT_SLABTYPE_HSN_DETAIL_ASSGN where SLABTID=" + SlabTId + " order by HSNCODE").ToList();

                return Json(query, JsonRequestBehavior.AllowGet);


            }

        } //...end

        //..........TARIFFTMID get function....................
        public JsonResult TARIFFTMID(int id)
        {
            var query = context.Database.SqlQuery<int>("select TARIFFTMID from exporttariffmaster where TARIFFMID=" + id).ToList();
            return Json(query, JsonRequestBehavior.AllowGet);
        } //........end
        public JsonResult GetCount(int id)/*OTL charges DEDNOS count*/
        {

            int BILLEMID = 0;
            if (id == 0 )
            {
                BILLEMID = 0;
            }
            else { BILLEMID = Convert.ToInt32(id); }
            var query = context.Database.SqlQuery<int>("select NOC from VW_IMPORT_BILL_OPEN_SHEET_SEAL_COUNT_ASSGN where BILLEMID=" + BILLEMID).ToList();
            return Json(query, JsonRequestBehavior.AllowGet);
        } //........end


        public JsonResult GetTransportCharge(int id)/*handling amt in category master*/
        {
            var query = context.Database.SqlQuery<CategoryMaster>("select * from CategoryMaster where CATEID=" + id).ToList();
            return Json(query, JsonRequestBehavior.AllowGet);
        } //........end

        //..........................Printview...
        [Authorize(Roles = "ImportInvoicePrint")]
        public void PrintView(string id)
        {

            var param = id.Split(';');
            // Response.Write(@"10.10.5.5"); Response.End();
            //  ........delete TMPRPT...//

            var ids = Convert.ToInt32(param[0]);
            var rpttype = Convert.ToInt32(param[1]);
            var gsttype = 0;// Convert.ToInt32(param[2]);
            var billedto =  Convert.ToInt32(param[3]);
            var strHead = param[4].ToString();

            var Querytr = context.Database.SqlQuery<TransactionMaster>("select * from transactionmaster where TRANMID=" + ids).ToList();
            var PCNT = 0;
            var slbnrnsts = 0;

            if (Querytr.Count() != 0)
            {
                PCNT = Convert.ToInt32(Querytr[0].TRANPCOUNT);
                slbnrnsts = Querytr[0].SLABNARN_STS;
            }

            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "IMPORTINVOICE", Convert.ToInt32(ids), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;



                // cryRpt.Load(Server.MapPath("~/") + "//Reports//RPT_0302.rpt");


                //........Get TRANPCOUNT...//
                //var Query = context.Database.SqlQuery<int>("select TRANPCOUNT from transactionmaster where TRANMID=" + ids).ToList();
                //var PCNT = 0;

                //if (Query.Count() != 0) { PCNT = Query[0]; }
                var TRANPCOUNT = ++PCNT;
                // Response.Write(++PCNT);
                // Response.End();

                context.Database.ExecuteSqlCommand("UPDATE transactionmaster SET TRANPCOUNT=" + TRANPCOUNT + " WHERE TRANMID=" + ids);

                string RptNamePath = "";
                gsttype = 1;
                //switch (billedto)
                //{
                //    case 1:
                //        if (rpttype == 0)
                //            if (gsttype == 0) { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_rpt_IMP.RPT"; } else { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "GST_import_Invoice_rpt_imp.RPT"; }
                //        else if (rpttype == 1)
                //            if (gsttype == 0) { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_Group_rpt_IMP.RPT"; } else { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "GST_import_Invoice_Group_rpt_IMP.RPT"; }

                //        break;

                //    default:

                //        if (rpttype == 0)
                //            if (gsttype == 0) { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_rpt.RPT"; } else { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "GST_import_Invoice_rpt.RPT"; }
                //        else if (rpttype == 1)
                //            if (gsttype == 0) { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_Group_rpt.RPT"; } else { RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "GST_import_Invoice_Group_rpt.RPT"; }

                //        break;
                //}
                if (slbnrnsts == 0)
                    RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "GST_import_Invoice_rpt.RPT"; 
                else
                    RptNamePath = ConfigurationManager.AppSettings["Reporturl"] + "GST_import_Invoice_rpt_1003.RPT";


                //cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_Group_rpt.RPT");

                cryRpt.Load(RptNamePath);
                cryRpt.RecordSelectionFormula = "{VW_IMPORT_TRANSACTION_GST_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_IMPORT_TRANSACTION_GST_PRINT_ASSGN.TRANMID} = " + ids;



                string paramName = "@FTHANDLING";

                for (int i = 0; i < cryRpt.DataDefinition.FormulaFields.Count; i++)
                    if (cryRpt.DataDefinition.FormulaFields[i].FormulaName == "{" + paramName + "}")
                        cryRpt.DataDefinition.FormulaFields[i].Text = "'" + strHead + "'";


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
        //public void PrintView(string id)
        //{

        //    var param = id.Split(';');
        //    // Response.Write(@"10.10.5.5"); Response.End();
        //    //  ........delete TMPRPT...//

        //    var ids = Convert.ToInt32(param[0]);
        //    var rpttype = Convert.ToInt32(param[1]); 
        //    context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
        //    var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "IMPORTINVOICE", Convert.ToInt32(ids), Session["CUSRID"].ToString());
        //    if (TMPRPT_IDS == "Successfully Added")
        //    {
        //        ReportDocument cryRpt = new ReportDocument();
        //        TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
        //        TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
        //        ConnectionInfo crConnectionInfo = new ConnectionInfo();
        //        Tables CrTables;



        //        // cryRpt.Load(Server.MapPath("~/") + "//Reports//RPT_0302.rpt");


        //        //........Get TRANPCOUNT...//
        //        var Query = context.Database.SqlQuery<int>("select TRANPCOUNT from transactionmaster where TRANMID=" + ids).ToList();
        //        var PCNT = 0;

        //        if (Query.Count() != 0) { PCNT = Query[0]; }
        //        var TRANPCOUNT = ++PCNT;
        //        // Response.Write(++PCNT);
        //        // Response.End();

        //        context.Database.ExecuteSqlCommand("UPDATE transactionmaster SET TRANPCOUNT=" + TRANPCOUNT + " WHERE TRANMID=" + ids);

        //        if (rpttype==0)
        //        cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_rpt.RPT");
        //        else if (rpttype == 1)
        //            cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_Group_rpt.RPT");

        //        cryRpt.RecordSelectionFormula = "{VW_IMPORT_TRANSACTION_GST_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_IMPORT_TRANSACTION_GST_PRINT_ASSGN.TRANMID} = " + ids;



        //        crConnectionInfo.ServerName = stringbuilder.DataSource;
        //        crConnectionInfo.DatabaseName = stringbuilder.InitialCatalog;
        //        crConnectionInfo.UserID = stringbuilder.UserID;
        //        crConnectionInfo.Password = stringbuilder.Password;

        //        CrTables = cryRpt.Database.Tables;
        //        foreach (CrystalDecisions.CrystalReports.Engine.Table CrTable in CrTables)
        //        {
        //            crtableLogoninfo = CrTable.LogOnInfo;
        //            crtableLogoninfo.ConnectionInfo = crConnectionInfo;
        //            CrTable.ApplyLogOnInfo(crtableLogoninfo);
        //        }


        //        cryRpt.ExportToHttpResponse(ExportFormatType.PortableDocFormat, System.Web.HttpContext.Current.Response, false, "");
        //        cryRpt.Dispose();
        //        cryRpt.Close();
        //    }

        //}
        //end

        //bform start

        //[Authorize(Roles = "ImportInvoiceNameUpdate")]
        public ActionResult BForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }



            var ids = Convert.ToInt32(id);
            //var gstamt = Convert.ToInt32(param[1]);

            //BILLED TO
            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst1 = new List<SelectListItem>();
            SelectListItem selectedItemtax1 = new SelectListItem { Text = "CHA", Value = "0", Selected = true };
            selectedtaxlst1.Add(selectedItemtax1);
            ViewBag.BILLEDTO = selectedtaxlst1;


            ViewBag.TRANMID = ids;
            ViewBag.FGSTAMT = 0;//gstamt;

            ViewBag.OSMID = 0;
            ViewBag.OSBILLREFNAME = "";
            ViewBag.OSBILLREFID = 0;
            ViewBag.OSBBCHACATEAGSTNO = "";
            ViewBag.OSBBCHASTATEID = 0;
            ViewBag.OSBBCHAADDR1 = "";
            ViewBag.OSBBCHAADDR2 = "";
            ViewBag.OSBBCHAADDR3 = "";
            ViewBag.OSBBCHAADDR4 = "";
            ViewBag.OSBBCHA_CATEAID = 0;

            var squer = context.Database.SqlQuery<TransactionMaster>("select *from TransactionMaster  where TRANMID=" + ids).ToList();

            if (squer.Count > 0)
            {

                ViewBag.OSMID = 0;
                ViewBag.OSBILLREFNAME = squer[0].TRANREFNAME;
                ViewBag.OSBILLREFID = squer[0].TRANREFID;

                int chaid = Convert.ToInt32(squer[0].TRANREFID);
                int chaaid = Convert.ToInt32(squer[0].CATEAID);

                if (chaaid > 0)
                {
                    var adds = context.Database.SqlQuery<Category_Address_Details>("Select *From CATEGORY_ADDRESS_DETAIL Where CATEAID  = " + chaaid + " ORDER By  CATEAID DESC").ToList();
                    ViewBag.OSBBCHACATEAGSTNO = adds[0].CATEAGSTNO;
                    ViewBag.OSBBCHASTATEID = adds[0].STATEID;
                    ViewBag.OSBBCHAADDR1 = adds[0].CATEAADDR1;
                    ViewBag.OSBBCHAADDR2 = adds[0].CATEAADDR2;
                    ViewBag.OSBBCHAADDR3 = adds[0].CATEAADDR3;
                    ViewBag.OSBBCHAADDR4 = adds[0].CATEAADDR4;
                    ViewBag.OSBBCHA_CATEAID = adds[0].CATEAID;

                    var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + adds[0].STATEID).ToList();
                    if (starqy.Count > 0)
                    {
                        ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
                        ViewBag.STATETYPE = starqy[0].STATETYPE;
                    }


                    ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", adds[0].CATEAID).ToList();
                }
                else
                {
                    var adds = context.Database.SqlQuery<Category_Address_Details>("Select Top 1 *From CATEGORY_ADDRESS_DETAIL Where CATEID  = " + chaid + " ORDER By  CATEAID DESC").ToList();
                    ViewBag.OSBBCHACATEAGSTNO = adds[0].CATEAGSTNO;
                    ViewBag.OSBBCHASTATEID = adds[0].STATEID;
                    ViewBag.OSBBCHAADDR1 = adds[0].CATEAADDR1;
                    ViewBag.OSBBCHAADDR2 = adds[0].CATEAADDR2;
                    ViewBag.OSBBCHAADDR3 = adds[0].CATEAADDR3;
                    ViewBag.OSBBCHAADDR4 = adds[0].CATEAADDR4;
                    ViewBag.OSBBCHA_CATEAID = adds[0].CATEAID;

                    var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + adds[0].STATEID).ToList();
                    if (starqy.Count > 0)
                    {
                        ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
                        ViewBag.STATETYPE = starqy[0].STATETYPE;
                    }


                    ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC").ToList();
                }

            }


            //var query = context.Database.SqlQuery<TransactionDetail>("select top 1 * from TransactionDetail where TRANMID=" + ids).ToList();
            //if (query[0].TRANDREFID > 0)
            //{
            //    var query1 = context.Database.SqlQuery<OpenSheetDetail>("select  top 1  * from OpenSheetDetail where GIDID =" + query[0].TRANDREFID).ToList();

            //    if (query1[0].OSMID > 0)
            //    {
            //        var query2 = context.Database.SqlQuery<OpenSheetMaster>("select  top 1  * from OpenSheetMaster where OSMID =" + query1[0].OSMID).ToList();

            //        if (query2.Count > 0)
            //        {
            //            ViewBag.OSMID = query1[0].OSMID;
            //            ViewBag.OSBILLREFNAME = query2[0].OSBILLREFNAME;
            //            ViewBag.OSBILLREFID = query2[0].OSBILLREFID;

            //            int chaid = Convert.ToInt32(query2[0].OSBILLREFID);
            //            int chaaid = Convert.ToInt32(query2[0].OSBBCHACATEAID);


            //            if (chaaid == 0)
            //            {
            //                var adds = context.Database.SqlQuery<Category_Address_Details>("Select Top 1  *From CATEGORY_ADDRESS_DETAIL Where CATEID  = " + chaid + " ORDER By  CATEAID DESC").ToList();

            //                if (adds.Count > 0)
            //                {
            //                    ViewBag.OSBBCHACATEAGSTNO = adds[0].CATEAGSTNO;
            //                    ViewBag.OSBBCHASTATEID = adds[0].STATEID;
            //                    ViewBag.OSBBCHAADDR1 = adds[0].CATEAADDR1;
            //                    ViewBag.OSBBCHAADDR2 = adds[0].CATEAADDR1;
            //                    ViewBag.OSBBCHAADDR3 = adds[0].CATEAADDR3;
            //                    ViewBag.OSBBCHAADDR4 = adds[0].CATEAADDR4;
            //                    ViewBag.OSBBCHA_CATEAID = adds[0].CATEAID;

            //                    var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + adds[0].STATEID).ToList();
            //                    if (starqy.Count > 0)
            //                    {
            //                        ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
            //                        ViewBag.STATETYPE = starqy[0].STATETYPE;
            //                    }


            //                    ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", adds[0].CATEAID).ToList();
            //                }
            //            }
            //            else
            //            {
            //                var adds = context.Database.SqlQuery<Category_Address_Details>("Select  *From CATEGORY_ADDRESS_DETAIL Where CATEAID  = " + chaaid).ToList();

            //                if (adds.Count > 0)
            //                {
            //                    ViewBag.OSBBCHACATEAGSTNO = adds[0].CATEAGSTNO;
            //                    ViewBag.OSBBCHASTATEID = adds[0].STATEID;
            //                    ViewBag.OSBBCHAADDR1 = adds[0].CATEAADDR1;
            //                    ViewBag.OSBBCHAADDR2 = adds[0].CATEAADDR1;
            //                    ViewBag.OSBBCHAADDR3 = adds[0].CATEAADDR3;
            //                    ViewBag.OSBBCHAADDR4 = adds[0].CATEAADDR4;
            //                    ViewBag.OSBBCHA_CATEAID = adds[0].CATEAID;

            //                    var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + query2[0].OSBBCHASTATEID).ToList();
            //                    if (starqy.Count > 0)
            //                    {
            //                        ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
            //                        ViewBag.STATETYPE = starqy[0].STATETYPE;
            //                    }


            //                    ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC", chaaid).ToList();
            //                }
            //            }

            //            //ViewBag.OSBBCHACATEAID = new SelectList(context.categoryaddressdetails.Where(m => m.CATEID == chaid).OrderBy(x => x.CATEATYPEDESC), "CATEAID", "CATEATYPEDESC").ToList();
            //        }
            //    }


            //}

            return View();
        }

        #region GetStateType
        public JsonResult GetStateType(string id)
        {
            if (id != "0")
            {
                var result = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER WHERE DISPSTATUS = 0 and STATEID=" + Convert.ToInt32(id)).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var result = "";
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion
        //bform end

        //public ActionResult AForm(string id)
        //{
        //    if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

        //    var param = id.Split('~');

        //    var ids = Convert.ToInt32(param[0]);
        //    var gstamt = Convert.ToInt32(param[1]);

        //    ViewBag.id = ids;
        //    ViewBag.FGSTAMT = gstamt;
        //    //var query = context.Database.SqlQuery<TransactionMaster>("select * from TransactionMaster where TRANMID=" + ids).ToList();
        //    //if (query[0].TRANCSNAME != null)
        //    //{
        //    //    ViewBag.TRANCSNAME = query[0].TRANCSNAME;
        //    //    ViewBag.TRANIMPADDR1 = query[0].TRANIMPADDR1;
        //    //    ViewBag.TRANIMPADDR2 = query[0].TRANIMPADDR2;
        //    //    ViewBag.TRANIMPADDR3 = query[0].TRANIMPADDR3;
        //    //    ViewBag.TRANIMPADDR4 = query[0].TRANIMPADDR4;
        //    //}
        //    //else
        //    //{
        //    //    var chaid = Convert.ToInt32(query[0].TRANREFID);
        //    //    var sql = context.Database.SqlQuery<CategoryMaster>("select * from CategoryMaster where CATEID=" + chaid).ToList();
        //    //    ViewBag.TRANCSNAME = sql[0].CATENAME;
        //    //    ViewBag.TRANIMPADDR1 = sql[0].CATEADDR1;
        //    //    ViewBag.TRANIMPADDR2 = sql[0].CATEADDR2;
        //    //    ViewBag.TRANIMPADDR3 = sql[0].CATEADDR3;
        //    //    ViewBag.TRANIMPADDR4 = sql[0].CATEADDR4;
        //    //}

        //    //var query = ""; // rajesh to check context.Database.SqlQuery<pr_Import_Invoice_Print_Customer_Detail_Assgn_Result>("EXEC pr_Import_Invoice_Print_Customer_Detail_Assgn @PTranMID = " + ids).ToList();
        //    //if (query[0].OSBILLREFNAME != null)
        //    //{
        //    //    ViewBag.TRANCSNAME = query[0].OSBILLREFNAME;
        //    //    ViewBag.TRANIMPADDR1 = query[0].CATEADDR1;
        //    //    ViewBag.TRANIMPADDR2 = query[0].CATEADDR2;
        //    //    ViewBag.TRANIMPADDR3 = query[0].CATEADDR3;
        //    //    ViewBag.TRANIMPADDR4 = query[0].CATEADDR4;
        //    //    ViewBag.GSTNO = query[0].CATEGSTNO;
        //    //}
        //    return View();
        //}

        public JsonResult AFormAddr(string id)
        {

            var param = id.Split('~');

            var ids = Convert.ToInt32(param[0]);
            var billedto = Convert.ToInt32(param[1]);

            if (billedto == 0)
            {
                var query = context.Database.SqlQuery<VW_IMPORT_TRANSACTION_ADDRESS_DETAIL_ASSGN>("select TRANCSNAME,TRANIMPADDR1,TRANIMPADDR2,TRANIMPADDR3,TRANIMPADDR4,CHACATEGSTNO from VW_IMPORT_TRANSACTION_ADDRESS_DETAIL_ASSGN where TRANMID=" + ids).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var query = context.Database.SqlQuery<VW_IMPORT_TRANSACTION_ADDRESS_DETAIL_ASSGN>("select IMPRTNAME,IMPCATEADDR1,IMPCATEADDR2,IMPCATEADDR3,IMPCATEADDR4,IMPCATEGSTNO from VW_IMPORT_TRANSACTION_ADDRESS_DETAIL_ASSGN where TRANMID=" + ids).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);

            }

        }


        public JsonResult AFormCategoryAddr(string id)
        {

            var param = id.Split('~');

            var ids = Convert.ToInt32(param[0]);
            var billedto = Convert.ToInt32(param[1]);

            if (billedto == 0)
            {
                var query = context.Database.SqlQuery<CategoryMaster>("select TRANCSNAME,TRANIMPADDR1,TRANIMPADDR2,TRANIMPADDR3,TRANIMPADDR4,CHACATEGSTNO from CategoryMaster where CATETID =" + ids).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var query = context.Database.SqlQuery<VW_IMPORT_TRANSACTION_ADDRESS_DETAIL_ASSGN>("select IMPRTNAME,IMPCATEADDR1,IMPCATEADDR2,IMPCATEADDR3,IMPCATEADDR4,IMPCATEGSTNO from VW_IMPORT_TRANSACTION_ADDRESS_DETAIL_ASSGN where TRANMID=" + ids).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);

            }

        }

        [HttpPost]
        public ActionResult UpdateBchaNmae(FormCollection tab)
        {
            string status = "";
            using (var trans = context.Database.BeginTransaction())
            {
                try
                {
                    string TRANMID = Convert.ToString(tab["TRANMID"]);
                    //string OSMID = Convert.ToString(tab["OSMID"]);
                    string OSBILLREFNAME = Convert.ToString(tab["OSBILLREFNAME"]);
                    string OSBILLREFID = Convert.ToString(tab["OSBILLREFID"]);

                    string OSBBCHACATEAID = Convert.ToString(tab["OSBBCHACATEAID"]);
                    string OSBBCHACATEAGSTNO = Convert.ToString(tab["OSBBCHACATEAGSTNO"]);
                    string OSBBCHASTATEID = Convert.ToString(tab["OSBBCHASTATEID"]);
                    string OSBBCHAADDR1 = Convert.ToString(tab["OSBBCHAADDR1"]);
                    string OSBBCHAADDR2 = Convert.ToString(tab["OSBBCHAADDR2"]);
                    string OSBBCHAADDR3 = Convert.ToString(tab["OSBBCHAADDR3"]);
                    string OSBBCHAADDR4 = Convert.ToString(tab["OSBBCHAADDR4"]);

                    var query = context.Database.SqlQuery<TransactionDetail>("select  * from TransactionDetail where TRANMID=" + TRANMID).ToList();
                    for (int i = 0; i < query.Count; i++)
                    {
                        if (query[i].TRANDREFID > 0)
                        {
                            var query1 = context.Database.SqlQuery<OpenSheetDetail>("select  * from OpenSheetDetail where GIDID =" + query[i].TRANDREFID).ToList();

                            if (query1[0].OSMID > 0)
                            {
                                int OSMID = 0;
                                OSMID = query1[0].OSMID;
                                if (OSMID > 0)
                                {
                                    string osuquery = " UPDATE OPENSHEETMASTER SET OSBILLREFNAME = '" + Convert.ToString(OSBILLREFNAME) + "',";
                                    osuquery += " OSBILLREFID = " + Convert.ToInt32(OSBILLREFID) + ",";
                                    osuquery += " OSBBCHACATEAID = " + Convert.ToInt32(OSBBCHACATEAID) + ",";
                                    osuquery += " OSBBCHACATEAGSTNO = '" + Convert.ToString(OSBBCHACATEAGSTNO) + "', OSBBCHASTATEID = " + Convert.ToInt32(OSBBCHASTATEID) + ",";
                                    osuquery += " OSBBCHAADDR1 = '" + Convert.ToString(OSBBCHAADDR1) + "', OSBBCHAADDR2 = '" + Convert.ToString(OSBBCHAADDR2) + "',";
                                    osuquery += " OSBBCHAADDR3 = '" + Convert.ToString(OSBBCHAADDR3) + "', OSBBCHAADDR4 = '" + Convert.ToString(OSBBCHAADDR4) + "' WHERE OSMID =" + Convert.ToInt32(OSMID) + " ";
                                    context.Database.ExecuteSqlCommand(osuquery);
                                }
                            }
                        }
                    }
                    

                    if (TRANMID != "" || TRANMID != null || TRANMID != "0")
                    {
                        string uquery = " UPDATE TRANSACTIONMASTER SET TRANREFNAME = '" + Convert.ToString(OSBILLREFNAME) + "', TRANREFID = " + Convert.ToInt32(OSBILLREFID) + ",";
                        uquery += " CATEAID = " + Convert.ToInt32(OSBBCHACATEAID) + ", CATEAGSTNO = '" + Convert.ToString(OSBBCHACATEAGSTNO) + "',";
                        uquery += " STATEID = " + Convert.ToInt32(OSBBCHASTATEID) + ", ";
                        uquery += " TRANIMPADDR1 = '" + Convert.ToString(OSBBCHAADDR1) + "',";
                        uquery += " TRANIMPADDR2 = '" + Convert.ToString(OSBBCHAADDR2) + "',";
                        uquery += " TRANIMPADDR3 = '" + Convert.ToString(OSBBCHAADDR3) + "',";
                        uquery += " TRANIMPADDR4 = '" + Convert.ToString(OSBBCHAADDR4) + "' ";
                        uquery += "  WHERE TRANMID =" + Convert.ToInt32(TRANMID) + " ";
                        context.Database.ExecuteSqlCommand(uquery);

                    }

                    status = "saved";
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    status = ex.Message.ToString();
                    trans.Rollback();
                }
            }

            return Json(status, JsonRequestBehavior.AllowGet);
        }

        public void UpdateBillingName(FormCollection F_Form)
        {
            TransactionMaster transactionmaster = new TransactionMaster();
            Int32 TRANMID = Convert.ToInt32(F_Form["FTRANMID"]);
            Int32 OSBILLREFID = Convert.ToInt32(F_Form["OSBILLREFID"]);
            string OSBILLREFNAME = F_Form["OSBILLREFNAME"];
            Int16 OSBILLEDTO = Convert.ToInt16(F_Form["OSBILLEDTO"]);

            if (TRANMID != 0)
            {
                transactionmaster = context.transactionmaster.Find(TRANMID);
            }

            context.Entry(transactionmaster).Entity.TRANREFID = Convert.ToInt32(F_Form["OSBILLREFID"]);
            context.Entry(transactionmaster).Entity.TRANREFNAME = Convert.ToString(F_Form["OSBILLREFNAME"]);
            context.Entry(transactionmaster).Entity.TRANCSNAME = Convert.ToString(F_Form["OSBILLREFNAME"]);
            //context.Entry(transactionmaster).Entity.TRANIMPADDR2 = Convert.ToString(F_Form["TRANIMPADDR2"]);
            //context.Entry(transactionmaster).Entity.TRANIMPADDR3 = Convert.ToString(F_Form["TRANIMPADDR3"]);
            //context.Entry(transactionmaster).Entity.TRANIMPADDR4 = Convert.ToString(F_Form["TRANIMPADDR4"]);
            context.SaveChanges();

            InvoiceNameUpdate(TRANMID, OSBILLREFID, OSBILLEDTO, OSBILLREFNAME);

            Response.Write("Saved");
        }/*END*/



        public ActionResult AForm(string id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var param = id.Split('~');

            var ids = Convert.ToInt32(param[0]);
            //var gstamt = Convert.ToInt32(param[1]);

            ViewBag.id = ids;
            //ViewBag.FGSTAMT = gstamt;

            var query = context.Database.SqlQuery<pr_Invoice_Print_Customer_Detail_Assgn_Result>("EXEC pr_Invoice_Print_Customer_Detail_Assgn @PTranMID = " + ids).ToList();
            if (query[0].OSBILLREFNAME != null)
            {
                ViewBag.TRANCSNAME = query[0].OSBILLREFNAME;
                ViewBag.TRANBILLREFID = query[0].TRANREFID;
                ViewBag.TRANIMPADDR1 = query[0].CATEADDR1;
                ViewBag.TRANIMPADDR2 = query[0].CATEADDR2;
                ViewBag.TRANIMPADDR3 = query[0].CATEADDR3;
                ViewBag.TRANIMPADDR4 = query[0].CATEADDR4;
                ViewBag.CATEMAIL = query[0].CATEMAIL;
                ViewBag.CATEPHN1 = query[0].CATEPHN1;
                ViewBag.GSTNO = query[0].CATEGSTNO;
            }

            ViewBag.slbnrndiv = "show";
            ViewBag.SLABNARN_HANDLDESC = "";
            ViewBag.SLABNARN_TRANHANDAMT = "";
            ViewBag.SLABNARN_ADNLDESC = "";
            ViewBag.SLABNARN_ADNLAMT = "";
            var query2 = context.Database.SqlQuery<pr_Get_Import_Slab_Narration_Detail_Result>("EXEC pr_Get_Import_Slab_Narration_Detail @PTranMID = " + ids).ToList();
            if (query2.Count > 0)
            {
                if (query2[0].SLABTID == 4)
                {
                    ViewBag.SLABNARN_HANDLDESC = query2[0].SLABNARTN;
                    ViewBag.SLABNARN_TRANHANDAMT = Math.Round(Convert.ToDouble(query2[0].TranHandlingAmt),0);
                    if (query2.Count > 1)
                    {
                        if (query2[1].SLABTID == 16)
                        {
                            ViewBag.SLABNARN_ADNLDESC = query2[1].SLABNARTN;
                            ViewBag.SLABNARN_ADNLAMT = Math.Round(Convert.ToDouble(query2[1].TranAddnlAmt),0);
                        }

                    }
                }
                else if (query2[0].SLABTID == 16)
                {
                    ViewBag.SLABNARN_ADNLDESC = query2[0].SLABNARTN;
                    ViewBag.SLABNARN_ADNLAMT = Math.Round(Convert.ToDouble(query2[0].TranAddnlAmt),0);
                }

            }
            else
                ViewBag.slbnrndiv = "hide";


            return View();
        }


        [HttpPost]
        public JsonResult UpdateEMailMobile(FormCollection F_Form)
        {
            string CATEID = F_Form["TRANBILLREFID"].ToString();
            string CATEMAIL = F_Form["CATEMAIL"].ToString();
            string CATEPHN1 = F_Form["CATEPHN1"].ToString();
           
            string SLABNARN_HANDLDESC = F_Form["SLABNARN_HANDLDESC"].ToString().Trim().Replace("\"", "");
            string SLABNARN_TRANHANDAMT = F_Form["SLABNARN_TRANHANDAMT"].ToString();
            string SLABNARN_ADNLDESC = F_Form["SLABNARN_ADNLDESC"].ToString().Trim().Replace("\"", "");
            string SLABNARN_ADNLAMT = F_Form["SLABNARN_ADNLAMT"].ToString();
            string FTRANMID = F_Form["FTRANMID"].ToString();
            int SLABNARN_STS;
            SLABNARN_STS = 0;
            if (SLABNARN_TRANHANDAMT == "")
                SLABNARN_TRANHANDAMT = "0";
            if (SLABNARN_ADNLAMT == "")
                SLABNARN_ADNLAMT = "0";
            decimal snhamt = Convert.ToDecimal(SLABNARN_TRANHANDAMT);
            if (SLABNARN_HANDLDESC != "" || SLABNARN_ADNLDESC != "")
            { 
                SLABNARN_STS = 1;
            }
            try
            {
                if(SLABNARN_STS > 0)
                {
                    context.Database.ExecuteSqlCommand("update transactionmaster SET SLABNARN_HANDLDESC = '" + SLABNARN_HANDLDESC + "', SLABNARN_ADNLDESC= '" + SLABNARN_ADNLDESC + "', SLABNARN_STS = " + Convert.ToInt32(SLABNARN_STS) + " WHERE SDPTID = 1 AND TRANMID =" + Convert.ToInt32(FTRANMID));
                }
                
                var Query = context.Database.SqlQuery<CategoryMaster>("select * from CATEGORYMASTER where CateID=" + Convert.ToInt32(CATEID)).ToList();
                if (Query.Count() > 0)
                {
                    context.Database.ExecuteSqlCommand("Update CATEGORYMASTER set CATEMAIL  = '" + CATEMAIL + "', CATEPHN1 = '" + CATEPHN1 + "' where CateID = " + Convert.ToInt32(CATEID));
                    //Response.Write("Saved");
                    var sts = "Saved";
                    return Json(sts, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var sts = "Not Saved";
                    return Json(sts, JsonRequestBehavior.AllowGet);
                }

            }
            catch (Exception e)
            {
                return Json(e.Message, JsonRequestBehavior.AllowGet);
            }
        }


        public void InvoiceNameUpdate(int tranmid, int osbillrefid, Int16 osbilledto, string osbillrefname)
        {
            try
            {
                var Query = context.Database.SqlQuery<int>("select OSMID from ZW_IMPORT_INVOICE_BILLNAME_UPDATE_DETAIL_CHECK_ASSGN where TRANMID=" + tranmid).ToList();
                if (Query.Count() > 0)
                {
                    var osmid = Query[0];
                    context.Database.ExecuteSqlCommand("pr_Import_Invoice_Billing_Name_Update_Assgn @PTranMId = " + tranmid + ", @POSBillRefId  = " + osbillrefid + ", @PBilledto = " + osbilledto + ", @POSMId = " + osmid + " , @POSBillRefName='" + osbillrefname + "'");
                    // Response.Write("Saved Successfully");
                }
            }
            catch (Exception e) { Response.Write(e.Message); }
        }


        public void SaveAddress(FormCollection F_Form)
        {
            TransactionMaster transactionmaster = new TransactionMaster();
            Int32 TRANMID = Convert.ToInt32(F_Form["FTRANMID"]);

            if (TRANMID != 0)
            {
                transactionmaster = context.transactionmaster.Find(TRANMID);
            }

            context.Entry(transactionmaster).Entity.TRANCSNAME = Convert.ToString(F_Form["TRANCSNAME"]);
            context.Entry(transactionmaster).Entity.TRANIMPADDR1 = Convert.ToString(F_Form["TRANIMPADDR1"]);
            context.Entry(transactionmaster).Entity.TRANIMPADDR2 = Convert.ToString(F_Form["TRANIMPADDR2"]);
            context.Entry(transactionmaster).Entity.TRANIMPADDR3 = Convert.ToString(F_Form["TRANIMPADDR3"]);
            context.Entry(transactionmaster).Entity.TRANIMPADDR4 = Convert.ToString(F_Form["TRANIMPADDR4"]);
            context.SaveChanges();
            Response.Write("Saved");
        }/*END*/
        /*PRINT DETAIL*/
        public ActionResult CForm(int id)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            ViewBag.id = id;
            var query = context.Database.SqlQuery<TransactionMaster>("select * from TransactionMaster where TRANMID=" + id).ToList();

            var chaid = Convert.ToInt32(query[0].TRANREFID);
            var sql = context.Database.SqlQuery<CategoryMaster>("select * from CategoryMaster where CATEID=" + chaid).ToList();
            ViewBag.CATEMAIL = sql[0].CATEMAIL;

            var TmpAStr = "<table width='539' border='0' cellpadding='0' cellspacing='0' style='background:#FFCC66;border:1px solid #663300'><tr><Td colspan='2'  style='background:#663300;color:#FFFFFF;font-weight:700;text-align:center;padding:5px'>Import</Td> </tr> <tr>";
            TmpAStr = TmpAStr + " <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left'width='208' >Invoice No. </th> <td style='padding:4px;border-bottom:1px solid #663300'  width='331'>" + query[0].TRANDNO + "</td> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >Date </th>";
            TmpAStr = TmpAStr + " <td style='padding:4px;border-bottom:1px solid #663300' >" + query[0].TRANDATE + "</td> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >Bill Amount </th> <td style='padding:4px;border-bottom:1px solid #663300' >[[PInvAmt]]</td> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >CHA Name </th>";
            TmpAStr = TmpAStr + " <td style='padding:4px;border-bottom:1px solid #663300' >" + query[0].TRANREFNAME + "</td> </tr> </table> <br> <br> <table width='539' border='0' cellpadding='0' cellspacing='0' style='background:#FFCC66;border:1px solid #663300 '> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left'width='208' >SUDHARSHAN LOGISTICS PRIVATE LIMITED</th> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' ># 592, ENNORE EXPRESS HIGH ROAD,</th>";
            TmpAStr = TmpAStr + " </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >CHENNAI - 600 057. </th> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >Phone No. : 6545 5252 / 2573 3447 / 2573 3762</th> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >E-Mail Id : chennaicfs@sancotrans.com</th> </tr> </table>";

            ViewBag.SUB = "Import Invoice No." + query[0].TRANDNO;
            //ViewBag.MSG = TmpAStr;
            //ViewBag.TRANIMPADDR2 = sql[0].CATEADDR2;
            //ViewBag.TRANIMPADDR3 = sql[0].CATEADDR3;
            //ViewBag.TRANIMPADDR4 = sql[0].CATEADDR4;

            return View();
        }
        [HttpPost]
        public void Contact(FormCollection mysbfrm)
        {
            try
            {
                var query = context.Database.SqlQuery<TransactionMaster>("select * from TransactionMaster where TRANMID=" + Convert.ToInt32(mysbfrm["FTRANMID"])).ToList();
                var TmpAStr = "<table width='539' border='0' cellpadding='0' cellspacing='0' style='background:#FFCC66;border:1px solid #663300'><tr><Td colspan='2'  style='background:#663300;color:#FFFFFF;font-weight:700;text-align:center;padding:5px'>Import</Td> </tr> <tr>";
                TmpAStr = TmpAStr + " <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left'width='208' >Invoice No. </th> <td style='padding:4px;border-bottom:1px solid #663300'  width='331'>" + query[0].TRANDNO + "</td> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >Date </th>";
                TmpAStr = TmpAStr + " <td style='padding:4px;border-bottom:1px solid #663300' >" + query[0].TRANDATE + "</td> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >Bill Amount </th> <td style='padding:4px;border-bottom:1px solid #663300' >" + query[0].TRANNAMT + "</td> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >CHA Name </th>";
                TmpAStr = TmpAStr + " <td style='padding:4px;border-bottom:1px solid #663300' >" + query[0].TRANCSNAME + "</td> </tr> </table> <br> <br> <table width='539' border='0' cellpadding='0' cellspacing='0' style='background:#FFCC66;border:1px solid #663300 '> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left'width='208' >SUDHARSHAN LOGISTICS PRIVATE LIMITED</th> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >New No. 41, Redhills High Road, Andarkuppam, New Manali,</th>";
                TmpAStr = TmpAStr + " </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >CHENNAI - 600 103. </th> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >Phone No. : 7144 9000 </th> </tr> <tr> <th  style='padding:4px;border-bottom:1px solid #663300;border-right:1px solid #663300' align='left' >E-Mail Id : billcfs@sudharsan.co</th> </tr> </table>";

                var body = TmpAStr;
                var message = new MailMessage();
                message.To.Add(new MailAddress(mysbfrm["CATEMAIL"].ToString()));  // replace with valid value 
                if (mysbfrm["CATEMAILCC"].ToString() != "")
                    message.CC.Add(new MailAddress(address: mysbfrm["CATEMAILCC"].ToString(), displayName: Session["COMPNAME"].ToString()));
                //   message.CC.Add(new MailAddress(address: "dinesh@fusiontec.com", displayName: Session["COMPNAME"].ToString()));
                // if (mysbfrm["CATEMAILBCC"].ToString() != "")
                message.Bcc.Add(new MailAddress(address: "ssathya@sancotrans.com", displayName: Session["COMPNAME"].ToString()));
                // message.Bcc.Add(new MailAddress(address: "edp@sancotrans.com", displayName: Session["COMPNAME"].ToString()));
                message.Bcc.Add(new MailAddress(address: "ramaswamy@sancotrans.com", displayName: Session["COMPNAME"].ToString()));
                message.Bcc.Add(new MailAddress(address: "billcfs@sudharsan.co", displayName: Session["COMPNAME"].ToString()));
                message.Bcc.Add(new MailAddress(address: "chennaicfs@sudharsan.co", displayName: Session["COMPNAME"].ToString()));
                message.ReplyToList.Add(new MailAddress("billcfs@sudharsan.co"));
                message.From = new MailAddress(address: "billcfs@sudharsan.co", displayName: Session["COMPNAME"].ToString());  // replace with valid value
                message.Subject = mysbfrm["TMPSUB"].ToString();

                message.Body = TmpAStr;
                message.IsBodyHtml = true;
                message.Attachments.Add(new Attachment("E:\\CFS\\" + Session["CUSRID"] + "\\ImportInv\\" + query[0].TRANNO + ".pdf"));
                using (var smtp = new SmtpClient())
                {
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential("billcfs@sudharsan.co", "Cfs2billing@24");//Billcfs@963

                    // smtp.Host = "smtp.gmail.com";
                    smtp.Host = "mail.sudharsan.co";
                    smtp.Port = 587;
                    smtp.EnableSsl = false;
                    smtp.Send(message);

                }

                Response.Write("Success");
            }
            catch (Exception ex)
            {
                Response.Write(ex.Message);
            }
        }
        //public void Contact(FormCollection mysbfrm)
        //{

        //        var body = "<p>Email From: {0} ({1})</p><p>Message:</p><p>{2}</p>";
        //        var message = new MailMessage();
        //        message.To.Add(new MailAddress("name@gmail.com")); //replace with valid value
        //        message.Subject = "Your email subject";
        //        message.Body = string.Format(body, model.FromName, model.FromEmail, model.Message);
        //        message.IsBodyHtml = true;
        //        if (model.Upload != null && model.Upload.ContentLength > 0)
        //        {
        //            message.Attachments.Add(new Attachment(model.Upload.InputStream, Path.GetFileName(model.Upload.FileName)));
        //        }
        //        using (var smtp = new SmtpClient())
        //        {
        //            await smtp.SendMailAsync(message);
        //           // return RedirectToAction("Sent");
        //        }

        //}


    

        [Authorize(Roles = "ImportInvoicePrint")]
        public void APrintView(int id)
        {
            // Response.Write(@"10.10.5.5"); Response.End();
            //  ........delete TMPRPT...//
            //var param = id.Split(';');

            var ids = id;
            

            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "IMPORTINVOICE", Convert.ToInt32(ids), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;



                // cryRpt.Load(Server.MapPath("~/") + "//Reports//RPT_0302.rpt");


                //........Get TRANPCOUNT...//
                var Query = context.Database.SqlQuery<TransactionMaster>("select * from transactionmaster where TRANMID=" + ids).ToList();
                var PCNT = 0;
                var slbnrnsts = 0;

                if (Query.Count() != 0) { PCNT = Convert.ToInt32(Query[0].TRANPCOUNT);
                    slbnrnsts = Query[0].SLABNARN_STS; }
                var TRANPCOUNT = ++PCNT;
                // Response.Write(++PCNT);
                // Response.End();

                context.Database.ExecuteSqlCommand("UPDATE transactionmaster SET TRANPCOUNT=" + TRANPCOUNT + " WHERE TRANMID=" + ids);

                //if (rpttype == 0)
                if(slbnrnsts == 0 )
                    cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "gst_import_Invoice_rpt_E01.RPT");
                else
                    cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "gst_import_Invoice_rpt_E03.RPT");
                //else cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "import_Invoice_group_rpt_E01.RPT");

                cryRpt.RecordSelectionFormula = "{VW_IMPORT_TRANSACTION_GST_PRINT_ASSGN.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_IMPORT_TRANSACTION_GST_PRINT_ASSGN.TRANMID} = " + ids;



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
                //string path = "E:\\CFS\\" + Session["CUSRID"] + "\\ImportInv";
                //if (!(Directory.Exists(path)))
                //{
                //    Directory.CreateDirectory(path);
                //}
                //cryRpt.ExportToDisk(ExportFormatType.PortableDocFormat, path + "\\" + Query[0].TRANNO + ".pdf");
                //  cryRpt.SaveAs(path+ "\\"+Query[0].TRANNO+".pdf");
                cryRpt.ExportToHttpResponse(ExportFormatType.PortableDocFormat, System.Web.HttpContext.Current.Response, false, "");
                cryRpt.Close();
                cryRpt.Dispose();
                GC.Collect();
                stringbuilder.Clear();
            }

        }
        //end
        //...............Delete Row.............
        [Authorize(Roles = "ImportInvoiceDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            String fld = Request.Form.Get("fld");
            String temp = Delete_fun.delete_check1(fld, id);

            if (temp.Equals("PROCEED"))
            {
                TransactionMaster transactionmaster = context.transactionmaster.Find(Convert.ToInt32(id));
                context.transactionmaster.Remove(transactionmaster);
                context.SaveChanges();
                Response.Write("Deleted successfully...");
            }
            else
                Response.Write(temp);

        }//-----End of Delete Row

        // ========================= Edit Log Pages =========================
        public ActionResult EditLog()
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }
            return View();
        }

        public ActionResult EditLogInvoice(int? tranmid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var list = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                string gidnoParam = tranmid.HasValue ? tranmid.Value.ToString() : null;
                
                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"SELECT TOP 2000 [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [Modules] = 'ImportInvoice'
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
                var dictBank = context.bankmasters.ToDictionary(x => x.BANKMID, x => x.BANKMDESC);
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTariff = context.tariffmasters.ToDictionary(x => x.TARIFFMID, x => x.TARIFFMDESC);
                var dictTariffGrp = context.tariffgroupmasters.ToDictionary(x => x.TGID, x => x.TGDESC);
                var dictTranMode = context.transactionmodemaster.ToDictionary(x => x.TRANMODE, x => x.TRANMODEDETL);

                string Map(string field, string raw)
                {
                    if (raw == null) return raw;
                    var f = (field ?? string.Empty).Trim();
                    var val = raw.Trim();
                    if (string.IsNullOrEmpty(val)) return raw;
                    int ival;
                    switch (f.ToUpperInvariant())
                    {
                        case "BANKMID":
                            return int.TryParse(val, out ival) && dictBank.ContainsKey(ival) ? dictBank[ival] : raw;
                        case "LCATEID":
                            return int.TryParse(val, out ival) && dictCate.ContainsKey(ival) ? dictCate[ival] : raw;
                        case "TARIFFMID":
                            return int.TryParse(val, out ival) && dictTariff.ContainsKey(ival) ? dictTariff[ival] : raw;
                        case "TARIFFGID":
                            return int.TryParse(val, out ival) && dictTariffGrp.ContainsKey(ival) ? dictTariffGrp[ival] : raw;
                        case "TRANMODE":
                            return int.TryParse(val, out ival) && dictTranMode.ContainsKey(ival) ? dictTranMode[ival] : raw;
                        case "TRANBTYPE":
                            return val == "1" ? "Load" : val == "2" ? "Destuff" : raw;
                        case "DISPSTATUS":
                            return val == "1" ? "CANCELLED" : val == "0" ? "INBOOKS" : raw;
                        case "TRANOTYPE":
                            var dictOperation = context.ImportDestuffSlipOperation.ToDictionary(x => x.OPRTYPE, x => x.OPRTYPEDESC);
                            return int.TryParse(val, out ival) && dictOperation.ContainsKey(ival) ? dictOperation[ival] : raw;
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
                        case "TRANDATE": return "Transaction Date";
                        case "TRANTIME": return "Transaction Time";
                        case "TRANNO": return "Transaction No";
                        case "TRANDNO": return "Transaction Detail No";
                        case "TRANREFID": return "Reference ID";
                        case "TRANREFNAME": return "Reference Name";
                        case "LCATEID": return "Labour";
                        case "TRANBTYPE": return "Bill Type";
                        case "TRANMODE": return "Transaction Mode";
                        case "TRANMODEDETL": return "Mode Detail";
                        case "TRANREFNO": return "Reference No";
                        case "TRANREFDATE": return "Reference Date";
                        case "TRANREFAMT": return "Amount";
                        case "TRANROAMT": return "Round Off Amount";
                        case "TRANGAMT": return "Gross Amount";
                        case "TRANSAMT": return "Storage Amount";
                        case "TRANHAMT": return "Handling Amount";
                        case "TRANNAMT": return "Net Amount";
                        case "TRANLMID": return "Lorry Memo ID";
                        case "TRANLMNO": return "Lorry Memo No";
                        case "TRANLSID": return "Lorry Slip ID";
                        case "TRANLSNO": return "Lorry Slip No";
                        case "TRANRMKS": return "Remarks";
                        case "BANKMID": return "Bank";
                        case "REGSTRID": return "Registration ID";
                        case "DISPSTATUS": return "Status";
                        case "PRCSDATE": return "Process Date";
                        case "TRANEAMT": return "Escort Amount";
                        case "TRANFAMT": return "Fuel";
                        case "TRANTCAMT": return "Total Charge Amount";
                        case "TRANAAMT": return "Additional Amount";
                        case "TRANADONAMT": return "Add on";
                        case "TRANINOC1": return "Invoice OC1";
                        case "TRANINOC2": return "Invoice OC2";
                        case "TRANCGSTAMT": return "CGST Amount";
                        case "TRANSGSTAMT": return "SGST Amount";
                        case "TRANIGSTAMT": return "IGST Amount";
                        case "STRG_HSNCODE": return "Storage HSN Code";
                        case "HANDL_HSNCODE": return "Handling HSN Code";
                        case "STRG_TAXABLE_AMT": return "Storage Taxable Amount";
                        case "HANDL_TAXABLE_AMT": return "Handling Taxable Amount";
                        case "STRG_CGST_EXPRN": return "Storage CGST %";
                        case "STRG_SGST_EXPRN": return "Storage SGST %";
                        case "STRG_IGST_EXPRN": return "Storage IGST %";
                        case "STRG_CGST_AMT": return "Storage CGST Amount";
                        case "STRG_SGST_AMT": return "Storage SGST Amount";
                        case "STRG_IGST_AMT": return "Storage IGST Amount";
                        case "HANDL_CGST_EXPRN": return "Handling CGST %";
                        case "HANDL_SGST_EXPRN": return "Handling SGST %";
                        case "HANDL_IGST_EXPRN": return "Handling IGST %";
                        case "TRANBILLREFNO": return "Bill Reference No";
                        case "CUSTGID": return "Customer Group ID";
                        case "TARIFFGID": return "Tariff Group";
                        case "TARIFFMID": return "Tariff";
                        case "TRANOTYPE": return "Operation";
                        case "TRANVDATE": return "Validity Date";
                        case "TRANDPNO": return "Duty Paid No.";
                        case "TRANDSAMT": return "Storage(Calc)";
                        case "TRANCFID": return "costfactor details";
                        case "TRANEDATE": return "Charge Date";
                        case "TRANDHAMT": return "Handling";
                        case "TRAN_PULSE_STRG_TYPE": return "Storage(Calc)";
                        case "DPAIDNO": return "Duty Paid No";
                        case "CFID": return "Costfactordetails";
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
            catch { /* Best-effort mapping */ }

            ViewBag.Module = "ImportInvoice";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        // Compare two versions for a given TRANMID
        public ActionResult EditLogInvoiceCompare(int? tranmid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            if (tranmid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide TRANMID, Version A and Version B to compare.";
                return RedirectToAction("EditLogInvoice", new { tranmid = tranmid });
            }

            versionA = (versionA ?? string.Empty).Trim();
            versionB = (versionB ?? string.Empty).Trim();
            string gidnoString = tranmid.HasValue ? tranmid.Value.ToString() : "";

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
                                                WHERE [GIDNO]=@GIDNO AND [Modules]='ImportInvoice' AND RTRIM(LTRIM([Version]))=@V", sql))
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
                                Modules = r["Modules"] == DBNull.Value ? null : Convert.ToString(r["Modules"])
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
                                Modules = r2["Modules"] == DBNull.Value ? null : Convert.ToString(r2["Modules"])
                            });
                        }
                    }
                }
            }

            // Map technical field names to friendly form labels
            try
            {
                var dictBank = context.bankmasters.ToDictionary(x => x.BANKMID, x => x.BANKMDESC);
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTariff = context.tariffmasters.ToDictionary(x => x.TARIFFMID, x => x.TARIFFMDESC);
                var dictTariffGrp = context.tariffgroupmasters.ToDictionary(x => x.TGID, x => x.TGDESC);
                var dictTranMode = context.transactionmodemaster.ToDictionary(x => x.TRANMODE, x => x.TRANMODEDETL);

                string Map(string field, string raw)
                {
                    if (raw == null) return raw;
                    var f = (field ?? string.Empty).Trim();
                    var val = raw.Trim();
                    if (string.IsNullOrEmpty(val)) return raw;
                    int ival;
                    switch (f.ToUpperInvariant())
                    {
                        case "BANKMID":
                            return int.TryParse(val, out ival) && dictBank.ContainsKey(ival) ? dictBank[ival] : raw;
                        case "LCATEID":
                            return int.TryParse(val, out ival) && dictCate.ContainsKey(ival) ? dictCate[ival] : raw;
                        case "TARIFFMID":
                            return int.TryParse(val, out ival) && dictTariff.ContainsKey(ival) ? dictTariff[ival] : raw;
                        case "TARIFFGID":
                            return int.TryParse(val, out ival) && dictTariffGrp.ContainsKey(ival) ? dictTariffGrp[ival] : raw;
                        case "TRANMODE":
                            return int.TryParse(val, out ival) && dictTranMode.ContainsKey(ival) ? dictTranMode[ival] : raw;
                        case "TRANBTYPE":
                            return val == "1" ? "Load" : val == "2" ? "Destuff" : raw;
                        case "DISPSTATUS":
                            return val == "1" ? "CANCELLED" : val == "0" ? "INBOOKS" : raw;
                        case "TRANOTYPE":
                            var dictOperation = context.ImportDestuffSlipOperation.ToDictionary(x => x.OPRTYPE, x => x.OPRTYPEDESC);
                            return int.TryParse(val, out ival) && dictOperation.ContainsKey(ival) ? dictOperation[ival] : raw;
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
                        case "TRANDATE": return "Transaction Date";
                        case "TRANTIME": return "Transaction Time";
                        case "TRANNO": return "Transaction No";
                        case "TRANDNO": return "Transaction Detail No";
                        case "TRANREFID": return "Reference ID";
                        case "TRANREFNAME": return "Reference Name";
                        case "LCATEID": return "Labour";
                        case "TRANBTYPE": return "Bill Type";
                        case "TRANMODE": return "Transaction Mode";
                        case "TRANMODEDETL": return "Mode Detail";
                        case "TRANREFNO": return "Reference No";
                        case "TRANREFDATE": return "Reference Date";
                        case "TRANREFAMT": return "Amount";
                        case "TRANROAMT": return "Round Off Amount";
                        case "TRANGAMT": return "Gross Amount";
                        case "TRANSAMT": return "Storage Amount";
                        case "TRANHAMT": return "Handling Amount";
                        case "TRANNAMT": return "Net Amount";
                        case "TRANLMID": return "Lorry Memo ID";
                        case "TRANLMNO": return "Lorry Memo No";
                        case "TRANLSID": return "Lorry Slip ID";
                        case "TRANLSNO": return "Lorry Slip No";
                        case "TRANRMKS": return "Remarks";
                        case "BANKMID": return "Bank";
                        case "REGSTRID": return "Registration ID";
                        case "DISPSTATUS": return "Status";
                        case "PRCSDATE": return "Process Date";
                        case "TRANEAMT": return "Escort Amount";
                        case "TRANFAMT": return "Fuel";
                        case "TRANTCAMT": return "Total Charge Amount";
                        case "TRANAAMT": return "Additional Amount";
                        case "TRANADONAMT": return "Add on";
                        case "TRANINOC1": return "Invoice OC1";
                        case "TRANINOC2": return "Invoice OC2";
                        case "TRANCGSTAMT": return "CGST Amount";
                        case "TRANSGSTAMT": return "SGST Amount";
                        case "TRANIGSTAMT": return "IGST Amount";
                        case "STRG_HSNCODE": return "Storage HSN Code";
                        case "HANDL_HSNCODE": return "Handling HSN Code";
                        case "STRG_TAXABLE_AMT": return "Storage Taxable Amount";
                        case "HANDL_TAXABLE_AMT": return "Handling Taxable Amount";
                        case "STRG_CGST_EXPRN": return "Storage CGST %";
                        case "STRG_SGST_EXPRN": return "Storage SGST %";
                        case "STRG_IGST_EXPRN": return "Storage IGST %";
                        case "STRG_CGST_AMT": return "Storage CGST Amount";
                        case "STRG_SGST_AMT": return "Storage SGST Amount";
                        case "STRG_IGST_AMT": return "Storage IGST Amount";
                        case "HANDL_CGST_EXPRN": return "Handling CGST %";
                        case "HANDL_SGST_EXPRN": return "Handling SGST %";
                        case "HANDL_IGST_EXPRN": return "Handling IGST %";
                        case "TRANBILLREFNO": return "Bill Reference No";
                        case "CUSTGID": return "Customer Group ID";
                        case "TARIFFGID": return "Tariff Group";
                        case "TARIFFMID": return "Tariff";
                        case "TRANOTYPE": return "Operation";
                        case "TRANVDATE": return "Validity Date";
                        case "TRANDPNO": return "Duty Paid No.";
                        case "TRANDSAMT": return "Storage(Calc)";
                        case "TRANCFID": return "costfactor details";
                        case "TRANEDATE": return "Charge Date";
                        case "TRANDHAMT": return "Handling";
                        case "TRAN_PULSE_STRG_TYPE": return "Storage(Calc)";
                        case "DPAIDNO": return "Duty Paid No";
                        case "CFID": return "Costfactordetails";
                        default: return field;
                    }
                }

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
            ViewBag.Module = "ImportInvoice";
            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        // ========================= Edit Logging Helper Methods =========================
        private void LogTransactionEdits(TransactionMaster before, TransactionMaster after, string userId, SCFSERPContext context, List<TransactionDetail> beforeDetails = null)
        {
            if (before == null || after == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANMID", "COMPYID", "SDPTID", "PRCSDATE", "LMUSRID", "CUSRID",
                "TRANTID", "TRANPCOUNT", "TRANCSNAME", "LEMID", "TRANAHAMT",
                "TRANHBLNO", "TRANPONO", "TRANIMPADDR1", "TRANIMPADDR2", "TRANIMPADDR3", "TRANIMPADDR4",
                "SLABNARN_HANDLDESC", "SLABNARN_ADNLDESC", "SLABNARN_STS",
                "TRANTALLYCHAID", "TRANTALLYCHANAME", "TCATEAID", "TCATEAGSTNO", "TSTATEID",
                "TALLYSTAT", "IRNNO", "ACKNO", "ACKDT", "QRCODEPATH", "CATEAID", "STATEID", "CATEAGSTNO",
                "TRANGSTNO", "TRANPAMT", "TRANREFBNAME", "TRANAMTWRDS", "TRANLMDATE", "TRANLSDATE", "TRANNARTN",
                "HANDL_CGST_AMT", "HANDL_SGST_AMT", "HANDL_IGST_AMT", "HANDL_TAXABLE_AMT"
            };

            var gidno = after.TRANMID.ToString();
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
                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'ImportInvoice'", sql))
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
            
            // Track logged field changes to prevent duplicates within the same save operation
            // Key format: "FieldName|OldValue|NewValue" (normalized)
            var loggedChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Helper method to create a unique key for a field change
            string GetChangeKey(string fieldName, string oldValue, string newValue)
            {
                // Normalize NULL values to empty string for comparison
                var oldVal = string.IsNullOrEmpty(oldValue) ? "NULL" : oldValue.Trim();
                var newVal = string.IsNullOrEmpty(newValue) ? "NULL" : newValue.Trim();
                return $"{fieldName}|{oldVal}|{newVal}";
            }
            
            // Wrapper method that checks for duplicates before inserting
            void InsertEditLogRowWithDedup(string fieldName, string oldValue, string newValue)
            {
                var changeKey = GetChangeKey(fieldName, oldValue, newValue);
                if (loggedChanges.Contains(changeKey))
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping duplicate log entry: Field={fieldName}, OldValue={oldValue}, NewValue={newValue}");
                    return;
                }
                
                loggedChanges.Add(changeKey);
                InsertEditLogRow(cs.ConnectionString, gidno, fieldName, oldValue, newValue, userId, versionLabel, "ImportInvoice");
            }
            
            var props = typeof(TransactionMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                
                InsertEditLogRowWithDedup(p.Name, os, ns);
            }
            
            // Log TransactionDetail changes
            try
            {
                // Use provided beforeDetails if available, otherwise load from database
                if (beforeDetails == null)
                {
                    beforeDetails = context.transactiondetail.AsNoTracking().Where(x => x.TRANMID == before.TRANMID).ToList();
                }
                var afterDetails = context.transactiondetail.AsNoTracking().Where(x => x.TRANMID == after.TRANMID).ToList();
                
                System.Diagnostics.Debug.WriteLine($"LogTransactionEdits: beforeDetails.Count={beforeDetails?.Count ?? 0}, afterDetails.Count={afterDetails?.Count ?? 0}");
                
                var beforeDict = beforeDetails.ToDictionary(x => x.TRANDID, x => x);
                var afterDict = afterDetails.ToDictionary(x => x.TRANDID, x => x);
                
                // Use HashSet to ensure we only process each detail record once
                var allDetailIds = new HashSet<int>();
                foreach (var d in beforeDetails) 
                {
                    if (d.TRANDID > 0) allDetailIds.Add(d.TRANDID);
                }
                foreach (var d in afterDetails) 
                {
                    if (d.TRANDID > 0) allDetailIds.Add(d.TRANDID);
                }
                
                // Track which detail records we've already logged to prevent duplicates
                var loggedDetailIds = new HashSet<int>();
                
                foreach (var detailId in allDetailIds)
                {
                    // Skip if we've already logged this detail record (duplicate prevention)
                    if (loggedDetailIds.Contains(detailId))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping duplicate detail TRANDID={detailId}");
                        continue;
                    }
                    
                    var beforeDetail = beforeDict.ContainsKey(detailId) ? beforeDict[detailId] : null;
                    var afterDetail = afterDict.ContainsKey(detailId) ? afterDict[detailId] : null;
                    
                    if (afterDetail == null) continue;
                    
                    // Mark this detail as logged to prevent duplicate processing
                    loggedDetailIds.Add(detailId);
                    
                    // Debug: Log what we're comparing
                    if (beforeDetail != null)
                    {
                        var beforeTariff = beforeDetail.TARIFFMID;
                        System.Diagnostics.Debug.WriteLine($"Logging detail TRANDID={detailId}: before.TARIFFMID={beforeTariff}, after.TARIFFMID={afterDetail.TARIFFMID}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Logging detail TRANDID={detailId}: before=null, after.TARIFFMID={afterDetail.TARIFFMID}");
                    }
                    
                    LogTransactionDetailEdits(beforeDetail, afterDetail, gidno, userId, versionLabel, context, loggedChanges);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log TransactionDetail changes: {ex.Message}");
            }
        }
        
        private void LogTransactionDetailEdits(TransactionDetail before, TransactionDetail after, string gidno, string userId, string versionLabel, SCFSERPContext context, HashSet<string> loggedChanges = null)
        {
            if (after == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            // Get V0 baseline values for this record to use as OldValue when before is null/empty
            Dictionary<string, string> v0BaselineValues = null;
            try
            {
                v0BaselineValues = GetV0BaselineValues(cs.ConnectionString, gidno, "ImportInvoice");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load V0 baseline values: {ex.Message}");
            }
            
            // Wrapper method that checks for duplicates before inserting
            void InsertEditLogRowWithDedup(string fieldName, string oldValue, string newValue)
            {
                if (loggedChanges != null)
                {
                    // Helper method to create a unique key for a field change
                    string GetChangeKey(string fn, string ov, string nv)
                    {
                        var oldVal = string.IsNullOrEmpty(ov) ? "NULL" : ov.Trim();
                        var newVal = string.IsNullOrEmpty(nv) ? "NULL" : nv.Trim();
                        return $"{fn}|{oldVal}|{newVal}";
                    }
                    
                    var changeKey = GetChangeKey(fieldName, oldValue, newValue);
                    if (loggedChanges.Contains(changeKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping duplicate log entry: Field={fieldName}, OldValue={oldValue}, NewValue={newValue}");
                        return;
                    }
                    
                    loggedChanges.Add(changeKey);
                }
                
                InsertEditLogRow(cs.ConnectionString, gidno, fieldName, oldValue, newValue, userId, versionLabel, "ImportInvoice");
            }

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANDID", "TRANMID", "BILLEDID", "SLABTID", "TRANDREFID",
                "TRANIDATE", "TRANSDATE",
                "TRANVHLFROM", "TRANVHLTO", "DISPSTATUS", "STFDID", "SBDID", "TRANDAID",
                "RCOL1", "RCOL2", "RCOL3", "RCOL4", "RCOL5", "RCOL6", "RCOL7",
                "RAMT1", "RAMT2", "RAMT3", "RAMT4", "RAMT5", "RAMT6", "RAMT7",
                "RCAMT1", "RCAMT2", "RCAMT3", "RCAMT4", "RCAMT5", "RCAMT6", "RCAMT7",
                "TRANDQTY", "TRANDRATE", "TRANDWGHT", "TRANDNOP",
                "TRANDEAMT", "TRANDFAMT", "TRANDPAMT", "TRANDAAMT", "TRANDNAMT",
                "TRAND_COVID_DISC_AMT", "TRANDADONAMT",
                "TRAND_STRG_CGST_AMT", "TRAND_STRG_SGST_AMT", "TRAND_STRG_IGST_AMT",
                "TRAND_HANDL_CGST_AMT", "TRAND_HANDL_SGST_AMT", "TRAND_HANDL_IGST_AMT",
                "TRAND_HANDL_CGST_EXPRN", "TRAND_HANDL_SGST_EXPRN", "TRAND_HANDL_IGST_EXPRN"
            };

            var props = typeof(TransactionDetail).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                var ov = before != null ? p.GetValue(before, null) : null;
                var nv = p.GetValue(after, null);

                // Always log TARIFFMID changes, even if old value is null
                if (p.Name.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't skip if TARIFFMID - we want to log even if old value is null/0
                }
                else
                {
                    if (BothNull(ov, nv)) continue;
                }

                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                bool changed;

                if (type == typeof(decimal))
                {
                    var d1 = ToNullableDecimal(ov) ?? 0m;
                    var d2 = ToNullableDecimal(nv) ?? 0m;
                    if (d1 == 0m && d2 == 0m) continue;
                    changed = d1 != d2;
                }
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                {
                    var i1 = Convert.ToInt64(ov ?? 0);
                    var i2 = Convert.ToInt64(nv ?? 0);
                    // Always log TARIFFMID changes, even if one value is 0
                    if (p.Name.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                    {
                        changed = i1 != i2;
                    }
                    else
                    {
                        if (i1 == 0 && i2 == 0) continue;
                        changed = i1 != i2;
                    }
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
                    bool def1 = string.IsNullOrEmpty(s1) || s1 == "-" || s1 == "0" || s1 == "0.0" || s1 == "0.00";
                    bool def2 = string.IsNullOrEmpty(s2) || s2 == "-" || s2 == "0" || s2 == "0.0" || s2 == "0.00";
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

                // Get old value - prioritize V0 baseline (has formatted descriptions), then format before object value
                string os = "";
                
                // First, try to get from V0 baseline (this has the formatted display values)
                if (v0BaselineValues != null && v0BaselineValues.ContainsKey(p.Name) && !string.IsNullOrEmpty(v0BaselineValues[p.Name]))
                {
                    os = v0BaselineValues[p.Name];
                }
                else
                {
                    // If no V0 baseline, format the before object value
                    if (ov != null)
                    {
                        os = FormatValForLoggingDetail(p.Name, ov);
                    }
                    else
                    {
                        // If before object is null and no V0 baseline, use empty string
                        os = "";
                    }
                }
                
                var ns = FormatValForLoggingDetail(p.Name, nv);

                // Additional validation: Skip if formatted old and new values are effectively the same
                var osTrimmed = (os ?? "").Trim();
                var nsTrimmed = (ns ?? "").Trim();
                if (string.Equals(osTrimmed, nsTrimmed, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping {p.Name}: Formatted values are identical (os='{osTrimmed}', ns='{nsTrimmed}')");
                    continue;
                }

                // Debug logging to verify values are being captured
                System.Diagnostics.Debug.WriteLine($"Logging {p.Name}: OldValue='{os}', NewValue='{ns}', before.ov={ov}, v0BaselineExists={v0BaselineValues != null && v0BaselineValues.ContainsKey(p.Name)}");

                InsertEditLogRowWithDedup(p.Name, os ?? "", ns ?? "");
                
                // If TARIFFMID changed, also log TARIFFGID (Tariff Group)
                if (p.Name.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        int oldTariffId = 0, newTariffId = 0;
                        if (ov != null)
                        {
                            if (ov is int) oldTariffId = (int)ov;
                            else if (ov is short) oldTariffId = (short)ov;
                            else if (ov is long) oldTariffId = (int)(long)ov;
                            else int.TryParse(Convert.ToString(ov), out oldTariffId);
                        }
                        
                        // If oldTariffId is 0, try to get from V0 baseline
                        if (oldTariffId == 0 && v0BaselineValues != null && v0BaselineValues.ContainsKey("TARIFFMID"))
                        {
                            var v0TariffDesc = v0BaselineValues["TARIFFMID"];
                            if (!string.IsNullOrEmpty(v0TariffDesc))
                            {
                                // Try to find tariff by description
                                var v0Tariff = context.tariffmasters.FirstOrDefault(t => t.TARIFFMDESC == v0TariffDesc);
                                if (v0Tariff != null)
                                {
                                    oldTariffId = v0Tariff.TARIFFMID;
                                }
                            }
                        }
                        
                        if (nv != null)
                        {
                            if (nv is int) newTariffId = (int)nv;
                            else if (nv is short) newTariffId = (short)nv;
                            else if (nv is long) newTariffId = (int)(long)nv;
                            else int.TryParse(Convert.ToString(nv), out newTariffId);
                        }
                        
                        // Get old TARIFFGID (Tariff Group) value
                        string oldTariffGid = "";
                        if (oldTariffId > 0)
                        {
                            var oldTariff = context.tariffmasters.FirstOrDefault(t => t.TARIFFMID == oldTariffId);
                            if (oldTariff != null && oldTariff.TGID.HasValue)
                            {
                                var oldTariffGrp = context.tariffgroupmasters.FirstOrDefault(tg => tg.TGID == oldTariff.TGID.Value);
                                if (oldTariffGrp != null && !string.IsNullOrEmpty(oldTariffGrp.TGDESC))
                                    oldTariffGid = oldTariffGrp.TGDESC;
                            }
                        }
                        else
                        {
                            // Old value was 0 or null - try to get from V0 baseline
                            if (v0BaselineValues != null && v0BaselineValues.ContainsKey("TARIFFGID"))
                            {
                                oldTariffGid = v0BaselineValues["TARIFFGID"];
                            }
                            else
                            {
                                // No V0 baseline found - log as empty to show no tariff was selected
                                oldTariffGid = "";
                            }
                        }
                        
                        // Get new TARIFFGID (Tariff Group) value
                        string newTariffGid = "";
                        if (newTariffId > 0)
                        {
                            var newTariff = context.tariffmasters.FirstOrDefault(t => t.TARIFFMID == newTariffId);
                            if (newTariff != null && newTariff.TGID.HasValue)
                            {
                                var newTariffGrp = context.tariffgroupmasters.FirstOrDefault(tg => tg.TGID == newTariff.TGID.Value);
                                if (newTariffGrp != null && !string.IsNullOrEmpty(newTariffGrp.TGDESC))
                                    newTariffGid = newTariffGrp.TGDESC;
                            }
                        }
                        else
                        {
                            // New value is 0 or null - log as empty to show no tariff is selected
                            newTariffGid = "";
                        }
                        
                        // Only log TARIFFGID if it actually changed or if TARIFFMID changed
                        if (oldTariffGid != newTariffGid || oldTariffId != newTariffId)
                        {
                            InsertEditLogRowWithDedup("TARIFFGID", oldTariffGid ?? "", newTariffGid ?? "");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log TARIFFGID change: {ex.Message}");
                    }
                }
            }
        }
        
        private void LogTransactionMasterFactorEdits(List<TransactionMasterFactor> before, List<TransactionMasterFactor> after, string gidno, string userId, SCFSERPContext context)
        {
            if (after == null) after = new List<TransactionMasterFactor>();
            if (before == null) before = new List<TransactionMasterFactor>();
            
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;
            
            // Get version label
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
                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'ImportInvoice'", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", gidno);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        nextVersion = Convert.ToInt32(obj);
                }
            }
            catch { /* ignore version errors */ }
            
            var versionLabel = $"V{nextVersion}-{gidno}";
            
            // Get cost factor descriptions for mapping
            var costFactorDict = context.costfactormasters.ToDictionary(x => x.CFID, x => x.CFDESC);
            
            // Create sets of CFIDs for comparison
            var beforeCFIDs = before.Select(x => x.CFID).OrderBy(x => x).ToList();
            var afterCFIDs = after.Select(x => x.CFID).OrderBy(x => x).ToList();
            
            // Check if CFIDs changed
            if (!beforeCFIDs.SequenceEqual(afterCFIDs))
            {
                // Format old and new values as comma-separated cost factor descriptions
                string oldValue = "";
                if (beforeCFIDs.Any())
                {
                    oldValue = string.Join(", ", beforeCFIDs.Select(cfid => 
                        costFactorDict.ContainsKey(cfid) ? costFactorDict[cfid] : cfid.ToString()));
                }
                else
                {
                    oldValue = "";
                }
                
                string newValue = "";
                if (afterCFIDs.Any())
                {
                    newValue = string.Join(", ", afterCFIDs.Select(cfid => 
                        costFactorDict.ContainsKey(cfid) ? costFactorDict[cfid] : cfid.ToString()));
                }
                else
                {
                    newValue = "";
                }
                
                // Only log if values actually changed
                if (oldValue != newValue)
                {
                    InsertEditLogRow(cs.ConnectionString, gidno, "CFID", oldValue, newValue, userId, versionLabel, "ImportInvoice");
                }
            }
        }
        
        private string FormatValForLoggingDetail(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            
            try
            {
                if (fieldName.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle null or empty values - return empty string to show old value was null/0
                    if (string.IsNullOrEmpty(formattedValue) || formattedValue == "0")
                        return "";
                    
                    int tariffId;
                    if (int.TryParse(formattedValue, out tariffId) && tariffId > 0)
                    {
                        var tariff = context.tariffmasters.FirstOrDefault(t => t.TARIFFMID == tariffId);
                        if (tariff != null && !string.IsNullOrEmpty(tariff.TARIFFMDESC))
                            return tariff.TARIFFMDESC;
                    }
                    // If lookup fails, return the ID value
                    return formattedValue;
                }
                else if (fieldName.Equals("TRANOTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    int opTypeId;
                    if (int.TryParse(formattedValue, out opTypeId) && opTypeId > 0)
                    {
                        var opType = context.ImportDestuffSlipOperation.FirstOrDefault(o => o.OPRTYPE == opTypeId);
                        if (opType != null && !string.IsNullOrEmpty(opType.OPRTYPEDESC))
                            return opType.OPRTYPEDESC;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FormatValForLoggingDetail lookup failed for {fieldName}: {ex.Message}");
            }

            return formattedValue;
        }

        private string FormatValForLogging(string fieldName, object value)
        {
            var formattedValue = FormatVal(value);
            if (string.IsNullOrEmpty(formattedValue)) return formattedValue;

            try
            {
                if (fieldName.Equals("BANKMID", StringComparison.OrdinalIgnoreCase))
                {
                    int bankId;
                    if (int.TryParse(formattedValue, out bankId) && bankId > 0)
                    {
                        var bank = context.bankmasters.FirstOrDefault(b => b.BANKMID == bankId);
                        if (bank != null && !string.IsNullOrEmpty(bank.BANKMDESC))
                            return bank.BANKMDESC;
                    }
                }
                else if (fieldName.Equals("LCATEID", StringComparison.OrdinalIgnoreCase))
                {
                    int cateId;
                    if (int.TryParse(formattedValue, out cateId) && cateId > 0)
                    {
                        var cate = context.categorymasters.FirstOrDefault(c => c.CATEID == cateId);
                        if (cate != null && !string.IsNullOrEmpty(cate.CATENAME))
                            return cate.CATENAME;
                    }
                }
                else if (fieldName.Equals("TRANMODE", StringComparison.OrdinalIgnoreCase))
                {
                    int modeId;
                    if (int.TryParse(formattedValue, out modeId) && modeId > 0)
                    {
                        var mode = context.transactionmodemaster.FirstOrDefault(m => m.TRANMODE == modeId);
                        if (mode != null && !string.IsNullOrEmpty(mode.TRANMODEDETL))
                            return mode.TRANMODEDETL;
                    }
                }
                else if (fieldName.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                {
                    int tariffId;
                    if (int.TryParse(formattedValue, out tariffId) && tariffId > 0)
                    {
                        var tariff = context.tariffmasters.FirstOrDefault(t => t.TARIFFMID == tariffId);
                        if (tariff != null && !string.IsNullOrEmpty(tariff.TARIFFMDESC))
                            return tariff.TARIFFMDESC;
                    }
                }
                else if (fieldName.Equals("TARIFFGID", StringComparison.OrdinalIgnoreCase))
                {
                    int tariffGrpId;
                    if (int.TryParse(formattedValue, out tariffGrpId) && tariffGrpId > 0)
                    {
                        var tariffGrp = context.tariffgroupmasters.FirstOrDefault(tg => tg.TGID == tariffGrpId);
                        if (tariffGrp != null && !string.IsNullOrEmpty(tariffGrp.TGDESC))
                            return tariffGrp.TGDESC;
                    }
                }
                else if (fieldName.Equals("TRANOTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    int opTypeId;
                    if (int.TryParse(formattedValue, out opTypeId) && opTypeId > 0)
                    {
                        var opType = context.ImportDestuffSlipOperation.FirstOrDefault(o => o.OPRTYPE == opTypeId);
                        if (opType != null && !string.IsNullOrEmpty(opType.OPRTYPEDESC))
                            return opType.OPRTYPEDESC;
                    }
                }
                else if (fieldName.Equals("DISPSTATUS", StringComparison.OrdinalIgnoreCase))
                {
                    if (formattedValue == "1") return "CANCELLED";
                    if (formattedValue == "0") return "INBOOKS";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FormatValForLogging lookup failed for {fieldName}: {ex.Message}");
            }

            return formattedValue;
        }

        private static string FormatVal(object value)
        {
            if (value == null) return null;
            if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
            if (value is DateTime?)
            {
                var ndt = (DateTime?)value;
                return ndt.HasValue ? ndt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null;
            }
            if (value is decimal dec) return dec.ToString("0.####");
            var ndecs = value as decimal?;
            if (ndecs.HasValue) return ndecs.Value.ToString("0.####");
            return Convert.ToString(value);
        }

        private static bool BothNull(object a, object b) => a == null && b == null;

        private static decimal? ToNullableDecimal(object v)
        {
            if (v == null) return null;
            if (v is decimal d) return d;
            var nd = v as decimal?;
            if (nd.HasValue) return nd.Value;
            decimal parsed;
            return decimal.TryParse(Convert.ToString(v), out parsed) ? parsed : (decimal?)null;
        }

        private static Dictionary<string, string> GetV0BaselineValues(string connectionString, string gidno, string modules)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var sql = new SqlConnection(connectionString))
                {
                    sql.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT [FieldName], [NewValue]
                        FROM [dbo].[GateInDetailEditLog]
                        WHERE [GIDNO] = @GIDNO 
                        AND [Modules] = @Modules
                        AND (RTRIM(LTRIM([Version])) = @VLower 
                             OR RTRIM(LTRIM([Version])) = @VUpper 
                             OR RTRIM(LTRIM([Version])) = '0' 
                             OR RTRIM(LTRIM([Version])) = 'V0')", sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO", gidno);
                        cmd.Parameters.AddWithValue("@Modules", modules);
                        var baselineVerLower = "v0-" + gidno;
                        var baselineVerUpper = "V0-" + gidno;
                        cmd.Parameters.AddWithValue("@VLower", baselineVerLower);
                        cmd.Parameters.AddWithValue("@VUpper", baselineVerUpper);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var fieldName = reader["FieldName"]?.ToString();
                                var newValue = reader["NewValue"]?.ToString();
                                if (!string.IsNullOrEmpty(fieldName))
                                {
                                    result[fieldName] = newValue ?? "";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetV0BaselineValues failed: {ex.Message}");
            }
            return result;
        }

        private static void InsertEditLogRow(string connectionString, string gidno, string fieldName, string oldValue, string newValue, string changedBy, string versionLabel, string modules)
        {
            try
            {
                using (var sql = new SqlConnection(connectionString))
                {
                    sql.Open();
                    using (var cmd = new SqlCommand(@"INSERT INTO [dbo].[GateInDetailEditLog]
                        ([GIDNO], [FieldName], [OldValue], [NewValue], [ChangedBy], [ChangedOn], [Version], [Modules])
                        VALUES (@GIDNO, @FieldName, @OldValue, @NewValue, @ChangedBy, GETDATE(), @Version, @Modules)", sql))
                    {
                        cmd.Parameters.AddWithValue("@GIDNO", gidno);
                        cmd.Parameters.AddWithValue("@FieldName", fieldName);
                        cmd.Parameters.AddWithValue("@OldValue", (object)oldValue ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewValue", (object)newValue ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChangedBy", changedBy ?? "");
                        cmd.Parameters.AddWithValue("@Version", (object)versionLabel ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Modules", modules ?? string.Empty);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to insert edit log: {ex.Message}");
                throw;
            }
        }

        private void EnsureBaselineVersionZero(TransactionMaster snapshot, string userId)
        {
            try
            {
                var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

                var gidno = snapshot.TRANMID.ToString();
                if (string.IsNullOrWhiteSpace(gidno)) return;

                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[GateInDetailEditLog] WHERE [GIDNO]=@GIDNO AND [Modules]='ImportInvoice' AND (RTRIM(LTRIM([Version]))=@VLower OR RTRIM(LTRIM([Version]))=@VUpper OR RTRIM(LTRIM([Version]))='0' OR RTRIM(LTRIM([Version]))='V0')", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", gidno);
                    var baselineVerLower = "v0-" + gidno;
                    var baselineVerUpper = "V0-" + gidno;
                    cmd.Parameters.AddWithValue("@VLower", baselineVerLower);
                    cmd.Parameters.AddWithValue("@VUpper", baselineVerUpper);
                    sql.Open();
                    var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    if (exists) return;
                }

                // Load TransactionDetail records for baseline
                List<TransactionDetail> detailRecords = null;
                try
                {
                    detailRecords = context.transactiondetail.AsNoTracking().Where(x => x.TRANMID == snapshot.TRANMID).ToList();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load TransactionDetail for baseline: {ex.Message}");
                }

                InsertBaselineSnapshot(snapshot, userId, detailRecords);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureBaselineVersionZero failed: {ex.Message}");
            }
        }

        private void InsertBaselineSnapshot(TransactionMaster snapshot, string userId, List<TransactionDetail> detailRecords = null)
        {
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            var gidno = snapshot.TRANMID.ToString();
            if (string.IsNullOrWhiteSpace(gidno)) return;
            var baselineVer = "v0-" + gidno;
            
            // Track logged field changes to prevent duplicates within baseline creation
            // Key format: "FieldName|OldValue|NewValue" (normalized)
            var loggedChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Helper method to create a unique key for a field change
            string GetChangeKey(string fieldName, string oldValue, string newValue)
            {
                // Normalize NULL values to empty string for comparison
                var oldVal = string.IsNullOrEmpty(oldValue) ? "NULL" : oldValue.Trim();
                var newVal = string.IsNullOrEmpty(newValue) ? "NULL" : newValue.Trim();
                return $"{fieldName}|{oldVal}|{newVal}";
            }
            
            // Wrapper method that checks for duplicates before inserting
            void InsertEditLogRowWithDedup(string fieldName, string oldValue, string newValue)
            {
                var changeKey = GetChangeKey(fieldName, oldValue, newValue);
                if (loggedChanges.Contains(changeKey))
                {
                    System.Diagnostics.Debug.WriteLine($"Skipping duplicate baseline log entry: Field={fieldName}, OldValue={oldValue}, NewValue={newValue}");
                    return;
                }
                
                loggedChanges.Add(changeKey);
                InsertEditLogRow(cs.ConnectionString, gidno, fieldName, oldValue, newValue, userId, baselineVer, "ImportInvoice");
            }

            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANMID", "COMPYID", "SDPTID", "PRCSDATE", "LMUSRID", "CUSRID",
                "TRANTID", "TRANPCOUNT", "TRANCSNAME", "LEMID", "TRANAHAMT",
                "TRANHBLNO", "TRANPONO", "TRANIMPADDR1", "TRANIMPADDR2", "TRANIMPADDR3", "TRANIMPADDR4",
                "SLABNARN_HANDLDESC", "SLABNARN_ADNLDESC", "SLABNARN_STS",
                "TRANTALLYCHAID", "TRANTALLYCHANAME", "TCATEAID", "TCATEAGSTNO", "TSTATEID",
                "TALLYSTAT", "IRNNO", "ACKNO", "ACKDT", "QRCODEPATH", "CATEAID", "STATEID", "CATEAGSTNO",
                "TRANGSTNO", "TRANPAMT"
            };

            var props = typeof(TransactionMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                        q.Name.Equals(baseName + "NAME", StringComparison.OrdinalIgnoreCase) ||
                        (q.Name.EndsWith("NAME", StringComparison.OrdinalIgnoreCase) && q.Name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                    ));
                    if (nameProp != null) continue;
                }

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
                InsertEditLogRowWithDedup(p.Name, null, newVal);
            }

            // Log TransactionDetail records (including TARIFFMID)
            if (detailRecords != null && detailRecords.Count > 0)
            {
                foreach (var detail in detailRecords)
                {
                    InsertBaselineSnapshotDetail(detail, gidno, userId, baselineVer, cs.ConnectionString, loggedChanges);
                }
            }
        }

        private void InsertBaselineSnapshotDetail(TransactionDetail snapshot, string gidno, string userId, string baselineVer, string connectionString, HashSet<string> loggedChanges = null)
        {
            // Wrapper method that checks for duplicates before inserting
            void InsertEditLogRowWithDedup(string fieldName, string oldValue, string newValue)
            {
                if (loggedChanges != null)
                {
                    // Helper method to create a unique key for a field change
                    string GetChangeKey(string fn, string ov, string nv)
                    {
                        var oldVal = string.IsNullOrEmpty(ov) ? "NULL" : ov.Trim();
                        var newVal = string.IsNullOrEmpty(nv) ? "NULL" : nv.Trim();
                        return $"{fn}|{oldVal}|{newVal}";
                    }
                    
                    var changeKey = GetChangeKey(fieldName, oldValue, newValue);
                    if (loggedChanges.Contains(changeKey))
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping duplicate baseline log entry: Field={fieldName}, OldValue={oldValue}, NewValue={newValue}");
                        return;
                    }
                    
                    loggedChanges.Add(changeKey);
                }
                
                InsertEditLogRow(connectionString, gidno, fieldName, oldValue, newValue, userId, baselineVer, "ImportInvoice");
            }
            
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANDID", "TRANMID", "BILLEDID", "SLABTID", "TRANDREFID",
                "TRANIDATE", "TRANSDATE", "TRANEDATE",
                "TRANVHLFROM", "TRANVHLTO", "DISPSTATUS", "STFDID", "SBDID", "TRANDAID",
                "RCOL1", "RCOL2", "RCOL3", "RCOL4", "RCOL5", "RCOL6", "RCOL7",
                "RAMT1", "RAMT2", "RAMT3", "RAMT4", "RAMT5", "RAMT6", "RAMT7",
                "RCAMT1", "RCAMT2", "RCAMT3", "RCAMT4", "RCAMT5", "RCAMT6", "RCAMT7",
                "TRANDQTY", "TRANDRATE", "TRANDWGHT", "TRANDNOP",
                "TRANDEAMT", "TRANDFAMT", "TRANDPAMT", "TRANDAAMT", "TRANDNAMT",
                "TRANDHAMT", "TRAND_COVID_DISC_AMT", "TRANDADONAMT",
                "TRAND_STRG_CGST_AMT", "TRAND_STRG_SGST_AMT", "TRAND_STRG_IGST_AMT",
                "TRAND_HANDL_CGST_AMT", "TRAND_HANDL_SGST_AMT", "TRAND_HANDL_IGST_AMT",
                "TRAND_HANDL_CGST_EXPRN", "TRAND_HANDL_SGST_EXPRN", "TRAND_HANDL_IGST_EXPRN"
            };

            var props = typeof(TransactionDetail).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !p.PropertyType.IsValueType) continue;
                if (exclude.Contains(p.Name)) continue;

                var valObj = p.GetValue(snapshot, null);
                var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                // Always log TARIFFMID even if 0, to capture original state
                bool shouldLog = false;
                if (p.Name.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                {
                    shouldLog = true; // Always log TARIFFMID
                }
                else if (type == typeof(string))
                {
                    var s = (Convert.ToString(valObj) ?? string.Empty).Trim();
                    bool isDefault = string.IsNullOrEmpty(s) || s == "-" || s == "0" || s == "0.0" || s == "0.00" || s == "0.000" || s == "0.0000";
                    if (isDefault) continue;
                    shouldLog = true;
                }
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                {
                    var i = Convert.ToInt64(valObj ?? 0);
                    if (i == 0 && !p.Name.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase)) continue;
                    shouldLog = true;
                }
                else if (type == typeof(decimal))
                {
                    var d = ToNullableDecimal(valObj) ?? 0m;
                    if (d == 0m) continue;
                    shouldLog = true;
                }
                else if (type == typeof(double) || type == typeof(float))
                {
                    var d = Convert.ToDouble(valObj ?? 0.0);
                    if (Math.Abs(d) < 1e-9) continue;
                    shouldLog = true;
                }
                else
                {
                    shouldLog = true;
                }

                if (shouldLog)
                {
                    var newVal = FormatValForLoggingDetail(p.Name, valObj);
                    InsertEditLogRowWithDedup(p.Name, null, newVal ?? "");

                    // If TARIFFMID is logged, also log TARIFFGID
                    if (p.Name.Equals("TARIFFMID", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            int tariffId = 0;
                            if (valObj != null)
                            {
                                if (valObj is int) tariffId = (int)valObj;
                                else if (valObj is short) tariffId = (short)valObj;
                                else if (valObj is long) tariffId = (int)(long)valObj;
                                else int.TryParse(Convert.ToString(valObj), out tariffId);
                            }

                            string tariffGid = "";
                            if (tariffId > 0)
                            {
                                var tariff = context.tariffmasters.FirstOrDefault(t => t.TARIFFMID == tariffId);
                                if (tariff != null && tariff.TGID.HasValue)
                                {
                                    var tariffGrp = context.tariffgroupmasters.FirstOrDefault(tg => tg.TGID == tariff.TGID.Value);
                                    if (tariffGrp != null && !string.IsNullOrEmpty(tariffGrp.TGDESC))
                                        tariffGid = tariffGrp.TGDESC;
                                }
                            }

                            InsertEditLogRowWithDedup("TARIFFGID", null, tariffGid ?? "");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to log TARIFFGID in baseline: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}