using SurveyCRMWebApiV2.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SurveyCRMWebApiV2.Models;
using Common;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SurveyCRMWebApiV2.Controller
{
    [RoutePrefix("api/Survey/Info")]
    public class OutsideSurveyInfoController : ApiBaseController
	{
        private DBHelper _db;
        public static string removeSpecialCharactersPath(string str)
        {
            string returnvalue = "";
            string pattern = "([0-9]|[A-Z]|[a-z]|[]|\\d|\\s|[+,-\\\\.*()_\"'|:<>@!#$%^&={}]|[\u4e00-\u9fa5])";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection a = regex.Matches(str);
            for (int i = 0; i < a.Count; i++)
            {
                returnvalue += a[i].Value.ToString();
            }
            return returnvalue;
        }

        public OutsideSurveyInfoController()
        {
            _db = new DBHelper(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MS_Survey"].ConnectionString.Replace("Passingword", "Password"));

        }

        #region 新增一個問卷
        /// <summary>
        /// 新增一個問卷
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Route("Insert")]
        [HttpPost]
        public HttpResponseMessage Insert([FromBody] Object value)
        {
             Log.Info("api/Survey/Info/Insert Start！");
            //"Newtonsoft.Json.Linq.JArray"
            //"Newtonsoft.Json.Linq.Newtonsoft.Json.Linq.JObject"
            //多筆資料的話，此處需要處理，暫不管
            //if(value.GetType().Name=="JArray"){}
            Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(value.ToString());
            var replyData = new ReplyData();
            var result = "";
            Log.Info("api/Survey/Info/Insert Start2！");
            try
            {

                Log.Info("api/Survey/Info/Insert Start3！");
                //UserId 必須有?
                if (jo["Uid"] == null || String.IsNullOrWhiteSpace(jo["Uid"].ToString()))
                {
                    //報告錯誤
                    replyData.code = "-1";
                    replyData.message = $"新增一個問卷失敗！參數Uid不能為空！";
                    replyData.data = "";
                    Log.Error("新增問卷失敗!" + "參數Uid不能為空！");
                     result = JsonConvert.SerializeObject(replyData); 
                    return ReturnJsonBy(replyData);
                }

                //獲取操作員資訊
                Log.Info("api/Survey/Info/Insert Start4！");
                var key = User.Identity.Name;
                if (key == null)
                {
                    //報告錯誤
                    replyData.code = "-1";
                    replyData.message = $"用戶不存在！";
                    replyData.data = "";
                    Log.Error("發送OTP失敗!" + "用戶不存在！");
                     result = JsonConvert.SerializeObject(replyData);
                    return ReturnJsonBy(replyData);
                }

                Log.Info("api/Survey/Info/Insert Start5！");
                var info = Utility.Common.GetConnectionInfo(jo["Uid"].ToString());
                Log.Info("api/Survey/Info/Insert Start5-1！");
                if (info == null)
                {
                    //報告錯誤
                    replyData.code = "-1";
                    replyData.message = $"用戶不存在！";
                    replyData.data = "";
                    Log.Error("發送OTP失敗!" + "用戶不存在！");
                    result = JsonConvert.SerializeObject(replyData); 
                    return ReturnJsonBy(replyData);
                }
                var UpdUserId = info.UserId;
                //UpdUserId 會有程式依據Token取得，所以,目前暫時寫成00000000-0000-0000-0000-000000000000
                //var UpdUserId = "00000000-0000-0000-0000-000000000000";
                //UpdDateTime datetime2 yyyy-MM-dd HH:mm:ss.fffffff"
                //var UpdDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                //Check UserId是否存在不存在需要新增
                var UserId = jo["Uid"];
                var insSql = "";
                var insRole = "";

                Log.Info("api/Survey/Info/Insert Start6！");
                var SurveyId = jo["SurveyId"];
                    if (jo["SurveyId"] == null)
                    {
                        //據Allen講, DB中所有Id欄位,都是GUID產生, 那這是client 產好傳過來還是後台產生????????
                        /* 註解一下GUID：GUID，Globally Unique Identifier ,全局唯一標識，
                         * C#產生時，有下列4種格式：
                         * 格式 xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 每個x表0-9或者a-f的十六進制
                         * string guid1 = Guid.NewGuid().ToString("N"); d468954e22a145f8806ae41fb938e79e
                         * string guid2 = Guid.NewGuid().ToString("D"); c05d1709-0361-4304-8b2c-58fadcc4ae08
                         * string guid3 = Guid.NewGuid().ToString("P"); (d3a300a7-144d-4587-9e22-3a7699013f01)
                         * string guid4 = Guid.NewGuid().ToString("B"); {3351ca09-5302-400a-aea8-2a8be6c12b06}
                         * SQL Server 的 NEWID()產生的格式 c05d1709-0361-4304-8b2c-58fadcc4ae08 和C# D參數產生的一致。
                         */
                        SurveyId = Guid.NewGuid().ToString();

                        ////報告錯誤
                        //replyData.code = "-1";
                        //replyData.message = $"新增大量選項失敗！參數SurveyId不能為空！";
                        //replyData.data = "";
                        //Log.LogFile("新增大量選項失敗!" + "參數SurveyId不能為空！");
                        //return JsonConvert.SerializeObject(replyData);
                    }
                    var list = new List<KeyValuePair<string, SqlParameter[]>> ();

                    var Title = jo["Title"] == null ? "" : jo["Title"].ToString();
                    string sSql = $"INSERT INTO QUE001_QuestionnaireBase (" +
                    " SurveyId, Title, FinalUrl, ThankWords, DueAction," +
                    " DelFlag,  Audit, CreateUserId, CreateDateTime, UpdUserId," +
                    " UpdDateTime ) VALUES (  " +
                    $" @SurveyId, @Title, '','',0," +
                    $" '0', '0',@UserId, SYSDATETIME(), @UpdUserId," +
                    $" SYSDATETIME())";  //UpdDateTime 為datetime2: yyyy-MM-dd HH:mm:ss.ffffffff
                    Log.Info("新增一個問卷 " + sSql);

                    //-------sql para----start
                    SqlParameter[] sqlParams = new SqlParameter[] {
                        new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
                        new SqlParameter("@Title", SqlDbType.NVarChar),
                        new SqlParameter("@UserId", SqlDbType.UniqueIdentifier),
                        new SqlParameter("@UpdUserId", SqlDbType.UniqueIdentifier)
                    };
                    sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId.ToString());
                    sqlParams[1].Value = removeSpecialCharactersPath(Title);
                    sqlParams[2].Value = new System.Data.SqlTypes.SqlGuid(UserId.ToString());
                    sqlParams[3].Value = new System.Data.SqlTypes.SqlGuid(UpdUserId);
                //-------sql para----end

                var obj = new KeyValuePair<string, SqlParameter[]>(sSql, sqlParams);
                list.Add(obj);


                int iR = _db.ExecuteSqlTran(list);


                    //*******新增問卷時候，要一併新增結束頁資訊(QUE007) by Allen.20201005
                    var EndPagePic = AppSettingsHelper.EndPageDefault.EndPagePic.ToString();
                    var EndPageStyle = AppSettingsHelper.EndPageDefault.EndPageStyle.ToString();
                    var ButtonSentence = AppSettingsHelper.EndPageDefault.ButtonSentence.ToString();
                    var EnableRedirect = AppSettingsHelper.EndPageDefault.EnableRedirect.ToString();
                    var RedirectUrl = AppSettingsHelper.EndPageDefault.RedirectUrl.ToString();
                    //開始新增
                    sSql = " INSERT INTO QUE007_QuestionnaireEndPage " +
                        " (SurveyId, EndPagePic, EndPageStyle, ButtonSentence, EnableRedirect, " +
                        " RedirectUrl, UpdUserId, UpdDateTime ) ";

                    var vSql = $" VALUES(@SurveyId , @EndPagePic, @EndPageStyle ,@ButtonSentence ,@EnableRedirect," +
                            $" @RedirectUrl , @UpdUserId, SYSDATETIME())";
                    sSql = string.Concat(sSql, vSql);
                    Log.Info($"設計問卷 - 結束頁 - 新增,sql={sSql}");

                    //-------sql para----start
                    SqlParameter[] sqlParamsA = new SqlParameter[] {
                        new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
                        new SqlParameter("@EndPagePic", SqlDbType.NVarChar),
                        new SqlParameter("@EndPageStyle", SqlDbType.NVarChar),
                        new SqlParameter("@ButtonSentence", SqlDbType.NVarChar),
                        new SqlParameter("@EnableRedirect", SqlDbType.Bit),
                        new SqlParameter("@RedirectUrl", SqlDbType.NVarChar),
                        new SqlParameter("@UpdUserId", SqlDbType.UniqueIdentifier)
                    };
                    sqlParamsA[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId.ToString());
                    sqlParamsA[1].Value = EndPagePic;
                    sqlParamsA[2].Value = EndPageStyle;
                    sqlParamsA[3].Value = ButtonSentence;
                    sqlParamsA[4].Value = Convert.ToBoolean(EnableRedirect);
                    sqlParamsA[5].Value = RedirectUrl;
                    sqlParamsA[6].Value = new System.Data.SqlTypes.SqlGuid(UpdUserId);
                    //-------sql para----end

                    iR = _db.ExecuteSql(sSql, sqlParamsA);
                    //**********End

                    replyData.code = "200";
                    replyData.message = $"新增記錄完成。";
                    replyData.data = SurveyId;// ExecuteQuery($"SELECT * FROM QUE001_QuestionnaireBase WHERE SurveyId='{SurveyId}' ");

                     result = JsonConvert.SerializeObject(replyData); 

                    return ReturnJsonBy(result);

            }
            catch (Exception ex)
            {
                replyData.code = "-1";
                replyData.message = $"新增問卷失敗！{ex.Message}.";
                replyData.data = "";
                Log.Error("新增問卷失敗!" + ex.Message);
                 result = JsonConvert.SerializeObject(replyData); 
                return ReturnErrorJsonBy(result);
            }
           

        }
        /// <summary>
        /// 傳入UserId是否已存在
        /// </summary>
        /// <param name="UserId"></param>
        /// <returns></returns>
        private bool IsUserIdExists(object UserId)
        {
            string sSql = $"SELECT COUNT(1) FROM SSEC001_UserInfo WHERE UserId=@UserId";
            //-------sql para----start
            SqlParameter[] sqlParams = new SqlParameter[] {
                new SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
            };
            sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(UserId.ToString());
            //-------sql para----end
            try
            {
                var result = _db.GetSingle(sSql, sqlParams);
                if (string.IsNullOrEmpty(result) || result == "0")
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion 


    }
}
