using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.IO;
using System.Diagnostics;

namespace MarkingToMesWebService
{
    public class DBUtil
    {
        private static string FactoryDB = null;

        private static string Factory = null;

        private static SqlConnection connection;

        private static DBUtil dbUtil;

        public static DBUtil GetInstance()
        {
            if (dbUtil == null)
            {
                dbUtil = new DBUtil();
            }

            return dbUtil;
        }

        #region 获取sqlConnection对象
        /// <summary>
        /// 获取sqlConnection对象
        /// </summary>
        /// <returns></returns>
        private static SqlConnection GetConnection()
        {
            try
            {

                if (connection == null)
                {
                    string connstr = DBUtil.GetConnString1("ConnStr:");//GetConnString();
                    if (!connstr.Contains("MultipleActiveResultSets"))
                    {
                        connstr = connstr + ";MultipleActiveResultSets=true";
                    }
                    connection = new SqlConnection(connstr);

                }

                if (connection.State == ConnectionState.Closed)
                    connection.Open();
            }
            catch (Exception er)
            {
                string I_ReturnMessage = "4" + er.Message.ToString();
                dbUtil.WriteLog(I_ReturnMessage);
                throw new Exception(I_ReturnMessage);
            }

            return connection;
        }
        #endregion

        #region 获取连接字符串
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <returns></returns>
        private static string GetConnString()
        {

            DataSet ds = GetValueFromConfig();

            if (ds == null || ds.Tables[0].Rows.Count == 0)
            {
                return null;
            }

            string user = ds.Tables[0].Rows[0]["DBUSER"].ToString();

            string pwd = ds.Tables[0].Rows[0]["DBPWD"].ToString();

            string db = ds.Tables[0].Rows[0]["DB"].ToString();

            string server = ds.Tables[0].Rows[0]["DBSERVER"].ToString();

            string connStr = ds.Tables[0].Rows[0]["CONNSTRING"].ToString();

            connStr = string.Format(connStr, user, pwd, db, server);

            return connStr;
        }
        #endregion

        #region 获取连接字符串 未用到
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <returns></returns>
        private static string GetConnString1()
        {

            string user = GetValueFromConfig("DBUser");

            string pwd = GetValueFromConfig("DBPwd");

            string db = GetValueFromConfig("DB");

            string server = GetValueFromConfig("Server");

            string connStr = GetValueFromConfig("ConnString");

            connStr = string.Format(connStr, user, pwd, db, server);

            return connStr;
        }
        #endregion

        #region 获取配置文件键值 未用到
        /// <summary>
        /// 获取配置文件键值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetValueFromConfig(string key)
        {

            string value = ConfigurationManager.AppSettings[key].ToString();

            return value;
        }
        #endregion

