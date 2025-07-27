using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.IO;
using System.Text;

namespace OrbitMes_3DTest
{
    public static class HelperClass
    {
        /// <summary>
        /// 测试连接地址
        /// </summary>
        /// <param name="WCFAddress">WCFAddress</param>
        /// <returns></returns>
        public static bool TestConnectMES(string WCFAddress)
        {
            string Sql = @"select 1";
            OrBitADCService.ADCService ADC = new OrBitADCService.ADCService();
            DataSet ds = new DataSet();
            ds = ADC.GetDataSetWithSQLString(WCFAddress, Sql);
            if (ds == null || ds.Tables.Count <= 0 || ds.Tables[0].Rows.Count <= 0)
            {
                return false;
            }
            return true;
        }
        
        public static string CheckLotSNResultToXML(string I_ReturnMessage, string TestResult)
        {
            string ReturnResult = "";
            ReturnResult = @"<?xml version=""1.0"" encoding=""utf-8""?> ";
            ReturnResult = ReturnResult + @" <Root> ";
            ReturnResult = ReturnResult + " <I_ReturnMessage> " + I_ReturnMessage + @"</I_ReturnMessage>";
            ReturnResult = ReturnResult + " <TestResult> " + TestResult + @"</TestResult>";
            ReturnResult = ReturnResult + " </Root>";
            return ReturnResult;

        }
    }
}