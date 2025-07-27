using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Data;
namespace MarkingToMesWebService
{
    /// <summary>
    /// Service1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。
    // [System.Web.Script.Services.ScriptService]
    public class Service1 : System.Web.Services.WebService
    {
        DBUtil dbUtil = DBUtil.GetInstance();
        [WebMethod]
        public string MoNameCheck(string MoName, out int PcbQty)
        {
            string I_ReturnMessage = "";
            PcbQty = 0;
            I_ReturnMessage = "";

            try
            {
                //执行存储过程
                string SqlStr = @" DECLARE @I_ReturnMessage nvarchar(max),
                                       @PcbQty nvarchar(50),
                                       @MoName nvarchar(50)
                               EXEC [dbo].[MoNameCheck]
                               @I_ReturnMessage = @I_ReturnMessage OUTPUT,
                               @PcbQty = @PcbQty OUTPUT,
                               @MoName = '" + MoName + @"'
                               Select @MoName as MoName, @I_ReturnMessage as I_ReturnMessage, @PcbQty as PcbQty ";

                DataSet ds = dbUtil.GetDataSetFormStr(SqlStr);
                if (ds == null || ds.Tables.Count <= 0 || ds.Tables[0].Rows.Count <= 0)
                {
                    I_ReturnMessage = "MES:接口系统错误,请联系MES管理员进行处理";
                    return I_ReturnMessage;
                }
                else
                {
                    I_ReturnMessage = ds.Tables[0].Rows[0]["I_ReturnMessage"].ToString();
                    string StrQty = ds.Tables[0].Rows[0]["PcbQty"].ToString();
                    if (StrQty == "" || StrQty == null)
                    {
                        PcbQty = 0;
                        return I_ReturnMessage;
                    }
                    else
                    {
                        PcbQty = int.Parse(StrQty); 
                    }
                }
                
            }
            catch(Exception ex)
            {
                I_ReturnMessage = ex.Message;
                try
                {
                    string sql = "insert into PMTmp(char1) select '" + I_ReturnMessage.Substring(0, 1000) + "'";
                    dbUtil.ExecuteSQL(sql);
                    
                }catch(Exception er)
                { }
                
                return I_ReturnMessage;
            }

            return I_ReturnMessage;
        }

        [WebMethod]
        public string PrintingFormatList()
        {
            string I_ReturnMessage = "";
            I_ReturnMessage = "";

            try
            {
                //OrBitADCService.ADCService adc = new OrBitADCService.ADCService();
                
                //执行存储过程
                string SqlStr = @" DECLARE @I_ReturnMessage nvarchar(max)
                               EXEC [dbo].[PrintingFormatList]
                               @I_ReturnMessage = @I_ReturnMessage OUTPUT " + @"
                               Select @I_ReturnMessage as I_ReturnMessage ";
                
                DataSet ds = null;
                //ds = adc.GetDataSetWithSQLString(WCFAddress, SqlStr);
                ds = dbUtil.GetDataSetFormStr(SqlStr);
                if (ds == null || ds.Tables.Count <= 0 || ds.Tables[0].Rows.Count <= 0)
                {
                    I_ReturnMessage = "MES:接口系统错误,请联系MES管理员进行处理";
                    return I_ReturnMessage;
                }
                else
                {
                    I_ReturnMessage = ds.Tables[0].Rows[0]["I_ReturnMessage"].ToString();
                }
            }
            catch (Exception ex)
            {
                I_ReturnMessage = ex.Message;
                try
                {
                    string sql = "insert into PMTmp(char1) select '" + I_ReturnMessage.Substring(0, 1000) + "'";
                    dbUtil.ExecuteSQL(sql);
                }
                catch (Exception er)
                { }
                return I_ReturnMessage;
            }

            return I_ReturnMessage;
        }