        #region 获取配置文件键值
        /// <summary>
        /// 获取配置文件键值
        /// </summary>
        /// <returns></returns>
        private static DataSet GetValueFromConfig()
        {

            string sql = "SELECT TOP 1 DBSERVER,DB,DBPWD,DBUSER,CONNSTRING FROM dbo.FACTORYDB ";// WHERE FACTORYNAME ='" + Factory + "'";
           // OrBitADCService.ADCService orbit = new OrBitADCService.ADCService();

            DataSet ds = null;// db.GetDataSet(sql);//orbit.GetDataSetWithSQLString(FactoryDB, sql);

            return ds;
        }
        #endregion
        /// <summary>
        /// 执行非查询命令SQL命令
        /// </summary>
        public int ExecuteSQL(string sqlstring)
        {
            int count = -1;
            SqlConnection conn = GetConnection();
            try
            {
                 using (SqlCommand cmd = new SqlCommand(sqlstring, conn))
                {
                    cmd.CommandTimeout = 180;
                    count = cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                count = -1;
            }
            finally
            {
                //close();
            }
            return count;
        }
        #region sql返回数据集
        /// <summary>
        /// 返回数据集
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataSet GetDataSetFormStr(string sql)
        {
            SqlConnection conn = GetConnection();

            using(SqlDataAdapter adapter = new SqlDataAdapter(sql, conn))
            {
                adapter.SelectCommand.CommandTimeout = 180;
                DataSet ds = new DataSet();

                adapter.Fill(ds);

                return ds;
            }
        }
        #endregion

        #region 存储过程返回数据集
        public DataSet GetDataSetFromProcess(string ProcessName, string[] paras)
        {
            SqlConnection connection = GetConnection();

            using(SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 180;
                cmd.CommandText = ProcessName;//存储过程名称

                cmd.Connection = connection;

                cmd.CommandType = CommandType.StoredProcedure;

                int num = 1;

                foreach (string p in paras)
                {
                    string paraName = "@InPara" + num.ToString();

                    SqlParameter para = new SqlParameter(paraName, SqlDbType.NVarChar);

                    para.Direction = ParameterDirection.Input;

                    para.Value = p;

                    cmd.Parameters.Add(para);

                    num++;
                }

                using(SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
                
            }
            
        }
        #endregion

        #region 存储过程 返回值 输出参数

        public string GetReturnFromProcess(string ProcessName, string[] paras, int OutParas)
        {

            using(SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = ProcessName;//存储过程名称

                cmd.Connection = GetConnection();

                cmd.CommandType = CommandType.StoredProcedure;

                int num = 1;

                //输入参数
                foreach (string p in paras)
                {
                    string paraName = "@InPara" + num.ToString();

                    SqlParameter para = new SqlParameter(paraName, SqlDbType.NVarChar);

                    para.Direction = ParameterDirection.Input;

                    para.Value = p;

                    cmd.Parameters.Add(para);

                    num++;
                }

                //输出参数
                for (int p = 1; p <= OutParas; p++)
                {
                    string paraName = "@OutPara" + p.ToString();

                    SqlParameter para = new SqlParameter(paraName, SqlDbType.NVarChar, 2000);

                    para.Direction = ParameterDirection.Output;

                    cmd.Parameters.Add(para);

                }

                //返回值
                SqlParameter rtn = new SqlParameter("@ReturnValue", SqlDbType.Int);

                rtn.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(rtn);

                cmd.ExecuteNonQuery();

                string rtnStr = cmd.Parameters["@ReturnValue"].Value.ToString();

                for (int p = 1; p <= OutParas; p++)
                {
                    string paraName = "@OutPara" + p.ToString();

                    rtnStr = rtnStr + "@" + cmd.Parameters[paraName].Value.ToString();
                }

                return rtnStr;
            }
            
        }

        #endregion

        public string GetVFRankInfoFromProcess(string ProcessName, string LotSN, string customChar1, string customChar2, string customChar3)
        {

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 300;
                cmd.CommandText = ProcessName;//存储过程名称
                cmd.Connection = GetConnection();
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter pMoName = new SqlParameter("LotSN", SqlDbType.NVarChar);
                pMoName.Direction = ParameterDirection.Input;
                pMoName.Value = LotSN;
                cmd.Parameters.Add(pMoName);

                SqlParameter pI_ReturnMessage = new SqlParameter("I_ReturnMessage", SqlDbType.NVarChar, 2000);

                pI_ReturnMessage.Direction = ParameterDirection.Output;

                cmd.Parameters.Add(pI_ReturnMessage);

                //返回值
                SqlParameter rtn = new SqlParameter("@ReturnValue", SqlDbType.Int);
                rtn.Direction = ParameterDirection.ReturnValue;
                cmd.Parameters.Add(rtn);

                System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                string _logStr = "开始执行语句:" + cmd.CommandText.ToString()+",条码：" + LotSN;
                dbUtil.WriteLog(_logStr);
                try
                {
                    cmd.ExecuteNonQuery();
                    stopwatch.Stop();
                }
                catch (Exception er)
                {
                    stopwatch.Stop();
                    TimeSpan timespan = stopwatch.Elapsed;                    

                    dbUtil.WriteLog("执行语句结束,条码："+ LotSN + ",耗时：" + timespan.TotalMilliseconds.ToString() + "ms");

                    string I_ReturnMessage = "3" + er.Message.ToString();
                    dbUtil.WriteLog(I_ReturnMessage);
                    throw new Exception(I_ReturnMessage);
                }


                string rtnStr = cmd.Parameters["@ReturnValue"].Value.ToString();

                //if (rtnStr != "0")
                //{
                return cmd.Parameters["I_ReturnMessage"].Value.ToString();
                //}

                //return rtnStr;
            }
        }

        public string GetReturnFromGenerateBarCode(string ProcessName, string MoName, string Operator, string EquipmentNOCode, 
            string PMType, string LedBarCode, string PcbQty, string Reamrk1, string Reamrk2, string Reamrk3)
        {

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 300;
                cmd.CommandText = ProcessName;//存储过程名称

                cmd.Connection = GetConnection();

                cmd.CommandType = CommandType.StoredProcedure;


                SqlParameter pMoName = new SqlParameter("MoName", SqlDbType.NVarChar);

                pMoName.Direction = ParameterDirection.Input;

                pMoName.Value = MoName;

                cmd.Parameters.Add(pMoName);

                SqlParameter pOperator = new SqlParameter("Operator", SqlDbType.NVarChar);

                pOperator.Direction = ParameterDirection.Input;

                pOperator.Value = Operator;

                cmd.Parameters.Add(pOperator);

                SqlParameter pEquipmentNOCode = new SqlParameter("EquipmentNOCode", SqlDbType.NVarChar);

                pEquipmentNOCode.Direction = ParameterDirection.Input;

                pEquipmentNOCode.Value = EquipmentNOCode;

                cmd.Parameters.Add(pEquipmentNOCode);

                SqlParameter pPMType = new SqlParameter("PMType", SqlDbType.NVarChar);

                pPMType.Direction = ParameterDirection.Input;

                pPMType.Value = PMType;

                cmd.Parameters.Add(pPMType);

                SqlParameter pLedBarCode = new SqlParameter("LedBarCode", SqlDbType.NVarChar);

                pLedBarCode.Direction = ParameterDirection.Input;

                pLedBarCode.Value = LedBarCode;

                cmd.Parameters.Add(pLedBarCode);

                SqlParameter pPcbQty = new SqlParameter("PcbQty", SqlDbType.NVarChar);

                pPcbQty.Direction = ParameterDirection.Input;

                pPcbQty.Value = PcbQty;

                cmd.Parameters.Add(pPcbQty);

                SqlParameter pReamrk1 = new SqlParameter("Reamrk1", SqlDbType.NVarChar);

                pReamrk1.Direction = ParameterDirection.Input;

                pReamrk1.Value = Reamrk1;

                cmd.Parameters.Add(pReamrk1);

                SqlParameter pReamrk2 = new SqlParameter("Reamrk2", SqlDbType.NVarChar);

                pReamrk2.Direction = ParameterDirection.Input;

                pReamrk2.Value = Reamrk2;

                cmd.Parameters.Add(pReamrk2);

                SqlParameter pReamrk3 = new SqlParameter("Reamrk3", SqlDbType.NVarChar);

                pReamrk3.Direction = ParameterDirection.Input;

                pReamrk3.Value = Reamrk3;

                cmd.Parameters.Add(pReamrk3);


                SqlParameter pI_ReturnMessage = new SqlParameter("I_ReturnMessage", SqlDbType.NVarChar, 20000);

                pI_ReturnMessage.Direction = ParameterDirection.Output;

                cmd.Parameters.Add(pI_ReturnMessage);


                //返回值
                SqlParameter rtn = new SqlParameter("@ReturnValue", SqlDbType.Int);

                rtn.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(rtn);
                System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                string _logStr = "开始执行语句:" + cmd.CommandText.ToString();
                try
                {

                    cmd.ExecuteNonQuery();
                    stopwatch.Stop();
                }
                catch (Exception er)
                {
                    stopwatch.Stop();
                    TimeSpan timespan = stopwatch.Elapsed;
                    dbUtil.WriteLog(_logStr);

                    dbUtil.WriteLog("执行语句结束,耗时：" + timespan.TotalMilliseconds.ToString() + "ms");

                    string I_ReturnMessage = "3" + er.Message.ToString();
                    dbUtil.WriteLog(I_ReturnMessage);
                    throw new Exception(I_ReturnMessage);
                }

                string rtnStr = cmd.Parameters["@ReturnValue"].Value.ToString();

                //if (rtnStr != "0")
                //{
                    string _s = cmd.Parameters["I_ReturnMessage"].Value.ToString();
                    dbUtil.WriteLog("GenerateBarCode返回：" + _s);
                    return _s;
                //}

                //return rtnStr;
            }
        }

