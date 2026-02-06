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
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace scfs_erp.Controllers.Export
{
    [SessionExpire]
    public class ExportManualBillController : Controller
    {
        //
        // GET: /ExportManualBill/
        SCFSERPContext context = new SCFSERPContext();
        public static String constring = ConfigurationManager.ConnectionStrings["SCFSERP"].ConnectionString;
        SqlConnectionStringBuilder stringbuilder = new SqlConnectionStringBuilder(constring);
        [Authorize(Roles = "ExportManualBillIndex")]
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
                Session["REGSTRID"] = "49";
            }
            //...........Bill type......//
            List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
            if (Convert.ToInt32(Session["TRANBTYPE"]) == 1)
            {
                SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = true };
                selectedBILLYPE.Add(selectedItemGPTY);
                selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = false };
                selectedBILLYPE.Add(selectedItemGPTY);

            }
            else
            {
                SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = false };
                selectedBILLYPE.Add(selectedItemGPTY);
                selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = true };
                selectedBILLYPE.Add(selectedItemGPTY);

            }
            ViewBag.TRANBTYPE = selectedBILLYPE;
            ////..........end
            //............Billed to....//
            List<SelectListItem> selectedregid_ = new List<SelectListItem>();
            
            if (Convert.ToInt32(Session["REGSTRID"]) == 49)
            {
                SelectListItem selectedItemreg_ = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = true };
                selectedregid_.Add(selectedItemreg_);
                selectedItemreg_ = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = false };
                selectedregid_.Add(selectedItemreg_);
                ViewBag.REGSTRID = selectedregid_;
            }
            else if (Convert.ToInt32(Session["REGSTRID"]) == 50)
            {
                SelectListItem selectedItemreg_ = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = false };
                selectedregid_.Add(selectedItemreg_);
                selectedItemreg_ = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = true };
                selectedregid_.Add(selectedItemreg_);
                ViewBag.REGSTRID = selectedregid_;
            }
            
            //.....end


            return View();
        }
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            using (var e = new  CFSExportEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));

                var data = e.pr_Search_ExportManualBill(param.sSearch, Convert.ToInt32(Request["iSortCol_0"]), Request["sSortDir_0"], param.iDisplayStart, param.iDisplayStart + param.iDisplayLength,
                    totalRowsCount, filteredRowsCount, Convert.ToInt32(Session["compyid"]), Convert.ToInt32(Session["REGSTRID"]), 0, Convert.ToDateTime(Session["SDATE"]), Convert.ToDateTime(Session["EDATE"]));
                var aaData = data.Select(d => new string[] { d.TRANDATE.Value.ToString("dd/MM/yyyy"), d.TRANTIME.Value.ToString("hh:mm tt"), d.TRANDNO.ToString(), d.TRANREFNAME, d.TRANNAMT.ToString(), d.GSTAMT.ToString(), d.ACKNO, d.DISPSTATUS, d.TRANMID.ToString() }).ToArray();
                //var aaData = data.Select(d => new string[] { d.TRANDATE.Value.ToString("dd/MM/yyyy"), d.TRANTIME.Value.ToString("hh:mm tt"), d.TRANDNO.ToString(), d.TRANREFNAME, d.TRANNAMT.ToString(), d.DISPSTATUS, d.TRANMID.ToString(), d.GSTAMT.ToString(), d.ACKNO }).ToArray();
                return Json(new
                {
                    sEcho = param.sEcho,
                    aaData = aaData,
                    iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                    iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                }, JsonRequestBehavior.AllowGet);
            }
        }
        //[Authorize(Roles = "ExportManualBillEdit")]
        public void Edit(int id)
        {
            Response.Redirect("/ExportManualBill/GSTForm/" + id);
        }

        //[Authorize(Roles = "ExportManualBillEdit")]
        public void MEdit(int id)
        {
            Response.Redirect("/ExportManualBill/MGSTForm/" + id);
        }

        [Authorize(Roles = "ExportManualBillCreate")]
        public ActionResult Form(int? id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            TransactionMaster tab = new TransactionMaster();
            TransactionMD vm = new TransactionMD();

            //..........................................Dropdown data.........................//

            ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.BILLEDID = new SelectList(context.containersizemasters.Where(x => x.DISPSTATUS == 0 && x.CONTNRSID > 1), "CONTNRSID", "CONTNRSDESC");
            ViewBag.TRANDREFID = new SelectList(context.chargemasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CHRGDESC), "CHRGID", "CHRGDESC");
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");

            //...........Bill type......//
            List<SelectListItem> selectedBILLTYPE = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "LI", Value = "1", Selected = true };
            selectedBILLTYPE.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "DS", Value = "2", Selected = false };
            selectedBILLTYPE.Add(selectedItemDSP);
            ViewBag.TRANBTYPE = selectedBILLTYPE;
            //....end

            //............Billed to....//
            List<SelectListItem> selectedregid = new List<SelectListItem>();
            //SelectListItem selectedItemreg = new SelectListItem { Text = "EMPTY", Value = "21", Selected = false };
            //selectedregid.Add(selectedItemreg);
            //selectedItemreg = new SelectListItem { Text = "LOAD", Value = "22", Selected = false };
            //selectedregid.Add(selectedItemreg);
            //selectedItemreg = new SelectListItem { Text = "WGMT", Value = "23", Selected = true };
            //selectedregid.Add(selectedItemreg);
            //ViewBag.REGSTRID = selectedregid;
            SelectListItem selectedItemreg = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = true };
            selectedregid.Add(selectedItemreg);
            selectedItemreg = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = false };
            selectedregid.Add(selectedItemreg);
            ViewBag.REGSTRID = selectedregid;
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

                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", tab.BANKMID);
                ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", tab.TRANMODE);


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


                ////....................Bill type.................//
                List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANBTYPE) == 1)
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = false };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                else
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = false };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                ViewBag.TRANBTYPE = selectedBILLYPE;
                ////..........end
                //............Billed to....//
                List<SelectListItem> selectedregid_ = new List<SelectListItem>();
                //if (Convert.ToInt32(tab.REGSTRID) == 22)
                //{
                //    SelectListItem selectedItemreg_ = new SelectListItem { Text = "EMPTY", Value = "21", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //    selectedItemreg_ = new SelectListItem { Text = "LOAD", Value = "22", Selected = true };
                //    selectedregid_.Add(selectedItemreg_);
                //    ViewBag.REGSTRID = selectedregid_;
                //    selectedItemreg_ = new SelectListItem { Text = "WGMT", Value = "23", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //}
                //else if (Convert.ToInt32(tab.REGSTRID) == 21)
                //{
                //    SelectListItem selectedItemreg_ = new SelectListItem { Text = "EMPTY", Value = "21", Selected = true };
                //    selectedregid_.Add(selectedItemreg_);
                //    selectedItemreg_ = new SelectListItem { Text = "LOAD", Value = "22", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //    ViewBag.REGSTRID = selectedregid_;
                //    selectedItemreg_ = new SelectListItem { Text = "WGMT", Value = "23", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //}
                if (Convert.ToInt32(tab.REGSTRID) == 49)
                {
                    SelectListItem selectedItemreg_ = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = true };
                    selectedregid_.Add(selectedItemreg_);
                    selectedItemreg_ = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = false };
                    selectedregid_.Add(selectedItemreg_);
                    ViewBag.REGSTRID = selectedregid_;
                }
                else if (Convert.ToInt32(tab.REGSTRID) == 50)
                {
                    SelectListItem selectedItemreg_ = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = false };
                    selectedregid_.Add(selectedItemreg_);
                    selectedItemreg_ = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = true };
                    selectedregid_.Add(selectedItemreg_);
                    ViewBag.REGSTRID = selectedregid_;
                }

                //.....end

                vm.masterdata = context.transactionmaster.Where(det => det.TRANMID == id).ToList();
                vm.detaildata = context.transactiondetail.Where(det => det.TRANMID == id).ToList();
                vm.costfactor = context.transactionmasterfactor.Where(det => det.TRANMID == id).ToList();

            }
            return View(vm);
        }


        //gst form
        [Authorize(Roles = "ExportManualBillCreate")]
        public ActionResult GSTForm(int? id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            TransactionMaster tab = new TransactionMaster();
            TransactionMD vm = new TransactionMD();

            //..........................................Dropdown data.........................//
            ViewBag.ACHEADID = new SelectList(context.accountheadmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.ACHEADDESC), "ACHEADID", "ACHEADDESC");
            ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.BILLEDID = new SelectList(context.containersizemasters.Where(x => x.DISPSTATUS == 0 && x.CONTNRSID > 1), "CONTNRSID", "CONTNRSDESC");
            ViewBag.TRANDREFID = new SelectList(context.chargemasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CHRGDESC), "CHRGID", "CHRGDESC");
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");
            ViewBag.CATEAID = new SelectList("");
            //...........Bill type......//
            List<SelectListItem> selectedBILLTYPE = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "LI", Value = "1", Selected = true };
            selectedBILLTYPE.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "DS", Value = "2", Selected = false };
            selectedBILLTYPE.Add(selectedItemDSP);
            ViewBag.TRANBTYPE = selectedBILLTYPE;
            //....end

            //............Billed to....//
            List<SelectListItem> selectedregid = new List<SelectListItem>();
            //SelectListItem selectedItemreg = new SelectListItem { Text = "EMPTY", Value = "21", Selected = false };
            //selectedregid.Add(selectedItemreg);
            //selectedItemreg = new SelectListItem { Text = "LOAD", Value = "22", Selected = false };
            //selectedregid.Add(selectedItemreg);
            //selectedItemreg = new SelectListItem { Text = "WGMT", Value = "23", Selected = true };
            //selectedregid.Add(selectedItemreg);

            SelectListItem selectedItemreg = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = false };
            selectedregid.Add(selectedItemreg);
            selectedItemreg = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = false };
            selectedregid.Add(selectedItemreg);
            ViewBag.REGSTRID = selectedregid;


            //.....end

            //........display status.........//
            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDISP = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDISP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;
            //....end

            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst = new List<SelectListItem>();
            SelectListItem selectedItemtax = new SelectListItem { Text = "Yes", Value = "0", Selected = true };
            selectedtaxlst.Add(selectedItemtax);
            selectedItemtax = new SelectListItem { Text = "No", Value = "1", Selected = false };
            selectedtaxlst.Add(selectedItemtax);
            ViewBag.STAX = selectedtaxlst;

            if (id != 0)//....Edit Mode
            {
                tab = context.transactionmaster.Find(id);//find selected record

                //...................................Selected dropdown value..................................//

                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", tab.BANKMID);
                ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", tab.TRANMODE);

                var sqy = context.Database.SqlQuery<Category_Address_Details>("Select *from CATEGORY_ADDRESS_DETAIL WHERE CATEAID=" + tab.CATEAID).ToList();
                if (sqy.Count > 0)
                {
                    ViewBag.CATEAID = new SelectList(context.categoryaddressdetails.Where(x => x.CATEAID > 0), "CATEAID", "CATEATYPEDESC", tab.CATEAID);
                }
                else { ViewBag.CATEAID = new SelectList(""); }
                
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

                List<SelectListItem> selectedregide_ = new List<SelectListItem>();
                if (Convert.ToInt32(tab.REGSTRID) == 49)
                {
                    SelectListItem selectedItemrege1 = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = true };
                    selectedregide_.Add(selectedItemrege1);
                    ViewBag.REGSTRID = selectedregide_;
                }
                else
                {
                    SelectListItem selectedItemrege1 = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = true };
                    selectedregide_.Add(selectedItemrege1);
                    ViewBag.REGSTRID = selectedregide_;
                }
                    
                //.................Stax.................//
                List<SelectListItem> selectedDISP1 = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANTID) == 0)
                {
                    SelectListItem selectedItemDIS1 = new SelectListItem { Text = "No", Value = "1", Selected = false };
                    selectedDISP1.Add(selectedItemDIS1);
                    selectedItemDIS1 = new SelectListItem { Text = "Yes", Value = "0", Selected = true };
                    selectedDISP1.Add(selectedItemDIS1);
                    ViewBag.STAX = selectedDISP1;
                }
                else
                {
                    SelectListItem selectedItemDIS2 = new SelectListItem { Text = "No", Value = "1", Selected = true };
                    selectedDISP1.Add(selectedItemDIS2);
                    selectedItemDIS2 = new SelectListItem { Text = "Yes", Value = "0", Selected = false };
                    selectedDISP1.Add(selectedItemDIS2);
                    ViewBag.STAX = selectedDISP1;
                }//....end



                ////....................Bill type.................//
                List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANBTYPE) == 1)
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = false };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                else
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = false };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                ViewBag.TRANBTYPE = selectedBILLYPE;
                ////..........end
                //............Billed to....//
                List<SelectListItem> selectedregid_ = new List<SelectListItem>();
                //if (Convert.ToInt32(tab.REGSTRID) == 22)
                //{
                //    SelectListItem selectedItemreg_ = new SelectListItem { Text = "EMPTY", Value = "21", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //    selectedItemreg_ = new SelectListItem { Text = "LOAD", Value = "22", Selected = true };
                //    selectedregid_.Add(selectedItemreg_);
                //    ViewBag.REGSTRID = selectedregid_;
                //    selectedItemreg_ = new SelectListItem { Text = "WGMT", Value = "23", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //}
                //else if (Convert.ToInt32(tab.REGSTRID) == 21)
                //{
                //    SelectListItem selectedItemreg_ = new SelectListItem { Text = "EMPTY", Value = "21", Selected = true };
                //    selectedregid_.Add(selectedItemreg_);
                //    selectedItemreg_ = new SelectListItem { Text = "LOAD", Value = "22", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //    ViewBag.REGSTRID = selectedregid_;
                //    selectedItemreg_ = new SelectListItem { Text = "WGMT", Value = "23", Selected = false };
                //    selectedregid_.Add(selectedItemreg_);
                //}
                
                //.....end

                vm.masterdata = context.transactionmaster.Where(det => det.TRANMID == id).ToList();
                vm.detaildata = context.transactiondetail.Where(det => det.TRANMID == id).ToList();
                vm.costfactor = context.transactionmasterfactor.Where(det => det.TRANMID == id).ToList();
                vm.manualviewdata = context.Database.SqlQuery<pr_Export_Manual_Invoice_Ctrl_Flx_Assgn_Result>("pr_Export_Manual_Invoice_Ctrl_Flx_Assgn @PTRANMID=" + id + "").ToList();//........procedure  for edit mode details data

            }
            return View(vm);
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
                        TransactionMaster transactionmaster = new TransactionMaster();
                        TransactionDetail transactiondetail = new TransactionDetail();
                        //-------Getting Primarykey field--------
                        Int32 TRANMID = Convert.ToInt32(F_Form["masterdata[0].TRANMID"]);
                        Int32 TRANDID = 0;
                        string DELIDS = "";
                        //-----End

                        if (TRANMID != 0)
                        {
                            transactionmaster = context.transactionmaster.Find(TRANMID);
                        }

                        //...........transaction master.............//
                        transactionmaster.TRANMID = TRANMID;
                        transactionmaster.COMPYID = Convert.ToInt32(Session["compyid"]);
                        transactionmaster.SDPTID = 2;
                        transactionmaster.TRANTID = Convert.ToInt16(F_Form["STAX"]); //2;
                        transactionmaster.TRANLMID = 0;
                        transactionmaster.TRANLSID = 0;
                        transactionmaster.TRANLSNO = null;
                        transactionmaster.TRANLMNO = "";
                        transactionmaster.TRANLMDATE = DateTime.Now;
                        transactionmaster.TRANLSDATE = DateTime.Now;
                        transactionmaster.TRANNARTN = null;
                        var cusr = transactionmaster.CUSRID;
                        if (TRANMID.ToString() == "0" || cusr == null || cusr == "")
                            transactionmaster.CUSRID = Session["CUSRID"].ToString();
                        transactionmaster.LMUSRID = Session["CUSRID"].ToString();
                        transactionmaster.DISPSTATUS = Convert.ToInt16(F_Form["DISPSTATUS"]);
                        transactionmaster.PRCSDATE = DateTime.Now;
                        transactionmaster.TRANDATE = Convert.ToDateTime(F_Form["masterdata[0].TRANTIME"]).Date;
                        transactionmaster.TRANTIME = Convert.ToDateTime(F_Form["masterdata[0].TRANTIME"]);
                        transactionmaster.TRANREFID = Convert.ToInt32(F_Form["masterdata[0].TRANREFID"]);
                        transactionmaster.TRANREFNAME = F_Form["masterdata[0].TRANREFNAME"].ToString();
                        transactionmaster.LCATEID = 0;
                        transactionmaster.TRANBTYPE = Convert.ToInt16(F_Form["TRANBTYPE"]);
                        transactionmaster.REGSTRID = Convert.ToInt16(F_Form["REGSTRID"]);
                        transactionmaster.TRANMODE = Convert.ToInt16(F_Form["TRANMODE"]);
                        transactionmaster.TRANMODEDETL = (F_Form["masterdata[0].TRANMODEDETL"]);
                        transactionmaster.TRANGAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANGAMT"]);
                        transactionmaster.TRANNAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANNAMT"]);
                        transactionmaster.TRANROAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANROAMT"]);
                        transactionmaster.TRANREFAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANREFAMT"]);
                        transactionmaster.TRANRMKS = (F_Form["masterdata[0].TRANRMKS"]).ToString();
                        transactionmaster.TRANAMTWRDS = AmtInWrd.ConvertNumbertoWords(F_Form["masterdata[0].TRANNAMT"]);

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

                        string CATEAID = Convert.ToString(F_Form["CATEAID"]);
                        if (CATEAID == "" || CATEAID == null || CATEAID == "0")
                        { transactionmaster.CATEAID = 0; }
                        else { transactionmaster.CATEAID = Convert.ToInt32(CATEAID); }

                        string STATEID = Convert.ToString(F_Form["masterdata[0].STATEID"]);
                        if (STATEID == "" || STATEID == null || STATEID == "0")
                        { transactionmaster.STATEID = 0; }
                        else { transactionmaster.STATEID = Convert.ToInt32(STATEID); }

                        transactionmaster.CATEAGSTNO = Convert.ToString(F_Form["masterdata[0].CATEAGSTNO"]);
                        transactionmaster.TRANIMPADDR1 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR1"]);
                        transactionmaster.TRANIMPADDR2 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR2"]);
                        transactionmaster.TRANIMPADDR3 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR3"]);
                        transactionmaster.TRANIMPADDR4 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR4"]);

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
                        string bill = "";
                        if (Convert.ToInt32(F_Form["TRANBTYPE"]) == 1) bill = "LI";
                        else bill = "DS";
                        if (TRANMID == 0)
                        {
                            //transactionmaster.TRANNO = Convert.ToInt16(Autonumber.autonum("transactionmaster", "TRANNO", "TRANNO<>0 and TRANBTYPE=" + Convert.ToInt16(F_Form["TRANBTYPE"]) + " and REGSTRID=" + Convert.ToInt16(F_Form["REGSTRID"]) + " and SDPTID=2 and compyid=" + Convert.ToInt32(Session["compyid"]) + "").ToString());
                            if (regsid == 49)
                                transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.gstexportautonum("transactionmaster", "TRANNO", F_Form["REGSTRID"].ToString(), Session["compyid"].ToString(), F_Form["TRANBTYPE"].ToString(), Convert.ToInt32(transactionmaster.TRANREFID)).ToString());
                            else
                                transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.gstexportautonum_BS("transactionmaster", "TRANNO", F_Form["REGSTRID"].ToString(), Session["compyid"].ToString(), F_Form["TRANBTYPE"].ToString(), Convert.ToInt32(transactionmaster.TRANREFID)).ToString());
                            int ano = transactionmaster.TRANNO;
                            //string format = "SUD/EXP/";
                            string format = "EXP/" ;
                            string btyp = auto_numbr_invoice.GetCateBillType(Convert.ToInt32(transactionmaster.TRANREFID)).ToString();
                            if (btyp == "")
                            {
                                format = format + Session["GPrxDesc"] + "/";
                            }
                            else
                                format = format.Replace("/", "") + Session["GPrxDesc"] + btyp;
                            string billformat = "";
                            switch (regsid)
                            {
                                //case 21: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                                //case 22: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                                //case 23: billformat = "STL/MNUL/EXP/"; break;

                                case 49: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                                case 50: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                                //case 23: billformat = "STL/MNUL/EXP/"; break;
                            }
                            string prfx = string.Format(format + "{0:D5}", ano);
                            string billprfx = string.Format(billformat + "{0:D5}", ano);
                            transactionmaster.TRANDNO = prfx.ToString();
                            transactionmaster.TRANBILLREFNO = billprfx.ToString();

                            //........end of autonumber
                            context.transactionmaster.Add(transactionmaster);
                            context.SaveChanges();
                            TRANMID = transactionmaster.TRANMID;
                        }
                        else
                        {
                            //int ano = transactionmaster.TRANNO;
                            ////string format = "SUD/EXP/";
                            //string format = "EXP/";
                            //string btyp = auto_numbr_invoice.GetCateBillType(Convert.ToInt32(transactionmaster.TRANREFID)).ToString();
                            //if (btyp == "")
                            //{
                            //    format = format + Session["GPrxDesc"] + "/";
                            //}
                            //else
                            //    format = format.Replace("/", "") + Session["GPrxDesc"] + btyp;
                            //string billformat = "";
                            //switch (regsid)
                            //{
                            //    //case 21: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                            //    //case 22: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                            //    //case 23: billformat = "STL/MNUL/EXP/"; break;

                            //    case 49: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                            //    case 50: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                            //   // case 23: billformat = "STL/MNUL/EXP/"; break;
                            //}
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
                        string[] TRANIDATE = F_Form.GetValues("TRANIDATE");
                        string[] TRANSDATE = F_Form.GetValues("TRANSDATE");
                        string[] TRANEDATE = F_Form.GetValues("TRANEDATE");
                        string[] TRANDNAMT = F_Form.GetValues("TRANDNAMT");
                        string[] TRANDGAMT = F_Form.GetValues("TRANDGAMT");
                        string[] ACHEADID;
                        string[] TRANDREFID;
                        string[] TRANDDESC;
                        string[] TRANDREFNAME;
                        if (F_Form.GetValues("detaildata[0].ACHEADID") == null)
                        {
                            ACHEADID = F_Form.GetValues("ACHEADID");
                        }
                        else 
                        {
                            ACHEADID = F_Form.GetValues("detaildata[0].ACHEADID");
                        }
                        if (F_Form.GetValues("detaildata[0].TRANDREFID") == null)
                        {
                           TRANDREFID = F_Form.GetValues("TRANDREFID");
                        }
                        else
                        {
                            TRANDREFID = F_Form.GetValues("detaildata[0].TRANDREFID");
                        }
                        if (F_Form.GetValues("detaildata[0].TRANDDESC") == null)
                        {
                            TRANDDESC = F_Form.GetValues("TRANDDESC");
                        }
                        else
                        {
                            TRANDDESC = F_Form.GetValues("detaildata[0].TRANDDESC");
                        }
                        if (F_Form.GetValues("detaildata[0].TRANDREFNAME") == null)
                        {
                            TRANDREFNAME = F_Form.GetValues("TRANDREFNAME");
                        }
                        else
                        {
                            TRANDREFNAME = F_Form.GetValues("detaildata[0].TRANDREFNAME");
                        }
                        string[] BILLEDID = F_Form.GetValues("detaildata[0].BILLEDID");
                        string[] TRANVHLFROM = F_Form.GetValues("TRANVHLFROM");
                        string[] TRANVHLTO = F_Form.GetValues("TRANVHLTO");
                        for (int count = 0; count < F_TRANDID.Count(); count++)
                        {
                            TRANDID = Convert.ToInt32(F_TRANDID[count]);

                            if (TRANDID != 0)
                            {
                                transactiondetail = context.transactiondetail.Find(TRANDID);
                            }
                            transactiondetail.TRANMID = transactionmaster.TRANMID;
                            transactiondetail.TRANDREFNO = (TRANDREFNO[count]).ToString();
                            transactiondetail.TRANDREFNAME = (TRANDREFNAME[count]).ToString();
                            transactiondetail.TRANDREFID = 0; // Convert.ToInt32(TRANDREFID[count]);
                            transactiondetail.TRANDDESC = (TRANDDESC[count]).ToString();
                            transactiondetail.ACHEADID = Convert.ToInt32(ACHEADID[count]);
                            //transactiondetail.TRANIDATE = Convert.ToDateTime(TRANIDATE[count]);
                            transactiondetail.TRANIDATE = DateTime.Now;
                            transactiondetail.TRANSDATE = DateTime.Now;
                            transactiondetail.TRANEDATE = DateTime.Now;
                            transactiondetail.TRANVDATE = DateTime.Now;
                            transactiondetail.TRANVHLFROM = ""; //(TRANVHLFROM[count]).ToString();
                            transactiondetail.TRANVHLTO = "";// (TRANVHLTO[count]).ToString();
                            transactiondetail.TRANDGAMT = Convert.ToDecimal(TRANDGAMT[count]);
                            transactiondetail.TRANDNAMT = Convert.ToDecimal(TRANDGAMT[count]);
                            transactiondetail.BILLEDID = 0;//Convert.ToInt32(BILLEDID[count]);
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
                        trans.Commit(); Response.Redirect("Index");
                    }
                    catch (Exception EX)
                    {
                        var msg = EX.InnerException;
                        trans.Rollback();
                        Response.Write("Sorry!!An Error Ocurred...");
                    }
                }
            }

        }

        [Authorize(Roles = "ExportManualBillCreate")]
        public ActionResult MGSTForm(int? id = 0)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            TransactionMaster tab = new TransactionMaster();
            TransactionMD vm = new TransactionMD();

            //..........................................Dropdown data.........................//
            ViewBag.ACHEADID = new SelectList(context.accountheadmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.ACHEADDESC), "ACHEADID", "ACHEADDESC");
            ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL");
            ViewBag.BILLEDID = new SelectList(context.containersizemasters.Where(x => x.DISPSTATUS == 0 && x.CONTNRSID > 1), "CONTNRSID", "CONTNRSDESC");
            ViewBag.TRANDREFID = new SelectList(context.chargemasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.CHRGDESC), "CHRGID", "CHRGDESC");
            ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC");
            ViewBag.CATEAID = new SelectList("");
            //...........Bill type......//
            List<SelectListItem> selectedBILLTYPE = new List<SelectListItem>();
            SelectListItem selectedItemDSP = new SelectListItem { Text = "LI", Value = "1", Selected = true };
            selectedBILLTYPE.Add(selectedItemDSP);
            selectedItemDSP = new SelectListItem { Text = "DS", Value = "2", Selected = false };
            selectedBILLTYPE.Add(selectedItemDSP);
            ViewBag.TRANBTYPE = selectedBILLTYPE;
            //....end

            //............Billed to....//
            List<SelectListItem> selectedregid = new List<SelectListItem>();        

            SelectListItem selectedItemreg = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = false };
            selectedregid.Add(selectedItemreg);
            selectedItemreg = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = false };
            selectedregid.Add(selectedItemreg);
            ViewBag.REGSTRID = selectedregid;

            //.....end

            //........display status.........//
            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItemDISP = new SelectListItem { Text = "In Books", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItemDISP);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;
            //....end

            //.........s.Tax...//
            List<SelectListItem> selectedtaxlst = new List<SelectListItem>();
            SelectListItem selectedItemtax = new SelectListItem { Text = "Yes", Value = "0", Selected = true };
            selectedtaxlst.Add(selectedItemtax);
            selectedItemtax = new SelectListItem { Text = "No", Value = "1", Selected = false };
            selectedtaxlst.Add(selectedItemtax);
            ViewBag.STAX = selectedtaxlst;

            if (id != 0)//....Edit Mode
            {
                tab = context.transactionmaster.Find(id);//find selected record

                //...................................Selected dropdown value..................................//

                ViewBag.BANKMID = new SelectList(context.bankmasters.Where(x => x.DISPSTATUS == 0).OrderBy(x => x.BANKMDESC), "BANKMID", "BANKMDESC", tab.BANKMID);
                ViewBag.TRANMODE = new SelectList(context.transactionmodemaster.Where(x => x.TRANMODE > 0), "TRANMODE", "TRANMODEDETL", tab.TRANMODE);

                var sqy = context.Database.SqlQuery<Category_Address_Details>("Select *from CATEGORY_ADDRESS_DETAIL WHERE CATEAID=" + tab.CATEAID).ToList();
               
                if (sqy.Count > 0)
                {
                    
                    ViewBag.CATEAID = new SelectList(context.categoryaddressdetails.Where(x => x.CATEID == tab.TRANREFID), "CATEAID", "CATEATYPEDESC", tab.CATEAID);
                }
                else { ViewBag.CATEAID = new SelectList(""); }

                var starqy = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER where STATEID = " + tab.STATEID).ToList();
                if (starqy.Count > 0)
                {
                    ViewBag.STATEDESC = starqy[0].STATECODE + "  " + starqy[0].STATEDESC;
                    ViewBag.STATETYPE = starqy[0].STATETYPE;
                }


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

                List<SelectListItem> selectedregide_ = new List<SelectListItem>();
                if (Convert.ToInt32(tab.REGSTRID) == 49)
                {
                    SelectListItem selectedItemrege1 = new SelectListItem { Text = "TAX INVOICE", Value = "49", Selected = true };
                    selectedregide_.Add(selectedItemrege1);
                    ViewBag.REGSTRID = selectedregide_;
                }
                else
                {
                    SelectListItem selectedItemrege1 = new SelectListItem { Text = "BILL OF SUPPLY", Value = "50", Selected = true };
                    selectedregide_.Add(selectedItemrege1);
                    ViewBag.REGSTRID = selectedregide_;
                }

                //.................Stax.................//
                List<SelectListItem> selectedDISP1 = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANTID) == 0)
                {
                    SelectListItem selectedItemDIS1 = new SelectListItem { Text = "No", Value = "1", Selected = false };
                    selectedDISP1.Add(selectedItemDIS1);
                    selectedItemDIS1 = new SelectListItem { Text = "Yes", Value = "0", Selected = true };
                    selectedDISP1.Add(selectedItemDIS1);
                    ViewBag.STAX = selectedDISP1;
                }
                else
                {
                    SelectListItem selectedItemDIS2 = new SelectListItem { Text = "No", Value = "1", Selected = true };
                    selectedDISP1.Add(selectedItemDIS2);
                    selectedItemDIS2 = new SelectListItem { Text = "Yes", Value = "0", Selected = false };
                    selectedDISP1.Add(selectedItemDIS2);
                    ViewBag.STAX = selectedDISP1;
                }//....end



                ////....................Bill type.................//
                List<SelectListItem> selectedBILLYPE = new List<SelectListItem>();
                if (Convert.ToInt32(tab.TRANBTYPE) == 1)
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = false };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                else
                {
                    SelectListItem selectedItemGPTY = new SelectListItem { Text = "LI", Value = "1", Selected = false };
                    selectedBILLYPE.Add(selectedItemGPTY);
                    selectedItemGPTY = new SelectListItem { Text = "DS", Value = "2", Selected = true };
                    selectedBILLYPE.Add(selectedItemGPTY);

                }
                ViewBag.TRANBTYPE = selectedBILLYPE;
               
                List<SelectListItem> selectedregid_ = new List<SelectListItem>();
              

                vm.masterdata = context.transactionmaster.Where(det => det.TRANMID == id).ToList();
                vm.detaildata = context.transactiondetail.Where(det => det.TRANMID == id).ToList();
                vm.costfactor = context.transactionmasterfactor.Where(det => det.TRANMID == id).ToList();
                vm.manualviewdata = context.Database.SqlQuery<pr_Export_Manual_Invoice_Ctrl_Flx_Assgn_Result>("pr_Export_Manual_Invoice_Ctrl_Flx_Assgn @PTRANMID=" + id + "").ToList();//........procedure  for edit mode details data

            }
            return View(vm);
        }

        public void msavedata(FormCollection F_Form)
        {
            using (SCFSERPContext dataContext = new SCFSERPContext())
            {
                using (var trans = dataContext.Database.BeginTransaction())
                {
                    try
                    {
                        TransactionMaster transactionmaster = new TransactionMaster();
                        TransactionDetail transactiondetail = new TransactionDetail();
                        //-------Getting Primarykey field--------
                        Int32 TRANMID = Convert.ToInt32(F_Form["masterdata[0].TRANMID"]);
                        Int32 TRANDID = 0;
                        string DELIDS = "";
                        //-----End

                        // Capture BEFORE state for edit logging
                        TransactionMaster original = null;
                        if (TRANMID != 0)
                        {
                            original = context.transactionmaster.AsNoTracking().FirstOrDefault(x => x.TRANMID == TRANMID);
                        }

                        if (TRANMID != 0)
                        {
                            transactionmaster = context.transactionmaster.Find(TRANMID);
                        }

                        //...........transaction master.............//
                        transactionmaster.TRANMID = TRANMID;
                        transactionmaster.COMPYID = Convert.ToInt32(Session["compyid"]);
                        transactionmaster.SDPTID = 2;
                        transactionmaster.TRANTID = Convert.ToInt16(F_Form["STAX"]); //2;
                        transactionmaster.TRANLMID = 0;
                        transactionmaster.TRANLSID = 0;
                        transactionmaster.TRANLSNO = null;
                        transactionmaster.TRANLMNO = "";
                        transactionmaster.TRANLMDATE = DateTime.Now;
                        transactionmaster.TRANLSDATE = DateTime.Now;
                        transactionmaster.TRANNARTN = null;
                        var cusr = transactionmaster.CUSRID;
                        if (TRANMID.ToString() == "0" || cusr == null || cusr == "")
                            transactionmaster.CUSRID = Session["CUSRID"].ToString();
                        transactionmaster.LMUSRID = Session["CUSRID"].ToString();
                        transactionmaster.DISPSTATUS = Convert.ToInt16(F_Form["DISPSTATUS"]);
                        transactionmaster.PRCSDATE = DateTime.Now;
                        transactionmaster.TRANDATE = Convert.ToDateTime(F_Form["masterdata[0].TRANTIME"]).Date;
                        transactionmaster.TRANTIME = Convert.ToDateTime(F_Form["masterdata[0].TRANTIME"]);
                        transactionmaster.TRANREFID = Convert.ToInt32(F_Form["masterdata[0].TRANREFID"]);
                        transactionmaster.TRANREFNAME = F_Form["masterdata[0].TRANREFNAME"].ToString();
                        transactionmaster.LCATEID = 0;
                        transactionmaster.TRANBTYPE = Convert.ToInt16(F_Form["TRANBTYPE"]);
                        transactionmaster.REGSTRID = Convert.ToInt16(F_Form["REGSTRID"]);
                        transactionmaster.TRANMODE = Convert.ToInt16(F_Form["TRANMODE"]);
                        transactionmaster.TRANMODEDETL = (F_Form["masterdata[0].TRANMODEDETL"]);
                        transactionmaster.TRANGAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANGAMT"]);
                        transactionmaster.TRANNAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANNAMT"]);
                        transactionmaster.TRANROAMT = 0; // Convert.ToDecimal(F_Form["masterdata[0].TRANROAMT"]);
                        transactionmaster.TRANREFAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANREFAMT"]);
                        transactionmaster.TRANRMKS = (F_Form["masterdata[0].TRANRMKS"]).ToString();
                        transactionmaster.TRANAMTWRDS = AmtInWrd.ConvertNumbertoWords(F_Form["masterdata[0].TRANNAMT"]);

                        transactionmaster.STRG_HSNCODE = ""; //F_Form["STRG_HSN_CODE"].ToString();
                        transactionmaster.HANDL_HSNCODE = "";// F_Form["HANDL_HSN_CODE"].ToString();

                        transactionmaster.STRG_TAXABLE_AMT = 0; //Convert.ToDecimal(F_Form["STRG_TAXABLE_AMT"]);
                        transactionmaster.HANDL_TAXABLE_AMT = 0; //Convert.ToDecimal(F_Form["HANDL_TAXABLE_AMT"]);

                        transactionmaster.STRG_CGST_EXPRN = 0; // Convert.ToDecimal(F_Form["STRG_CGST_EXPRN"]);
                        transactionmaster.STRG_SGST_EXPRN = 0; // Convert.ToDecimal(F_Form["STRG_SGST_EXPRN"]);
                        transactionmaster.STRG_IGST_EXPRN = 0; //Convert.ToDecimal(F_Form["STRG_IGST_EXPRN"]);
                        transactionmaster.STRG_CGST_AMT = 0; //Convert.ToDecimal(F_Form["STRG_CGST_AMT"]);
                        transactionmaster.STRG_SGST_AMT = 0; //Convert.ToDecimal(F_Form["STRG_SGST_AMT"]);
                        transactionmaster.STRG_IGST_AMT = 0; //Convert.ToDecimal(F_Form["STRG_IGST_AMT"]);

                        transactionmaster.HANDL_CGST_EXPRN = 0; //Convert.ToDecimal(F_Form["HANDL_CGST_EXPRN"]);
                        transactionmaster.HANDL_SGST_EXPRN = 0; //Convert.ToDecimal(F_Form["HANDL_SGST_EXPRN"]);
                        transactionmaster.HANDL_IGST_EXPRN = 0; //Convert.ToDecimal(F_Form["HANDL_IGST_EXPRN"]);
                        transactionmaster.HANDL_CGST_AMT = 0; //Convert.ToDecimal(F_Form["HANDL_CGST_AMT"]);
                        transactionmaster.HANDL_SGST_AMT = 0; //Convert.ToDecimal(F_Form["HANDL_SGST_AMT"]);
                        transactionmaster.HANDL_IGST_AMT = 0; //Convert.ToDecimal(F_Form["HANDL_IGST_AMT"]);

                        transactionmaster.TRANCGSTAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANCGSTAMT"]);
                        transactionmaster.TRANSGSTAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANSGSTAMT"]);
                        transactionmaster.TRANIGSTAMT = Convert.ToDecimal(F_Form["masterdata[0].TRANIGSTAMT"]);

                       

                        string CATEAID = Convert.ToString(F_Form["CATEAID"]);
                        if (CATEAID == "" || CATEAID == null || CATEAID == "0")
                        { transactionmaster.CATEAID = 0; }
                        else { transactionmaster.CATEAID = Convert.ToInt32(CATEAID); }

                        string STATEID = Convert.ToString(F_Form["masterdata[0].STATEID"]);
                        if (STATEID == "" || STATEID == null || STATEID == "0")
                        { transactionmaster.STATEID = 0; }
                        else { transactionmaster.STATEID = Convert.ToInt32(STATEID); }

                        transactionmaster.CATEAGSTNO = Convert.ToString(F_Form["masterdata[0].CATEAGSTNO"]);
                        transactionmaster.TRANIMPADDR1 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR1"]);
                        transactionmaster.TRANIMPADDR2 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR2"]);
                        transactionmaster.TRANIMPADDR3 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR3"]);
                        transactionmaster.TRANIMPADDR4 = Convert.ToString(F_Form["masterdata[0].TRANIMPADDR4"]);

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
                        string bill = "";
                        if (Convert.ToInt32(F_Form["TRANBTYPE"]) == 1) bill = "LI";
                        else bill = "DS";
                        if (TRANMID == 0)
                        {
                            //transactionmaster.TRANNO = Convert.ToInt16(Autonumber.autonum("transactionmaster", "TRANNO", "TRANNO<>0 and TRANBTYPE=" + Convert.ToInt16(F_Form["TRANBTYPE"]) + " and REGSTRID=" + Convert.ToInt16(F_Form["REGSTRID"]) + " and SDPTID=2 and compyid=" + Convert.ToInt32(Session["compyid"]) + "").ToString());
                            if (regsid == 49)
                                transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.gstexportautonum("transactionmaster", "TRANNO", F_Form["REGSTRID"].ToString(), Session["compyid"].ToString(), F_Form["TRANBTYPE"].ToString(), Convert.ToInt32(transactionmaster.TRANREFID)).ToString());
                            else
                                transactionmaster.TRANNO = Convert.ToInt32(auto_numbr_invoice.gstexportautonum_BS("transactionmaster", "TRANNO", F_Form["REGSTRID"].ToString(), Session["compyid"].ToString(), F_Form["TRANBTYPE"].ToString(), Convert.ToInt32(transactionmaster.TRANREFID)).ToString());
                            int ano = transactionmaster.TRANNO;
                            //string format = "SUD/EXP/";
                            string format = "EXP/" ;
                            string btyp = auto_numbr_invoice.GetCateBillType(Convert.ToInt32(transactionmaster.TRANREFID)).ToString();
                            if (btyp == "")
                            {
                                format = format + Session["GPrxDesc"] + "/";
                            }
                            else
                                format = format.Replace("/", "") + Session["GPrxDesc"] + btyp;
                            string billformat = "";
                            switch (regsid)
                            {
                                //case 21: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                                //case 22: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                                //case 23: billformat = "STL/MNUL/EXP/"; break;

                                case 49: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                                case 50: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                                                                              //case 23: billformat = "STL/MNUL/EXP/"; break;
                            }
                            string prfx = string.Format(format + "{0:D5}", ano);
                            string billprfx = string.Format(billformat + "{0:D5}", ano);
                            transactionmaster.TRANDNO = prfx.ToString();
                            transactionmaster.TRANBILLREFNO = billprfx.ToString();

                            //........end of autonumber
                            context.transactionmaster.Add(transactionmaster);
                            context.SaveChanges();
                            TRANMID = transactionmaster.TRANMID;
                        }
                        else
                        {
                            //int ano = transactionmaster.TRANNO;
                            ////string format = "SUD/EXP/";
                            //string format = "EXP/";
                            //string btyp = auto_numbr_invoice.GetCateBillType(Convert.ToInt32(transactionmaster.TRANREFID)).ToString();
                            //if (btyp == "")
                            //{
                            //    format = format + Session["GPrxDesc"] + "/";
                            //}
                            //else
                            //    format = format.Replace("/", "") + Session["GPrxDesc"] + btyp;
                            //string billformat = "";
                            //switch (regsid)
                            //{
                            //    //case 21: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                            //    //case 22: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                            //    //case 23: billformat = "STL/MNUL/EXP/"; break;

                            //    case 49: billformat = "STL/MNUL/EXP/"; break; //"EXP/LI/"; break;
                            //    case 50: billformat = "STL/MNUL/EXP/"; break; //"EXP/" + bill + "/"; break;
                            //                                                  // case 23: billformat = "STL/MNUL/EXP/"; break;
                            //}
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
                        string[] TRANIDATE = F_Form.GetValues("TRANIDATE");
                        string[] TRANSDATE = F_Form.GetValues("TRANSDATE");
                        string[] TRANEDATE = F_Form.GetValues("TRANEDATE");

                        string[] TRANDHSNCODE = F_Form.GetValues("TRANDHSNCODE");
                        string[] TRANDGST = F_Form.GetValues("TRANDGST");

                        string[] TRANDNAMT = F_Form.GetValues("TRANDNAMT");
                        string[] TRANDGAMT = F_Form.GetValues("TRANDGAMT");

                        string[] TRAND_STRG_CGST_EXPRN = F_Form.GetValues("TRAND_STRG_CGST_EXPRN");
                        string[] TRAND_STRG_SGST_EXPRN = F_Form.GetValues("TRAND_STRG_SGST_EXPRN");
                        string[] TRAND_STRG_IGST_EXPRN = F_Form.GetValues("TRAND_STRG_IGST_EXPRN");

                        string[] TRAND_HANDL_CGST_EXPRN = F_Form.GetValues("TRAND_HANDL_CGST_EXPRN");
                        string[] TRAND_HANDL_SGST_EXPRN = F_Form.GetValues("TRAND_HANDL_SGST_EXPRN");
                        string[] TRAND_HANDL_IGST_EXPRN = F_Form.GetValues("TRAND_HANDL_IGST_EXPRN");

                        string[] TRAND_STRG_CGST_AMT = F_Form.GetValues("TRAND_STRG_CGST_AMT");
                        string[] TRAND_STRG_SGST_AMT = F_Form.GetValues("TRAND_STRG_SGST_AMT");
                        string[] TRAND_STRG_IGST_AMT = F_Form.GetValues("TRAND_STRG_IGST_AMT");

                        string[] TRAND_HANDL_CGST_AMT = F_Form.GetValues("TRAND_HANDL_CGST_AMT");
                        string[] TRAND_HANDL_SGST_AMT = F_Form.GetValues("TRAND_HANDL_SGST_AMT");
                        string[] TRAND_HANDL_IGST_AMT = F_Form.GetValues("TRAND_HANDL_IGST_AMT");

                        string[] ACHEADID;
                        //string[] TRANDREFID;
                        string[] TRANDDESC;
                        string[] TRANDREFNAME = F_Form.GetValues("TRANDREFNAME");

                        if (F_Form.GetValues("detaildata[0].ACHEADID") == null)
                        {
                            ACHEADID = F_Form.GetValues("ACHEADID");
                        }
                        else
                        {
                            ACHEADID = F_Form.GetValues("detaildata[0].ACHEADID");
                        }

                        //if (F_Form.GetValues("detaildata[0].TRANDREFID") == null)
                        //{
                        //    TRANDREFID = F_Form.GetValues("TRANDREFID");
                        //}
                        //else
                        //{
                        //    TRANDREFID = F_Form.GetValues("detaildata[0].TRANDREFID");
                        //}

                        if (F_Form.GetValues("detaildata[0].TRANDDESC") == null)
                        {
                            TRANDDESC = F_Form.GetValues("TRANDDESC");
                        }
                        else
                        {
                            TRANDDESC = F_Form.GetValues("detaildata[0].TRANDDESC");
                        }
                        
                        string[] BILLEDID = F_Form.GetValues("detaildata[0].BILLEDID");
                        string[] TRANVHLFROM = F_Form.GetValues("TRANVHLFROM");
                        string[] TRANVHLTO = F_Form.GetValues("TRANVHLTO");

                        for (int count = 0; count < F_TRANDID.Count(); count++)
                        {
                            TRANDID = Convert.ToInt32(F_TRANDID[count]);

                            if (TRANDID != 0)
                            {
                                transactiondetail = context.transactiondetail.Find(TRANDID);
                            }
                            transactiondetail.TRANMID = transactionmaster.TRANMID;
                            transactiondetail.TRANDREFNO = (TRANDHSNCODE[count]).ToString();
                            transactiondetail.TRANDREFNAME = (TRANDREFNAME[count]).ToString();
                            transactiondetail.TRANDREFID = 0; // Convert.ToInt32(TRANDREFID[count]);
                            transactiondetail.TRANDDESC = (TRANDDESC[count]).ToString();
                            transactiondetail.ACHEADID = Convert.ToInt32(ACHEADID[count]);

                            transactiondetail.TRANIDATE = DateTime.Now;  //Convert.ToDateTime(TRANIDATE[count]);
                            transactiondetail.TRANSDATE = DateTime.Now;
                            transactiondetail.TRANEDATE = DateTime.Now;
                            transactiondetail.TRANVDATE = DateTime.Now;
                            transactiondetail.TRANVHLFROM = ""; //(TRANVHLFROM[count]).ToString();
                            transactiondetail.TRANVHLTO = "";// (TRANVHLTO[count]).ToString();

                            transactiondetail.TRANDQTY = Convert.ToDecimal(TRANDGST[count]);

                            transactiondetail.TRAND_STRG_CGST_EXPRN = Convert.ToDecimal(TRAND_STRG_CGST_EXPRN[count]);
                            transactiondetail.TRAND_STRG_SGST_EXPRN = Convert.ToDecimal(TRAND_STRG_SGST_EXPRN[count]);
                            transactiondetail.TRAND_STRG_IGST_EXPRN = Convert.ToDecimal(TRAND_STRG_IGST_EXPRN[count]);

                            transactiondetail.TRAND_HANDL_CGST_EXPRN = Convert.ToDecimal(TRAND_HANDL_CGST_EXPRN[count]);
                            transactiondetail.TRAND_HANDL_SGST_EXPRN = Convert.ToDecimal(TRAND_HANDL_SGST_EXPRN[count]);
                            transactiondetail.TRAND_HANDL_IGST_EXPRN = Convert.ToDecimal(TRAND_HANDL_IGST_EXPRN[count]);

                            transactiondetail.TRAND_STRG_CGST_AMT = Convert.ToDecimal(TRAND_STRG_CGST_AMT[count]);
                            transactiondetail.TRAND_STRG_SGST_AMT = Convert.ToDecimal(TRAND_STRG_SGST_AMT[count]);
                            transactiondetail.TRAND_STRG_IGST_AMT = Convert.ToDecimal(TRAND_STRG_IGST_AMT[count]);

                            transactiondetail.TRAND_HANDL_CGST_AMT = Convert.ToDecimal(TRAND_HANDL_CGST_AMT[count]);
                            transactiondetail.TRAND_HANDL_SGST_AMT = Convert.ToDecimal(TRAND_HANDL_SGST_AMT[count]);
                            transactiondetail.TRAND_HANDL_IGST_AMT = Convert.ToDecimal(TRAND_HANDL_IGST_AMT[count]);

                            transactiondetail.TRANDGAMT = Convert.ToDecimal(TRANDGAMT[count]);
                            transactiondetail.TRANDNAMT = Convert.ToDecimal(TRANDNAMT[count]);

                            transactiondetail.BILLEDID = 0;//Convert.ToInt32(BILLEDID[count]);
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
                        //context.Database.ExecuteSqlCommand("EXEC PR_EXPORT_MANUAL_BILL_TRANMASTER_UPD @PTRANMID =" + TRANMID);
                        trans.Commit();
                        TRANMID = transactionmaster.TRANMID;
                        context.Database.ExecuteSqlCommand("EXEC PR_EXPORT_MANUAL_BILL_TRANMASTER_UPD @PTRANMID =" + TRANMID);
                        
                        // Log changes to GateInDetailEditLog after successful save
                        try
                        {
                            if (TRANMID != 0 && original != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ORIGINAL RECORD FOUND: TRANMID={original.TRANMID}, calling LogTransactionEdits");
                                
                                // Ensure baseline snapshot (Version = "0") exists for this record before logging diffs
                                EnsureBaselineVersionZero(original, Session["CUSRID"] != null ? Session["CUSRID"].ToString() : "");
                                
                                // Reload the saved record to get the final state (after transaction commit)
                                using (var logContext = new SCFSERPContext())
                                {
                                    var savedRecord = logContext.transactionmaster.AsNoTracking().FirstOrDefault(x => x.TRANMID == TRANMID);
                                    if (savedRecord != null)
                                    {
                                        LogTransactionEdits(original, savedRecord, Session["CUSRID"] != null ? Session["CUSRID"].ToString() : "");
                                        System.Diagnostics.Debug.WriteLine($"LogTransactionEdits completed successfully");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"SAVED RECORD NOT FOUND after commit for TRANMID={transactionmaster.TRANMID}");
                                    }
                                }
                            }
                            else if (TRANMID != 0 && original == null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ORIGINAL RECORD NOT FOUND for TRANMID={transactionmaster.TRANMID}");
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
                    catch (Exception EX)
                    {
                        var msg = EX.InnerException;
                        trans.Rollback();
                        Response.Write("Sorry!!An Error Ocurred...");
                    }
                }
            }

        }

        //--------Autocomplete CHA Name
        public JsonResult AutoCha(string term)
        {
            var result = (from r in context.categorymasters.Where(m => m.CATETID == 4).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID }).Distinct().OrderBy(X => X.CATENAME);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //---End 

        public JsonResult NewAutoCha(string term)
        {

            var result = (from r in context.VW_NEW_CATEGORY_STATETYPE_ASSGNs.Where(m => m.CATETID == 4 || m.CATETID == 2).Where(x => x.DISPSTATUS == 0)
                          where r.CATENAME.ToLower().Contains(term.ToLower())
                          select new { r.CATENAME, r.CATEID, r.STATETYPE }).Distinct().OrderBy(X => X.CATENAME);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetStateType(int? id = 0)
        {
            if (id > 0)
            {
                var result = context.Database.SqlQuery<StateMaster>("Select *from STATEMASTER WHERE STATEID=" + Convert.ToInt32(id)).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var result = "";
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetCharge(int id)//Get 
        {
            var result = context.Database.SqlQuery<ChargeMaster>("select * from ChargeMaster where CONTNRSID=" + id).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetAmount(int id)//Get 
        {

            var result = context.Database.SqlQuery<ChargeMaster>("select * from ChargeMaster where CHRGID=" + id).ToList();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        //---End 



        //......end

        //.............................storage ,handling,energy,fuel, and PTI amount...........//
        public JsonResult Bill_Detail(string id)
        {
            var param = id.Split('-');

            var TARIFFMID = Convert.ToInt32(param[0]);

            var CHRGETYPE = Convert.ToInt32(param[1]);
            var CONTNRSID = Convert.ToInt32(param[2]);
            var CHAID = Convert.ToInt32(param[3]);
            if (TARIFFMID == 4)/* INSTEAD OF SLABTID=6 ,,PARAM[4]*/
            {
                var query = context.Database.SqlQuery<VW_EXPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_EXPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + Convert.ToInt32(param[4]) + ",14,15,16) and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + "and CHAID=" + CHAID).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var query = context.Database.SqlQuery<VW_EXPORT_RATECARDMASTER_FLX_ASSGN>("select SLABTID,SLABAMT from VW_EXPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + " and SLABTID in (" + Convert.ToInt32(param[4]) + ",14,15,16) and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID).ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }


        }//.....end

        //............ratecardmaster.....................
        public JsonResult RATECARD(string id)
        {
            var param = id.Split('-');
            var TARIFFMID = Convert.ToInt32(param[0]);
            var CHRGETYPE = Convert.ToInt32(param[1]);
            var CONTNRSID = Convert.ToInt32(param[2]);
            var CHAID = Convert.ToInt32(param[3]);
            var SLABMIN = Convert.ToInt32(param[4]);
            if (TARIFFMID == 4)
            {
                var query = context.Database.SqlQuery<VW_EXPORT_RATECARDMASTER_FLX_ASSGN>("select SLABAMT,SLABMIN,SLABMAX from VW_EXPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + "and SLABTID=7 and HTYPE=0 and SDTYPE=1 and SLABMIN <= " + SLABMIN + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " and CHAID=" + CHAID + " order by SLABMIN").ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {

                var query = context.Database.SqlQuery<VW_EXPORT_RATECARDMASTER_FLX_ASSGN>("select SLABAMT,SLABMIN,SLABMAX from VW_EXPORT_RATECARDMASTER_FLX_ASSGN where TARIFFMID=" + TARIFFMID + "and SLABTID=7 and HTYPE=0 and SDTYPE=1 and SLABMIN <= " + SLABMIN + " and CHRGETYPE=" + CHRGETYPE + " and CONTNRSID= " + CONTNRSID + " order by SLABMIN").ToList();

                return Json(query, JsonRequestBehavior.AllowGet);


            }

        } //...end



        public JsonResult datediff(string id)
        {
            var param = id.Split('-');

            var SDATE = Convert.ToInt32(param[0]);

            var EDATE = Convert.ToInt32(param[1]);
            var Start = Convert.ToDateTime(SDATE);
            var End = Convert.ToDateTime(EDATE);
            Response.Write(SDATE + "------" + EDATE);
            Response.End();
            var DateDiff = End.Subtract(Start).Days;
            return Json(DateDiff, JsonRequestBehavior.AllowGet);
        }

        //...........cost factor with default value
        public JsonResult DefaultCF()
        {
            //   DbSqlQuery<CostFactorMaster> data = context.costfactormasters.SqlQuery("select * from costfactormaster  where CFID  in(2) order by CFID DESC");
            //DbSqlQuery<CostFactorMaster> data2 = context.costfactormasters.SqlQuery("select * from costfactormaster  where DISPSTATUS=0 and CFID  in(96,97) order by CFID");
            //data.Concat(data2);
            var data = context.Database.SqlQuery<CostFactorMaster>("select * from costfactormaster  where ((DISPSTATUS=0 and CFID  in(96,97)) or  CFID  in(2)) order by CFID").ToList();
            
            return Json(data, JsonRequestBehavior.AllowGet);

        }//....end

        //..........................Printview...
        [Authorize(Roles = "ExportManualBillPrint")]
        public void PrintView(string id)
        {
            var param = id.Split('~');

            var ids = Convert.ToInt32(param[0]);
            var gsttype = Convert.ToInt32(param[1]);

            //  ........delete TMPRPT...//
            context.Database.ExecuteSqlCommand("DELETE FROM TMPRPT_IDS WHERE KUSRID='" + Session["CUSRID"] + "'");
            var TMPRPT_IDS = TMP_InsertPrint.InsertToTMP("TMPRPT_IDS", "EXPORT_OSBILL", Convert.ToInt32(ids), Session["CUSRID"].ToString());
            if (TMPRPT_IDS == "Successfully Added")
            {
                ReportDocument cryRpt = new ReportDocument();
                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
                ConnectionInfo crConnectionInfo = new ConnectionInfo();
                Tables CrTables;



                // cryRpt.Load(Server.MapPath("~/") + "//Reports//RPT_0302.rpt");


                //........Get TRANPCOUNT...//
                var Query = context.Database.SqlQuery<int>("select TRANPCOUNT from transactionmaster where TRANMID=" + ids).ToList();
                var PCNT = 0;

                if (Query.Count() != 0) { PCNT = Query[0]; }
                var TRANPCOUNT = ++PCNT;
                // Response.Write(++PCNT);
                // Response.End();

                context.Database.ExecuteSqlCommand("UPDATE transactionmaster SET TRANPCOUNT=" + TRANPCOUNT + " WHERE TRANMID=" + ids);

                // if (gsttype == 0)
                // { cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"]+  "EXPORT_OS_Detail.RPT"); }
                // else
                //{ cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"]+  "GST_EXPORT_OS_Detail.RPT"); }
                cryRpt.Load(ConfigurationManager.AppSettings["Reporturl"] + "GST_EXPORT_OS_Detail.RPT");

                cryRpt.RecordSelectionFormula = "{VW_EXPORT_OS_DETAIL_RPT.KUSRID} = '" + Session["CUSRID"].ToString() + "' AND {VW_EXPORT_OS_DETAIL_RPT.TRANMID} = " + ids;


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
        public JsonResult GetGSTRATE(string id)
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
                var query = context.Database.SqlQuery<VW_EXPORT_SLABTYPE_HSN_DETAIL_ASSGN>("select HSNCODE,CGSTEXPRN,SGSTEXPRN,IGSTEXPRN from VW_EXPORT_SLABTYPE_HSN_DETAIL_ASSGN where SLABTID=" + SlabTId + " order by HSNCODE").ToList();
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            else
            {

                var query = context.Database.SqlQuery<VW_EXPORT_SLABTYPE_HSN_DETAIL_ASSGN>("select HSNCODE,ACGSTEXPRN as CGSTEXPRN,ASGSTEXPRN as SGSTEXPRN,AIGSTEXPRN as IGSTEXPRN from VW_EXPORT_SLABTYPE_HSN_DETAIL_ASSGN where SLABTID=" + SlabTId + " order by HSNCODE").ToList();

                return Json(query, JsonRequestBehavior.AllowGet);


            }

        }
        //...............Delete Row.............
        [Authorize(Roles = "ExportManualBillDelete")]
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

        #region Edit Log Methods
        public ActionResult EditLogExportManualBill(int? tranmid, DateTime? from = null, DateTime? to = null, string user = null, string fieldName = null, string version = null)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            var list = new List<scfs_erp.Models.GateInDetailEditLogRow>();
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs != null && !string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                // Convert tranmid to string for GIDNO comparison (GIDNO stores TRANMID as string)
                string gidnoParam = tranmid.HasValue ? tranmid.Value.ToString() : null;
                
                using (var sql = new SqlConnection(cs.ConnectionString))
                using (var cmd = new SqlCommand(@"SELECT TOP 2000 [GIDNO],[FieldName],[OldValue],[NewValue],[ChangedBy],[ChangedOn],[Version],[Modules]
                                                FROM [dbo].[GateInDetailEditLog]
                                                WHERE [Modules] = 'ExportManualBill'
                                                  AND (@GIDNO IS NULL OR [GIDNO] = @GIDNO)
                                                  AND (@FROM IS NULL OR [ChangedOn] >= @FROM)
                                                  AND (@TO   IS NULL OR [ChangedOn] <  DATEADD(day, 1, @TO))
                                                  AND (@USER IS NULL OR [ChangedBy] LIKE @USERPAT)
                                                  AND (@FIELD IS NULL OR [FieldName] LIKE @FIELDPAT)
                                                  AND (@VERSION IS NULL OR [Version] LIKE @VERPAT)
                                                  AND NOT (RTRIM(LTRIM([Version])) IN ('0','V0') OR LEFT(RTRIM(LTRIM([Version])),3) IN ('v0-','V0-'))
                                                  -- Only show rows where OldValue and NewValue are actually different
                                                  AND (
                                                      (LTRIM(RTRIM(ISNULL([OldValue], ''))) != LTRIM(RTRIM(ISNULL([NewValue], ''))))
                                                      OR (LTRIM(RTRIM(ISNULL([OldValue], ''))) = '' AND LTRIM(RTRIM(ISNULL([NewValue], ''))) != '')
                                                      OR (LTRIM(RTRIM(ISNULL([OldValue], ''))) != '' AND LTRIM(RTRIM(ISNULL([NewValue], ''))) = '')
                                                  )
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

            // Map raw DB codes to form-friendly display values for known fields
            try
            {
                // Build lookup dictionaries once
                var dictBank = context.bankmasters.ToDictionary(x => x.BANKMID, x => x.BANKMDESC);
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTariff = context.tariffmasters.ToDictionary(x => x.TARIFFMID, x => x.TARIFFMDESC);
                var dictMode = context.transactionmodemaster.ToDictionary(x => x.TRANMODE, x => x.TRANMODEDETL);

                var dictState = context.statemasters.ToDictionary(x => x.STATEID, x => x.STATEDESC);
                var dictTallyCHA = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTallyCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictAccountHead = context.accountheadmasters.ToDictionary(x => x.ACHEADID, x => x.ACHEADDESC);

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrWhiteSpace(val)) return val;
                    try
                    {
                        int id = 0;
                        if (field == "BANKMID" && int.TryParse(val, out id) && dictBank.ContainsKey(id))
                            return dictBank[id];
                        if (field == "LCATEID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "CATEAID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "TCATEAID" && int.TryParse(val, out id) && dictTallyCate.ContainsKey(id))
                            return dictTallyCate[id];
                        if (field == "TARIFFMID" && int.TryParse(val, out id) && dictTariff.ContainsKey(id))
                            return dictTariff[id];
                        if (field == "TRANMODE" && int.TryParse(val, out id) && dictMode.ContainsKey(id))
                            return dictMode[id];
                        if (field == "STATEID" && int.TryParse(val, out id) && dictState.ContainsKey(id))
                            return dictState[id];
                        if (field == "TSTATEID" && int.TryParse(val, out id) && dictState.ContainsKey(id))
                            return dictState[id];
                        if (field == "TRANTALLYCHAID" && int.TryParse(val, out id) && dictTallyCHA.ContainsKey(id))
                            return dictTallyCHA[id];
                        if (field == "ACHEADID" && int.TryParse(val, out id) && dictAccountHead.ContainsKey(id))
                            return dictAccountHead[id];
                        if (field == "Detail.ACHEADID" && int.TryParse(val, out id) && dictAccountHead.ContainsKey(id))
                            return dictAccountHead[id];
                        if (field == "DISPSTATUS")
                            return val == "1" ? "CANCELLED" : val == "0" ? "INBOOKS" : val;
                    }
                    catch { }
                    return val;
                };

                Func<string, string> Friendly = field =>
                {
                    var fieldNameMap = GetTransactionFieldDisplayNames();
                    if (fieldNameMap.ContainsKey(field)) return fieldNameMap[field];
                    return field.Replace("_", " ").Trim();
                };

                // Filter out removed fields before processing
                list = list.Where(row => {
                    if (row.FieldName == null) return true;
                    var rowFieldName = row.FieldName.Trim();
                    // Skip removed fields
                    if (rowFieldName.Equals("TRANREFID", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("TRANREFBNAME", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("TRANAMTWRDS", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("TRANLMDATE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("TRANLSDATE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_SGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_CGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_IGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_SGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_CGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_IGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("STRG_CGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("STRG_SGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("STRG_SGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("STRG_CGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_TAXABLE_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("STRG_TAXABLE_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("HANDL_HSNCODE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("STRG_HSNCODE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("CHA", StringComparison.OrdinalIgnoreCase) && row.FieldName.Equals("TRANREFID", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Bank", StringComparison.OrdinalIgnoreCase) && row.FieldName.Equals("TRANREFBNAME", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Amount in Words", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Lorry Memo Date", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Lorry Slip Date", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling SGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling CGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling IGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling SGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling CGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling IGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Storage CGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Storage SGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Storage SGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Storage CGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling Taxable Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Storage Taxable Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Handling HSN Code", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Storage HSN Code", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("TRANTALLYCHAID", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldName.Equals("Tally CHA", StringComparison.OrdinalIgnoreCase) && row.FieldName.Equals("TRANTALLYCHAID", StringComparison.OrdinalIgnoreCase))
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

            ViewBag.Module = "ExportManualBill";
            return View("~/Views/ImportGateIn/EditLogGateIn.cshtml", list);
        }

        // Compare two versions for a given TRANMID
        public ActionResult EditLogExportManualBillCompare(int? tranmid, string versionA, string versionB)
        {
            if (Convert.ToInt32(Session["compyid"]) == 0) { return RedirectToAction("Login", "Account"); }

            // Fallbacks: try alternate parameter names
            if (tranmid == null)
            {
                int tmp;
                var qsTran = Request["tranmid"] ?? Request["id"];
                if (!string.IsNullOrWhiteSpace(qsTran) && int.TryParse(qsTran.Trim(), out tmp))
                {
                    tranmid = tmp;
                }
            }

            // Normalize version strings (remove any whitespace/tabs)
            versionA = (versionA ?? string.Empty).Trim();
            versionB = (versionB ?? string.Empty).Trim();

            if (tranmid == null || string.IsNullOrWhiteSpace(versionA) || string.IsNullOrWhiteSpace(versionB))
            {
                TempData["Err"] = "Please provide TRANMID, Version A and Version B to compare.";
                return RedirectToAction("EditLogExportManualBill", new { tranmid = tranmid });
            }

            // Use TRANMID as the primary identifier (consistent with logging)
            string gidnoString = tranmid.HasValue ? tranmid.Value.ToString() : "";

            // Support baseline shortcuts
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
                                                WHERE [GIDNO]=@GIDNO AND [Modules]='ExportManualBill' AND (RTRIM(LTRIM([Version]))=@V OR RTRIM(LTRIM([Version]))=@VLower OR RTRIM(LTRIM([Version]))=@VUpper)", sql))
                {
                    cmd.Parameters.Add("@GIDNO", System.Data.SqlDbType.NVarChar, 50);
                    cmd.Parameters.Add("@V", System.Data.SqlDbType.NVarChar, 100);
                    cmd.Parameters.Add("@VLower", System.Data.SqlDbType.NVarChar, 100);
                    cmd.Parameters.Add("@VUpper", System.Data.SqlDbType.NVarChar, 100);

                    sql.Open();
                    cmd.Parameters["@GIDNO"].Value = gidnoString;
                    cmd.Parameters["@V"].Value = versionA.Trim();
                    cmd.Parameters["@VLower"].Value = versionA.Trim().ToLower();
                    cmd.Parameters["@VUpper"].Value = versionA.Trim().ToUpper();
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
                    cmd.Parameters["@VLower"].Value = versionB.Trim().ToLower();
                    cmd.Parameters["@VUpper"].Value = versionB.Trim().ToUpper();
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
                var dictBank = context.bankmasters.ToDictionary(x => x.BANKMID, x => x.BANKMDESC);
                var dictCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTariff = context.tariffmasters.ToDictionary(x => x.TARIFFMID, x => x.TARIFFMDESC);
                var dictMode = context.transactionmodemaster.ToDictionary(x => x.TRANMODE, x => x.TRANMODEDETL);

                var dictState = context.statemasters.ToDictionary(x => x.STATEID, x => x.STATEDESC);
                var dictTallyCHA = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictTallyCate = context.categorymasters.ToDictionary(x => x.CATEID, x => x.CATENAME);
                var dictAccountHead = context.accountheadmasters.ToDictionary(x => x.ACHEADID, x => x.ACHEADDESC);

                Func<string, string, string> Map = (field, val) =>
                {
                    if (string.IsNullOrWhiteSpace(val)) return val;
                    try
                    {
                        int id = 0;
                        if (field == "BANKMID" && int.TryParse(val, out id) && dictBank.ContainsKey(id))
                            return dictBank[id];
                        if (field == "LCATEID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "CATEAID" && int.TryParse(val, out id) && dictCate.ContainsKey(id))
                            return dictCate[id];
                        if (field == "TCATEAID" && int.TryParse(val, out id) && dictTallyCate.ContainsKey(id))
                            return dictTallyCate[id];
                        if (field == "TARIFFMID" && int.TryParse(val, out id) && dictTariff.ContainsKey(id))
                            return dictTariff[id];
                        if (field == "TRANMODE" && int.TryParse(val, out id) && dictMode.ContainsKey(id))
                            return dictMode[id];
                        if (field == "STATEID" && int.TryParse(val, out id) && dictState.ContainsKey(id))
                            return dictState[id];
                        if (field == "TSTATEID" && int.TryParse(val, out id) && dictState.ContainsKey(id))
                            return dictState[id];
                        if (field == "TRANTALLYCHAID" && int.TryParse(val, out id) && dictTallyCHA.ContainsKey(id))
                            return dictTallyCHA[id];
                        if (field == "ACHEADID" && int.TryParse(val, out id) && dictAccountHead.ContainsKey(id))
                            return dictAccountHead[id];
                        if (field == "Detail.ACHEADID" && int.TryParse(val, out id) && dictAccountHead.ContainsKey(id))
                            return dictAccountHead[id];
                        if (field == "DISPSTATUS")
                            return val == "1" ? "CANCELLED" : val == "0" ? "INBOOKS" : val;
                    }
                    catch { }
                    return val;
                };

                Func<string, string> Friendly = field =>
                {
                    var fieldNameMap = GetTransactionFieldDisplayNames();
                    if (fieldNameMap.ContainsKey(field)) return fieldNameMap[field];
                    return field.Replace("_", " ").Trim();
                };

                // Filter out removed fields before processing
                a = a.Where(row => {
                    if (row.FieldName == null) return true;
                    var rowFieldNameA = row.FieldName.Trim();
                    // Skip removed fields
                    if (rowFieldNameA.Equals("TRANREFID", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("TRANREFBNAME", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("TRANAMTWRDS", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("TRANLMDATE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("TRANLSDATE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("HANDL_SGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("HANDL_CGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("HANDL_SGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("HANDL_CGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("STRG_CGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("STRG_SGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("STRG_SGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("STRG_CGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("HANDL_TAXABLE_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("STRG_TAXABLE_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("HANDL_HSNCODE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("STRG_HSNCODE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("CHA", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Bank", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Amount in Words", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Lorry Memo Date", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Lorry Slip Date", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Handling SGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Handling CGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Handling SGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Handling CGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Storage CGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Storage SGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Storage SGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Storage CGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Handling Taxable Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Storage Taxable Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Handling HSN Code", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameA.Equals("Storage HSN Code", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }).ToList();
                
                b = b.Where(row => {
                    if (row.FieldName == null) return true;
                    var rowFieldNameB = row.FieldName.Trim();
                    // Skip removed fields
                    if (rowFieldNameB.Equals("TRANREFID", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("TRANREFBNAME", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("TRANAMTWRDS", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("TRANLMDATE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("TRANLSDATE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("HANDL_SGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("HANDL_CGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("HANDL_SGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("HANDL_CGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("STRG_CGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("STRG_SGST_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("STRG_SGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("STRG_CGST_EXPRN", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("HANDL_TAXABLE_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("STRG_TAXABLE_AMT", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("HANDL_HSNCODE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("STRG_HSNCODE", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("CHA", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Bank", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Amount in Words", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Lorry Memo Date", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Lorry Slip Date", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Handling SGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Handling CGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Handling SGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Handling CGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Storage CGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Storage SGST Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Storage SGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Storage CGST %", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Handling Taxable Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Storage Taxable Amount", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Handling HSN Code", StringComparison.OrdinalIgnoreCase) ||
                        rowFieldNameB.Equals("Storage HSN Code", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return true;
                }).ToList();

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
            ViewBag.Module = "ExportManualBill";
            return View("~/Views/ImportGateIn/EditLogGateInCompare.cshtml");
        }

        private static Dictionary<string, string> GetTransactionFieldDisplayNames()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"TRANDATE", "Date"}, {"TRANTIME", "Date/Time"}, {"TRANDNO", "Bill Number"}, {"TRANNO", "No"},
                {"TRANREFNAME", "CHA"}, {"BANKMID", "Bank"}, {"LCATEID", "Location Category"},
                {"TRANMODE", "Mode"}, {"TRANMODEDETL", "Mode Detail"}, {"TRANGAMT", "Gross Amount"}, {"TRANNAMT", "Net Amount"},
                {"TRANROAMT", "Round Off Amount"}, {"TRANREFAMT", "Amount"}, {"TRANRMKS", "Remarks"},
                {"TRANCGSTAMT", "C.G.S.T."}, {"TRANSGSTAMT", "S.G.S.T."}, {"TRANIGSTAMT", "I.G.S.T."},
                {"CATEAID", "Location"}, {"STATEID", "State"}, {"CATEAGSTNO", "GST NO"}, {"REGSTRID", "Tax Type"},
                {"DISPSTATUS", "Status"}, {"PRCSDATE", "Process Date"},
                {"TRANBTYPE", "Bill Type"}, {"TRANREFNO", "Number"}, {"TRANREFDATE", "Date"},
                {"TRANTALLYCHANAME", "Tally CHA"}, {"TCATEAID", "Tally CHA Location"}, 
                {"TCATEAGSTNO", "Tally CHA GST NO"}, {"TSTATEID", "State"}, {"TRANIMPADDR1", "Address 1"}, 
                {"TRANIMPADDR2", "Address 2"}, {"TRANIMPADDR3", "Address 3"}, {"TRANIMPADDR4", "Address 4"},
                // Detail field mappings
                {"Detail.ACHEADID", "Account Head"}, {"Detail.TRANDDESC", "Bill Description"},
                {"Detail.TRANDREFNO", "HSN Code"}, {"Detail.TRANDGAMT", "Amount"},
                {"ACHEADID", "Account Head"}, {"TRANDDESC", "Bill Description"},
                {"TRANDREFNO", "HSN Code"}, {"TRANDREFNAME", "Bill Description"}
            };
        }

        private void LogTransactionEdits(TransactionMaster before, TransactionMaster after, string userId)
        {
            if (before == null || after == null)
            {
                System.Diagnostics.Debug.WriteLine($"LogTransactionEdits: before={before != null}, after={after != null}");
                return;
            }
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
            {
                System.Diagnostics.Debug.WriteLine("LogTransactionEdits: No SCFSERP_EditLog connection string found");
                return;
            }

            var fieldDisplayNames = GetTransactionFieldDisplayNames();

            // Exclude system or noisy fields
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANMID", "COMPYID", "SDPTID", "PRCSDATE", "LMUSRID", "CUSRID",
                "TRANTID", "TRANPCOUNT", "TRANCSNAME", "LEMID", "TRANAHAMT",
                "TRANHBLNO", "TRANPONO",
                "SLABNARN_HANDLDESC", "SLABNARN_ADNLDESC", "SLABNARN_STS",
                "TALLYSTAT", "IRNNO", "ACKNO", "ACKDT", "QRCODEPATH",
                "TRANGSTNO", "TRANPAMT",
                // Removed fields - no longer displayed
                "TRANREFID", "TRANREFBNAME", "TRANAMTWRDS", "TRANLMDATE", "TRANLSDATE",
                "HANDL_SGST_AMT", "HANDL_CGST_AMT", "HANDL_IGST_AMT", "HANDL_SGST_EXPRN", "HANDL_CGST_EXPRN", "HANDL_IGST_EXPRN",
                "STRG_CGST_AMT", "STRG_SGST_AMT", "STRG_IGST_AMT", "STRG_SGST_EXPRN", "STRG_CGST_EXPRN", "STRG_IGST_EXPRN",
                "HANDL_TAXABLE_AMT", "STRG_TAXABLE_AMT", "HANDL_HSNCODE", "STRG_HSNCODE",
                "TRANTALLYCHAID"  // Tally CHA ID - exclude, only show TRANTALLYCHANAME
            };

            // Compute the next version ONCE per save
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
                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'ExportManualBill'", sql))
                {
                    cmd.Parameters.AddWithValue("@GIDNO", gidno);
                    sql.Open();
                    var obj = cmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                        nextVersion = Convert.ToInt32(obj);
                }
            }
            catch { /* ignore logging version errors */ }

            var props = typeof(TransactionMaster).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                InsertEditLogRow(cs.ConnectionString, gidno, p.Name, os, ns, userId, versionLabel, "ExportManualBill");
            }
            
            System.Diagnostics.Debug.WriteLine($"LogTransactionEdits completed. Total fields processed, changes logged for GIDNO={gidno}");
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
                else if (fieldName == "LCATEID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cate = context.categorymasters.FirstOrDefault(x => x.CATEID == lookupId);
                    if (cate != null) return cate.CATENAME;
                }
                else if (fieldName == "CATEAID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cateAddr = context.categoryaddressdetails.FirstOrDefault(x => x.CATEAID == lookupId);
                    if (cateAddr != null && !string.IsNullOrEmpty(cateAddr.CATEATYPEDESC)) return cateAddr.CATEATYPEDESC;
                }
                else if (fieldName == "TCATEAID" && int.TryParse(formattedValue, out lookupId))
                {
                    var cateAddr = context.categoryaddressdetails.FirstOrDefault(x => x.CATEAID == lookupId);
                    if (cateAddr != null && !string.IsNullOrEmpty(cateAddr.CATEATYPEDESC)) return cateAddr.CATEATYPEDESC;
                }
                else if (fieldName == "TARIFFMID" && int.TryParse(formattedValue, out lookupId))
                {
                    var tariff = context.tariffmasters.FirstOrDefault(x => x.TARIFFMID == lookupId);
                    if (tariff != null) return tariff.TARIFFMDESC;
                }
                else if (fieldName == "TRANMODE" && int.TryParse(formattedValue, out lookupId))
                {
                    var mode = context.transactionmodemaster.FirstOrDefault(x => x.TRANMODE == lookupId);
                    if (mode != null) return mode.TRANMODEDETL;
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

        private void EnsureBaselineVersionZero(TransactionMaster snapshot, string userId)
        {
            if (snapshot == null) return;
            var cs = ConfigurationManager.ConnectionStrings["SCFSERP_EditLog"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString)) return;

            var gidno = snapshot.TRANMID.ToString();
            var baselineVer = "v0-" + gidno;

            // Check if baseline already exists
            try
            {
                using (var sql = new SqlConnection(cs.ConnectionString))
                {
                    sql.Open();
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM [dbo].[GateInDetailEditLog] 
                                                    WHERE [GIDNO] = @GIDNO AND [Modules] = 'ExportManualBill' 
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

        private void InsertBaselineSnapshot(TransactionMaster snapshot, string userId, string connectionString, string gidno, string baselineVer)
        {
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
                InsertEditLogRow(connectionString, gidno, p.Name, null, newVal, userId, baselineVer, "ExportManualBill");
            }
        }
        #endregion
    }//----------End of Class
}//-------------End of namespace