        [WebMethod]
        public string PCBOnline1(string MoName, string Operator, string EquipmentNOCode, string PMType, bool IsHBIN, string HideLotSN,string ParentLotSn,string SmtBin,string CarrierNO)
        {          
            string[] paras = new string[2]
            {
                MoName,
                EquipmentNOCode
            };
            DataSet dataSet = null;
            try
            {
                dataSet = dbUtil.GetDataSetFromProcess("Txn_GetMessageByMo", paras);
            }
            catch (Exception ex)
            {
                string str = "NG!执行[Txn_GetMessageByMo]错误,工单设备:" + MoName + EquipmentNOCode + "错误信息：" + ex.Message.ToString();
                str = "2" + str;
                dbUtil.WriteLog(str);
                return str;
            }
            if (dataSet == null || dataSet.Tables[0].Rows.Count == 0)
            {
                return "NG!工单[" + MoName + "]不存在";
            }
            string text = "";
            string productName = dataSet.Tables[0].Rows[0]["ProductName"].ToString();
            string workflowName = dataSet.Tables[0].Rows[0]["WorkflowName"].ToString();
            string text2 = dataSet.Tables[0].Rows[0]["StandardPcbQty"].ToString();
            int num = int.Parse(text2);
            int num2 = 0;
            string[] array = HideLotSN.Split(';');
            num2 = array.Length;
            if (num != num2)
            {
                return "NG!工单[" + MoName + "] 的标准拼板数为[" + text2 + "]，实际拼板数为[" + num2.ToString() + "]与标准不符，不能喷码";
            }
            try
            {
                string[] array2 = array;
                foreach (string text3 in array2)
                {
                    if (text3.Trim() != "")
                    {
                        string returnFromStartProcess = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevision", MoName, productName, Operator, text3, text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin, CarrierNO);
                        if (returnFromStartProcess != "0")
                        {
                            return "NG!" + returnFromStartProcess;
                        }
                    }
                    text = ((!(text != "")) ? (text + text3) : (text + ";" + text3));
                }
                string returnFromStartProcess2 = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevision", MoName, productName, Operator, "Submit", text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin, CarrierNO);
                if (returnFromStartProcess2 != "0")
                {
                    return "NG!" + returnFromStartProcess2;
                }
            }
            catch (Exception ex2)
            {
                string str = "";
                str = "1" + ex2.Message.ToString();
                dbUtil.WriteLog(str);
                return "NG!" + str;
            }
            return "OK";
        }

        /// <summary>
        /// 惠州灯条，三星喷码机调用
        /// </summary>
        /// <param name="MoName"></param>
        /// <param name="Operator"></param>
        /// <param name="EquipmentNOCode"></param>
        /// <param name="PMType"></param>
        /// <param name="IsHBIN"></param>
        /// <param name="HideLotSN"></param>
        /// <param name="ParentLotSn"></param>
        /// <param name="SmtBin"></param>
        /// <returns></returns>
        [WebMethod]
        public string LB_PCBOnline(string MoName, string Operator, string EquipmentNOCode, string PMType, bool IsHBIN, string HideLotSN, string ParentLotSn, string SmtBin,string CarrierNO)
        {
            string[] paras = new string[2]
            {
                MoName,
                EquipmentNOCode
            };
            DataSet dataSet = null;
            try
            {
                dataSet = dbUtil.GetDataSetFromProcess("Txn_GetMessageByMo", paras);
            }
            catch (Exception ex)
            {
                string str = "执行[Txn_GetMessageByMo]错误,工单设备:"+ MoName+ EquipmentNOCode+ "错误信息：" + ex.Message.ToString();
                str = "2" + str;
                dbUtil.WriteLog(str);
                return str;
            }
            if (dataSet == null || dataSet.Tables[0].Rows.Count == 0)
            {
                return "工单[" + MoName + "]不存在";
            }
            string text = "";
            string productName = dataSet.Tables[0].Rows[0]["ProductName"].ToString();
            string workflowName = dataSet.Tables[0].Rows[0]["WorkflowName"].ToString();
            string text2 = dataSet.Tables[0].Rows[0]["StandardPcbQty"].ToString();
            int num = int.Parse(text2);
            int num2 = 0;
            string[] array = HideLotSN.Split(';');
            num2 = array.Length;
            if (num != num2)
            {
                return "工单[" + MoName + "] 的标准拼板数为[" + text2 + "]，实际拼板数为[" + num2.ToString() + "]与标准不符，不能喷码";
            }
            try
            {
                string[] array2 = array;
                foreach (string text3 in array2)
                {
                    if (text3.Trim() != "")
                    {
                        string returnFromStartProcess = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevision", MoName, productName, Operator, text3, text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin, CarrierNO);
                        if (returnFromStartProcess != "0")
                        {
                            return returnFromStartProcess;
                        }
                    }
                    text = ((!(text != "")) ? (text + text3) : (text + ";" + text3));
                }
                string returnFromStartProcess2 = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevision", MoName, productName, Operator, "Submit", text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin, CarrierNO);
                if (returnFromStartProcess2 != "0")
                {
                    return returnFromStartProcess2;
                }
            }
            catch (Exception ex2)
            {
                string str = "";
                str = "1" + ex2.Message.ToString();
                dbUtil.WriteLog(str);
                return str;
            }
            return "";
        }