        #region 存储过程 返回值 输出参数
        public string GetReturnFromStartProcess(string ProcessName, string MoName, string ProductName, string Operator, string LotSn,string HideLotSN, string StandardPcbQty, string PcbQty, string WorkflowName, 
            string EquipmentNOCode, string IsHBIN, string PMType, string ParentLotSn, string SmtBIN,string CarrierNO)
        {
            
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 300;
                cmd.CommandText = ProcessName;//存储过程名称

                cmd.Connection = GetConnection();

                cmd.CommandType = CommandType.StoredProcedure;


                SqlParameter pMoName = new SqlParameter("MoName", SqlDbType.NVarChar);

                pMoName.Direction = ParameterDirection.Input;

                pMoName.Value = MoName;

                cmd.Parameters.Add(pMoName);

                SqlParameter pCarrierNO = new SqlParameter("CarrierNO", SqlDbType.NVarChar);

                pCarrierNO.Direction = ParameterDirection.Input;

                pCarrierNO.Value = CarrierNO;

                cmd.Parameters.Add(pCarrierNO);


                SqlParameter pProductName = new SqlParameter("ProductName", SqlDbType.NVarChar);

                pProductName.Direction = ParameterDirection.Input;

                pProductName.Value = ProductName;

                cmd.Parameters.Add(pProductName);


                SqlParameter pOperator = new SqlParameter("Operator", SqlDbType.NVarChar);

                pOperator.Direction = ParameterDirection.Input;

                pOperator.Value = Operator;

                cmd.Parameters.Add(pOperator);


                SqlParameter pLotSn = new SqlParameter("LotSn", SqlDbType.NVarChar);

                pLotSn.Direction = ParameterDirection.Input;

                pLotSn.Value = LotSn;

                cmd.Parameters.Add(pLotSn);


                SqlParameter pHideLotSN = new SqlParameter("HideLotSN", SqlDbType.NVarChar);

                pHideLotSN.Direction = ParameterDirection.Input;

                pHideLotSN.Value = HideLotSN;

                cmd.Parameters.Add(pHideLotSN);


                SqlParameter pStandardPcbQty = new SqlParameter("StandardPcbQty", SqlDbType.NVarChar);

                pStandardPcbQty.Direction = ParameterDirection.Input;

                pStandardPcbQty.Value = StandardPcbQty;

                cmd.Parameters.Add(pStandardPcbQty);


                SqlParameter pPcbQty = new SqlParameter("PcbQty", SqlDbType.NVarChar);

                pPcbQty.Direction = ParameterDirection.Input;

                pPcbQty.Value = PcbQty;

                cmd.Parameters.Add(pPcbQty);


                SqlParameter pWorkflowName = new SqlParameter("WorkflowName", SqlDbType.NVarChar);

                pWorkflowName.Direction = ParameterDirection.Input;

                pWorkflowName.Value = WorkflowName;

                cmd.Parameters.Add(pWorkflowName);


                SqlParameter pEquipmentNOCode = new SqlParameter("EquipmentNOCode", SqlDbType.NVarChar);

                pEquipmentNOCode.Direction = ParameterDirection.Input;

                pEquipmentNOCode.Value = EquipmentNOCode;

                cmd.Parameters.Add(pEquipmentNOCode);


                SqlParameter pIsHBIN = new SqlParameter("IsHBIN", SqlDbType.NVarChar);

                pIsHBIN.Direction = ParameterDirection.Input;

                pIsHBIN.Value = IsHBIN;

                cmd.Parameters.Add(pIsHBIN);

                SqlParameter pPMType = new SqlParameter("PMType", SqlDbType.NVarChar);

                pPMType.Direction = ParameterDirection.Input;

                pPMType.Value = PMType;

                cmd.Parameters.Add(pPMType);

                SqlParameter pParentLotSn = new SqlParameter("ParentLotSn", SqlDbType.NVarChar);

                pParentLotSn.Direction = ParameterDirection.Input;

                pParentLotSn.Value = ParentLotSn;

                cmd.Parameters.Add(pParentLotSn);

                SqlParameter pSmtBIN = new SqlParameter("SmtBIN", SqlDbType.NVarChar);

                pSmtBIN.Direction = ParameterDirection.Input;

                pSmtBIN.Value = SmtBIN;

                cmd.Parameters.Add(pSmtBIN);


                SqlParameter pI_ReturnMessage = new SqlParameter("I_ReturnMessage", SqlDbType.NVarChar, 2000);

                pI_ReturnMessage.Direction = ParameterDirection.Output;

                cmd.Parameters.Add(pI_ReturnMessage);


                //返回值
                SqlParameter rtn = new SqlParameter("@ReturnValue", SqlDbType.Int);

                rtn.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(rtn);
                System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                string _logStr = "开始执行语句:" + cmd.CommandText.ToString();
                try
                {
                    
                    cmd.ExecuteNonQuery();
                    stopwatch.Stop();
                }catch(Exception er)
                {
                    stopwatch.Stop();
                    TimeSpan timespan = stopwatch.Elapsed;
                    dbUtil.WriteLog(_logStr);

                    dbUtil.WriteLog("执行语句结束,耗时：" + timespan.TotalMilliseconds.ToString() + "ms");

                    string I_ReturnMessage = "3" + er.Message.ToString();
                    dbUtil.WriteLog(I_ReturnMessage);
                    throw new Exception(I_ReturnMessage);
                }
                

                string rtnStr = cmd.Parameters["@ReturnValue"].Value.ToString();

                if (rtnStr!="0")
                {
                    string _s = cmd.Parameters["I_ReturnMessage"].Value.ToString();
                    dbUtil.WriteLog(_s);
                    return _s;
                }

                return rtnStr;
            }
        }