        [WebMethod]
        public string PCBOnline(string MoName, string Operator, string EquipmentNOCode, string PMType, bool IsHBIN, string HideLotSN, string ParentLotSn, string SmtBin,string CarrierNO )
        {
            // 20250723 qianyingying 芜湖重新部署服务 暂时不切换【Txn_LotSNStartScanRevisionBatch】
            return PCBOnline1(MoName, Operator, EquipmentNOCode, PMType, IsHBIN, HideLotSN, ParentLotSn, SmtBin, CarrierNO);
            /*
            dbUtil.WriteLog("开始校验条码：MoName：" + MoName + ";EquipmentNOCode:" + EquipmentNOCode + ";HideLotSN:" + HideLotSN + ";ParentLotSn:" + ParentLotSn);
            string[] paras = new string[2]
            {
                 MoName,
                 EquipmentNOCode
            };
             DataSet dataSet = null;
             try
             {
                 dataSet = dbUtil.GetDataSetFromProcess("Txn_GetMessageByMo", paras);
             }
             catch (Exception ex)
             {
                 string str = "执行[Txn_GetMessageByMo]错误,工单设备:" + MoName + EquipmentNOCode + "错误信息：" + ex.Message.ToString();
                 str = "2" + str;
                 dbUtil.WriteLog(str);
                 return str;
             }
             if (dataSet == null || dataSet.Tables[0].Rows.Count == 0)
             {
                 dbUtil.WriteLog("工单[" + MoName + "]不存在");
                 return "工单[" + MoName + "]不存在";
             }
             string text = "";
             string productName = dataSet.Tables[0].Rows[0]["ProductName"].ToString();
             string workflowName = dataSet.Tables[0].Rows[0]["WorkflowName"].ToString();
             string text2 = dataSet.Tables[0].Rows[0]["StandardPcbQty"].ToString();
             int num = int.Parse(text2);
             int num2 = 0;          
             string[] array = HideLotSN.Split(';');
             num2 = array.Length;
             if (num != num2)
             {
                 string str = "工单[" + MoName + "] 的标准拼板数为[" + text2 + "]，实际拼板数为[" + num2.ToString() + "]与标准不符，不能喷码";
                 dbUtil.WriteLog(str+ ",条码{"+HideLotSN+"}");
                 return str;
             }
             try
             {
                 //string[] array2 = array;
                 //foreach (string text3 in array2)
                 //{
                 //    if (text3.Trim()!="")
                 //    {
                 //        string returnFromStartProcess = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevision", MoName, productName, Operator, text3, text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin,CarrierNO);
                 //        if (returnFromStartProcess != "0")
                 //        {
                 //            return returnFromStartProcess;
                 //        }
                 //    }
                 //    text = ((!(text != "")) ? (text + text3) : (text + ";" + text3));                   
                 //}


                 //string returnFromStartProcess2 = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevision", MoName, productName, Operator, "Submit", text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin, CarrierNO);
                 string returnFromStartProcess2 = dbUtil.GetReturnFromStartProcess("Txn_LotSNStartScanRevisionBatch", MoName, productName, Operator, HideLotSN, text, text2, num2.ToString(), workflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin, CarrierNO);


                 if (returnFromStartProcess2 != "0")
                 {
                     return returnFromStartProcess2;
                 }
             }
             catch (Exception ex2)
             {
                 string str = "";
                 str = "1" + "校验条码报错："+ex2.Message.ToString();
                 dbUtil.WriteLog(str);
                 return str;
             }
             return "";
             */
        }

        /// <summary>
        /// COB 在线使用改方法,其他都使用常规灯条喷码方法
        /// </summary>
        /// <param name="MoName"></param>
        /// <param name="Operator"></param>
        /// <param name="EquipmentNOCode"></param>
        /// <param name="PMType"></param>
        /// <param name="IsHBIN"></param>
        /// <param name="HideLotSN"></param>
        /// <param name="ParentLotSn"></param>
        /// <param name="SmtBin"></param>
        /// <returns></returns>
        [WebMethod]
        public string PCBOnlineMiniPob(string MoName, string Operator, string EquipmentNOCode, string PMType, bool IsHBIN, string HideLotSN, string ParentLotSn, string SmtBin)
        {

            string[] para = { MoName, EquipmentNOCode };

            DataSet ds = null;
            try
            {
                ds = dbUtil.GetDataSetFromProcess("Txn_GetMessageByMo", para);
            }
            catch (Exception er)
            {

                string I_ReturnMessage = "执行[Txn_GetMessageByMo]错误,错误信息：" + er.Message.ToString();
                I_ReturnMessage = "2" + I_ReturnMessage;
                dbUtil.WriteLog(I_ReturnMessage);
                return I_ReturnMessage;
            }


            if (ds == null || ds.Tables[0].Rows.Count == 0)
            {
                string s = "工单[" + MoName + "]不存在";

                return s;
            }

            string ListLotSn = "";
            string ProductName = ds.Tables[0].Rows[0]["ProductName"].ToString();
            string WorkflowName = ds.Tables[0].Rows[0]["WorkflowName"].ToString();
            string StandardPcbQty = ds.Tables[0].Rows[0]["StandardPcbQty"].ToString();
            int SPcbQty = int.Parse(StandardPcbQty);

            int PcbQty = 0;
            string[] LotSn = HideLotSN.Split(';');

            PcbQty = LotSn.Length;

            if (SPcbQty != PcbQty)
            {
                return "工单[" + MoName + "] 的标准拼板数为[" + StandardPcbQty + "]，实际拼板数为[" + PcbQty.ToString() + "]与标准不符，不能喷码";

            }

            try
            {
                foreach (string iLotSn in LotSn)
                {
                    string rtnValue = dbUtil.GetReturnFromStartProcessMiniPob("Txn_LBPrintOnlineScan_MiniPOB",
                        MoName, ProductName, Operator, iLotSn,
                        ListLotSn, StandardPcbQty, PcbQty.ToString(),
                        WorkflowName, EquipmentNOCode, IsHBIN.ToString(),
                        PMType, ParentLotSn, SmtBin);

                    if (rtnValue != "0")
                    {
                        return rtnValue;
                    }
                    if (ListLotSn != "")
                    {
                        ListLotSn = ListLotSn + ";" + iLotSn;
                    }
                    else
                    {
                        ListLotSn = ListLotSn + iLotSn;
                    }
                }

                string rtn = dbUtil.GetReturnFromStartProcessMiniPob("Txn_LBPrintOnlineScan_MiniPOB", MoName, ProductName, Operator, "Submit", ListLotSn, StandardPcbQty, PcbQty.ToString(), WorkflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin);

                if (rtn != "0")
                {
                    return rtn;
                }

            }
            catch (Exception ex)
            {
                string I_ReturnMessage = "";
                I_ReturnMessage = "1" + ex.Message.ToString();
                dbUtil.WriteLog(I_ReturnMessage);
                return I_ReturnMessage;
            }
            return "";
        }

        //喷码格式，灯珠条码（混BIN多个灯珠条码用分隔符隔开）,工单号（多工单需要用分隔符隔开），设备编号，人员工号，
        //拼版数（为空以标准为准，不为空已填的为准，填值处理一次后要清空），备用1，备用2，备用3 这么多参数直接用json格式不更好
        [WebMethod]
        public string GenerateBarCode(string MoName, string Operator, string EquipmentNOCode, string PMType, string LedBarCode, string PcbQty,string Reamrk1, string Reamrk2, string Reamrk3)
        {
            try
            {

                dbUtil.WriteLog("开始获取条码：MoName：" + MoName+ ";EquipmentNOCode:"+ EquipmentNOCode+ ";LedBarCode:"+ LedBarCode+ ";PcbQty:"+ PcbQty);
                string rtnValue = dbUtil.GetReturnFromGenerateBarCode("Pro_SMTPM_DoMethod",
                    MoName, Operator, EquipmentNOCode, PMType,
                    LedBarCode, PcbQty, Reamrk1, Reamrk2, Reamrk3);

                //if (rtnValue != "0")
                //{
                return rtnValue;
                //}  
                //string rtn = dbUtil.GetReturnFromStartProcessMiniPob("Txn_LBPrintOnlineScan_MiniPOB", MoName, ProductName, Operator, "Submit", ListLotSn, StandardPcbQty, PcbQty.ToString(), WorkflowName, EquipmentNOCode, IsHBIN.ToString(), PMType, ParentLotSn, SmtBin);
                //if (rtn != "0")
                //{
                //    return rtn;
                //}

            }
            catch (Exception ex)
            {
                string I_ReturnMessage = "";
                I_ReturnMessage = "1" +"获取条码报错："+ ex.Message.ToString();
                dbUtil.WriteLog(I_ReturnMessage);
                return I_ReturnMessage;
            }
            return "";
        }

        /// <summary>
        /// 常温站喷电压档的
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        public string PrintingVoltageProfile(string LotSN, string customChar1,string customChar2,string customChar3)
        {
            try
            {
                string rtn = dbUtil.GetVFRankInfoFromProcess("Txn_GetVFRankInfo", LotSN, customChar1, customChar2, customChar3);

                if (rtn != "0")
                {
                    return rtn;
                }

            }
            catch (Exception ex)
            {
                string I_ReturnMessage = "";
                I_ReturnMessage = "1" + ex.Message.ToString();
                dbUtil.WriteLog(I_ReturnMessage);
                return I_ReturnMessage;
            }


            return "";
        }
    }
}