        public string GetReturnFromStartProcessMiniPob(string ProcessName, string MoName, string ProductName, string Operator, string LotSn, string HideLotSN, string StandardPcbQty, string PcbQty, string WorkflowName,
            string EquipmentNOCode, string IsHBIN, string PMType, string ParentLotSn, string SmtBIN)
        {

            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandTimeout = 300;
                cmd.CommandText = ProcessName;//存储过程名称

                cmd.Connection = GetConnection();

                cmd.CommandType = CommandType.StoredProcedure;


                SqlParameter pMoName = new SqlParameter("MoName", SqlDbType.NVarChar);

                pMoName.Direction = ParameterDirection.Input;

                pMoName.Value = MoName;

                cmd.Parameters.Add(pMoName);


                SqlParameter pProductName = new SqlParameter("ProductName", SqlDbType.NVarChar);

                pProductName.Direction = ParameterDirection.Input;

                pProductName.Value = ProductName;

                cmd.Parameters.Add(pProductName);


                SqlParameter pOperator = new SqlParameter("OperatorNO", SqlDbType.NVarChar);

                pOperator.Direction = ParameterDirection.Input;

                pOperator.Value = Operator;

                cmd.Parameters.Add(pOperator);


                SqlParameter pLotSn = new SqlParameter("LotSn", SqlDbType.NVarChar);

                pLotSn.Direction = ParameterDirection.Input;

                pLotSn.Value = LotSn;

                cmd.Parameters.Add(pLotSn);


                SqlParameter pHideLotSN = new SqlParameter("HideLotSN", SqlDbType.NVarChar);

                pHideLotSN.Direction = ParameterDirection.Input;

                pHideLotSN.Value = HideLotSN;

                cmd.Parameters.Add(pHideLotSN);


                SqlParameter pStandardPcbQty = new SqlParameter("StandardPcbQty", SqlDbType.NVarChar);

                pStandardPcbQty.Direction = ParameterDirection.Input;

                pStandardPcbQty.Value = StandardPcbQty;

                cmd.Parameters.Add(pStandardPcbQty);


                SqlParameter pPcbQty = new SqlParameter("ActualPcbQty", SqlDbType.NVarChar);

                pPcbQty.Direction = ParameterDirection.Input;

                pPcbQty.Value = PcbQty;

                cmd.Parameters.Add(pPcbQty);



                SqlParameter pWorkflowName = new SqlParameter("WorkflowName", SqlDbType.NVarChar);

                pWorkflowName.Direction = ParameterDirection.Input;

                pWorkflowName.Value = WorkflowName;

                cmd.Parameters.Add(pWorkflowName);


                SqlParameter pEquipmentNOCode = new SqlParameter("EquipmentNumber", SqlDbType.NVarChar);

                pEquipmentNOCode.Direction = ParameterDirection.Input;

                pEquipmentNOCode.Value = EquipmentNOCode;

                cmd.Parameters.Add(pEquipmentNOCode);


                SqlParameter pIsHBIN = new SqlParameter("ISMixBin", SqlDbType.NVarChar);

                pIsHBIN.Direction = ParameterDirection.Input;

                pIsHBIN.Value = IsHBIN;

                cmd.Parameters.Add(pIsHBIN);

                SqlParameter pPMType = new SqlParameter("PMType", SqlDbType.NVarChar);

                pPMType.Direction = ParameterDirection.Input;

                pPMType.Value = PMType;

                cmd.Parameters.Add(pPMType);

                SqlParameter pParentLotSn = new SqlParameter("ParentLotSn", SqlDbType.NVarChar);

                pParentLotSn.Direction = ParameterDirection.Input;

                pParentLotSn.Value = ParentLotSn;

                cmd.Parameters.Add(pParentLotSn);

                SqlParameter pSmtBIN = new SqlParameter("SmtBIN", SqlDbType.NVarChar);

                pSmtBIN.Direction = ParameterDirection.Input;

                pSmtBIN.Value = SmtBIN;

                cmd.Parameters.Add(pSmtBIN);


                SqlParameter pI_ReturnMessage = new SqlParameter("I_ReturnMessage", SqlDbType.NVarChar, 2000);

                pI_ReturnMessage.Direction = ParameterDirection.Output;

                cmd.Parameters.Add(pI_ReturnMessage);


                //返回值
                SqlParameter rtn = new SqlParameter("@ReturnValue", SqlDbType.Int);

                rtn.Direction = ParameterDirection.ReturnValue;

                cmd.Parameters.Add(rtn);
                System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                string _logStr = "开始执行语句:" + cmd.CommandText.ToString();
                try
                {

                    cmd.ExecuteNonQuery();
                    stopwatch.Stop();
                }
                catch (Exception er)
                {
                    stopwatch.Stop();
                    TimeSpan timespan = stopwatch.Elapsed;
                    dbUtil.WriteLog(_logStr);

                    dbUtil.WriteLog("执行语句结束,耗时：" + timespan.TotalMilliseconds.ToString() + "ms");

                    string I_ReturnMessage = "3" + er.Message.ToString();
                    dbUtil.WriteLog(I_ReturnMessage);
                    throw new Exception(I_ReturnMessage);
                }


                string rtnStr = cmd.Parameters["@ReturnValue"].Value.ToString();

                if (rtnStr != "0")
                {
                    string _s = cmd.Parameters["I_ReturnMessage"].Value.ToString();
                    dbUtil.WriteLog(_s);
                    return _s;
                }

                return rtnStr;
            }
        }
        #endregion
        public static string GetConnString1(string PreStr)
        {
            string WCFAddress = "";
            string TxtPath = System.AppDomain.CurrentDomain.BaseDirectory + @"WCFAddress.txt";

            if (!File.Exists(TxtPath))
            {
                return "";
            }
            else
            {
                //获取地址
                try
                {
                    using(StreamReader sr = new StreamReader(TxtPath, Encoding.Default))
                    {
                        String line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.ToString().Contains(PreStr)) ;//"WCF:"))
                            {
                                WCFAddress = line.ToString().Replace(PreStr, "").TrimStart().TrimEnd();
                            }

                        }
                        sr.Close();
                        sr.Dispose();
                        return WCFAddress;
                    }
                    
                }
                catch (IOException e)
                {
                    return WCFAddress;
                }
            }


        }

        /// <summary>
        /// 日志记录
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="msg">写入内容</param>
        /// <param name="flag">是否覆盖，true 续写，false 覆盖</param>
        public void WriteLog(string msg, bool flag = true)
        {
            //目录构成方式 log\name\name_******.type
            //Log\ServiceStatusLog\ServiceStatusLog_180423.
            try
            {
                string path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                string name = "Log";
                path = path + "\\" + name;

                DirectoryInfo directory = new DirectoryInfo(path);

                if (!directory.Exists)
                {
                    directory.Create();
                }
                string fileName = name + "_" + DateTime.Now.ToString("yyMMdd") + ".txt";

                fileName = path + "\\" + fileName;

                if (File.Exists(fileName))
                {
                    if (flag)
                    {
                        FileStream filestr = new FileStream(fileName, FileMode.Append, FileAccess.Write,FileShare.Write);
                        StreamWriter writer = new StreamWriter(filestr);
                        writer.WriteLine(DateTime.Now.ToString("yyMMdd hh:mm:ss") + " >>>" + msg);
                        writer.Close();
                    }
                    else
                    {
                        FileStream filestr = new FileStream(fileName, FileMode.Create, FileAccess.Write,FileShare.Write);
                        StreamWriter writer = new StreamWriter(filestr);
                        writer.WriteLine(DateTime.Now.ToString("yyMMdd hh:mm:ss") + " >>>" + msg);
                        writer.Close();
                    }
                }
                else
                {
                    FileStream filestr = new FileStream(fileName, FileMode.Create, FileAccess.Write,FileShare.Write);
                    StreamWriter writer = new StreamWriter(filestr);
                    writer.WriteLine(msg);
                    writer.Close();
                }
            }
            catch (Exception er)
            {
                throw er;
            }
        }
    }
}
