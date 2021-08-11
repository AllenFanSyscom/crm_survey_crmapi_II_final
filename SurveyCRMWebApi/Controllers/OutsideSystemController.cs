using SurveyCRMWebApiV2.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SurveyCRMWebApiV2.Models;
using Common;
using Newtonsoft.Json;
using System.Data;
using SurveyCRMWebApiV2.Utility;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SurveyCRMWebApiV2.Controller
{
	[RoutePrefix("api/system")]
	public class OutsideSystemController : ApiBaseController
	{
		private DBHelper _db;
		private DBHelper _crmDB;
        private bool _isValid;
        private readonly JwtHelpers jwt=new JwtHelpers();

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


        public OutsideSystemController()
        {
            _db = new DBHelper(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MS_Survey"].ConnectionString.Replace("Passingword", "Password"));
			_crmDB = new DBHelper(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MS_CRM"].ConnectionString.Replace("Passingword", "Password"));
            this.jwt = jwt;

        }

        [Route("auth")]
        [HttpPost]
        public HttpResponseMessage auth([FromBody] Object value)
        {
            var replyData = new ReplyData();
            try
            {
                string a = AppSettingsHelper.DefaultConnectionString;
                /* 輸入格式：
                 *{
                 *    "UserId":"99999999-0000-0000-0000-000000000002"             //UserId
                 *}
                 */
                Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(value.ToString());
                if (jo["UserId"] == null)
                {
                    //報告錯誤
                    replyData.code = "-1";
                    replyData.message = $"未傳入參數UserId！";
                    replyData.data = "";
                    Log.Error("SSO登入取得token失敗!" + "未傳入參數UserId！");
                    var result2 = JsonConvert.SerializeObject(replyData);
                    return ReturnJsonBy(result2);
                }


                //string UserId = AppSettingsHelper.EnvSwitchToCRM.SwitchToCRM ?
                //Utility.Common.Base64Decrypt(jo["UserId"].ToString(), new UTF8Encoding()) : jo["UserId"].ToString();

                //SSO傳入的UID，統一要使用BASE64編碼。
                string UserId = Utility.Common.Base64Decrypt(jo["UserId"].ToString());

                Log.Info($"SSO傳入的UID:{UserId}!");

                //1. 檢查UserId是否存在(SSEC001_UserInfo.UserId) 
                if (!Utility.Common.IsUserIdExists(UserId))
                {
                    //從CRM站台跳轉進來的GUID，如果不存在的話，要新增資料到問卷平台
                    Guid crmUserId = Guid.NewGuid();
                    bool isUserIdGuid = Guid.TryParse(UserId, out crmUserId);
                    DataTable dtCrmUser;

                    if (AppSettingsHelper.EnvSwitchToCRM.SwitchToCRM)
                    {
                        dtCrmUser = CRMDbApiHelper.SystemController_GetCRMUserInfo(UserId);
                    }
                    else
                    {
                        dtCrmUser = GetCRMUserInfoBy(UserId);
                    }

                    Log.Error($"dtCrmUser:{dtCrmUser.Rows.Count}!");
                    if (isUserIdGuid && dtCrmUser.Rows.Count > 0)
                    {
                        //Create User
                        DataRow crmRow = dtCrmUser.Rows[0];
                        var crmUserCode = crmRow["UserCode"] == DBNull.Value ? "" : crmRow["UserCode"].ToString().Trim();
                        var crmDeptNo = "";
                        var crmUserName = "";
                        var crmUserFullName = "";
                        var crmTelephone = crmRow["Telephone"] == DBNull.Value ? "" : crmRow["Telephone"].ToString().Trim();
                        var crmEMail = "";
                        var crmStartDate = "SYSDATETIME()";
                        var crmStopDate = " DATEADD(year, 100, SYSDATETIME())";
                        var crmStyleNo = 0;
                        var crmUsedMark = "1";
                        var crmRemark = "SSO跳轉問卷平台添加新User";
                        var crmTypeErrorTime = 0;
                        var insSql = $"INSERT INTO SSEC001_UserInfo (" +
                            " UserId, UserCode, DeptNo, UserName, UserFullName, Telephone, " +
                            " EMail,  StartDate, StopDate, StyleNo, UsedMark," +
                            " Remark, PwdErrorTime, UpdUserId, UpdDateTime ) VALUES (  " +
                            $" @crmUserId, @crmUserCode, @crmDeptNo, @crmUserName, @crmUserFullName,@crmTelephone," +
                            $" @crmEMail, " +
                            $" @crmStartDate," +
                            $" @crmStopDate," +
                            $" @crmStyleNo, @crmUsedMark," +
                            $" @crmRemark, @crmTypeErrorTime, @crmUserId,   SYSDATETIME() )";

                        //-------sql para----start
                        SqlParameter[] sqlParams = new SqlParameter[] {
                            new SqlParameter("@crmUserId", SqlDbType.UniqueIdentifier),
                            new SqlParameter("@crmUserCode", SqlDbType.NVarChar),
                            new SqlParameter("@crmDeptNo", SqlDbType.Char),
                            new SqlParameter("@crmUserName", SqlDbType.NVarChar),
                            new SqlParameter("@crmUserFullName", SqlDbType.NVarChar),
                            new SqlParameter("@crmTelephone", SqlDbType.VarChar),
                            new SqlParameter("@crmEMail", SqlDbType.VarChar),
                            new SqlParameter("@crmStartDate", SqlDbType.DateTime2),
                            new SqlParameter("@crmStopDate", SqlDbType.DateTime2),
                            new SqlParameter("@crmStyleNo", SqlDbType.Int),
                            new SqlParameter("@crmUsedMark", SqlDbType.Bit),
                            new SqlParameter("@crmRemark", SqlDbType.VarChar),
                            new SqlParameter("@crmTypeErrorTime", SqlDbType.Int),
                            new SqlParameter("@crmUserId", SqlDbType.UniqueIdentifier)
                        };
                        sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(crmUserId);
                        sqlParams[1].Value = removeSpecialCharactersPath(crmUserCode);
                        sqlParams[2].Value = crmDeptNo;
                        sqlParams[3].Value = crmUserName;
                        sqlParams[4].Value = crmUserFullName;
                        sqlParams[5].Value = removeSpecialCharactersPath(crmTelephone);
                        sqlParams[6].Value = crmEMail;
                        sqlParams[7].Value = crmStartDate;
                        sqlParams[8].Value = crmStopDate;
                        sqlParams[9].Value = Convert.ToInt32(crmStyleNo);
                        sqlParams[10].Value = Convert.ToBoolean(crmUsedMark);
                        sqlParams[11].Value = crmRemark;
                        sqlParams[12].Value = Convert.ToInt32(crmTypeErrorTime);
                        sqlParams[13].Value = new System.Data.SqlTypes.SqlGuid(crmUserId);
                        //-------sql para----end
                        Log.Info($"insSql 前面:{insSql}!");
                        var insertNum = _db.ExecuteSql(insSql, sqlParams);
                        Log.Info($"問卷平台跳轉SSO傳入Uid{UserId}不存在,需要新增!");
                        Log.Info($"問卷平台跳轉SSO新增Uid Sql {insSql}");
                        //2020/10/12--Allen/Gem: 用戶不存在，新增的時候，也要寫一筆資料到SSEC005_UserRole，RoleId 固定給2，
                        //                        UsedMark = True，StartDate = Date.Now, EndDate = Date.Now+100年
                        var insRole = $" INSERT INTO SSEC005_UserRole " +
                            $" (UserId, [RoleId], UsedMark, StartDate, EndDate, " +
                            "   Remark, UpdUserId, UpdDateTime) VALUES " +
                            $" (@UserId,2, '1',@crmStartDate, @crmStopDate," +
                            $"   '',@crmUserId,  SYSDATETIME() )";

                        SqlParameter[] sqlParam_role = new SqlParameter[] {
                            new SqlParameter("@UserId", SqlDbType.UniqueIdentifier),
                            new SqlParameter("@crmStartDate", SqlDbType.DateTime2),
                            new SqlParameter("@crmStopDate", SqlDbType.DateTime2),
                            new SqlParameter("@crmUserId", SqlDbType.UniqueIdentifier)
                             };

                        sqlParam_role[0].Value = new System.Data.SqlTypes.SqlGuid(UserId);
                        sqlParam_role[1].Value = crmStartDate;
                        sqlParam_role[2].Value = crmStopDate;
                        sqlParam_role[3].Value = new System.Data.SqlTypes.SqlGuid(crmUserId);

                        insertNum = _db.ExecuteSql(insRole, sqlParam_role);
                        Log.Info($"問卷平台跳轉SSO傳入Uid{UserId}不存在,新增UserId後，需要新增SEC005!");
                        Log.Info($"問卷平台跳轉SSO新增SEC005 Sql {insRole}");
                    }
                    else
                    {
                        //不存在
                        // 回傳結果
                        ErrorCode.Code = "101";    //帳號錯誤
                        replyData.code = ErrorCode.Code;
                        replyData.message = ErrorCode.Message;
                        replyData.data = "";
                        Log.Error($"SSO登入取得token失敗！用戶{UserId}不存在！");
                        return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
                    }

                }
                //2.存在則取出UserCode，產生Token: jwt.GenerateToken(UserCode)
                // 回傳結果，結果同OTP驗證
                TokenInfo sso = new TokenInfo();

                var UserCode = "";

                DataTable dtR = GetUserInfoBy(UserId);
                if (dtR.Rows.Count > 0)
                {
                    DataRow row = dtR.Rows[0];
                    UserCode = row["UserCode"] == DBNull.Value ? "" : row["UserCode"].ToString().Trim();
                    sso.UserId = row["UserId"];
                    sso.UserName = row["UserName"];
                    sso.RoleId = row["RoleId"];
                    sso.RoleName = row["RoleName"];
                }
                else
                {
                    ErrorCode.Code = "101";    //帳號錯誤
                    replyData.code = ErrorCode.Code;
                    replyData.message = ErrorCode.Message;
                    replyData.data = "";
                    Log.Error($"SSO登入取得token失敗！用戶{UserId}不存在！");
                    return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
                }
                //目前從SSEC001_UserInfo取得UserName/UserCode/Telephone請改取CHT_MSCRM的SystemUserBase裡面的FullName/EmployeeId/MobilePhone
                DataTable dtCrm;
                if (AppSettingsHelper.EnvSwitchToCRM.SwitchToCRM)
                {
                    dtCrm = CRMDbApiHelper.SystemController_GetCRMUserInfo(sso.UserId.ToString());
                }
                else
                {
                    dtCrm = GetCRMUserInfoBy(sso.UserId.ToString());
                }

                if (dtCrm.Rows.Count > 0)
                {
                    //User info 在CRM裡最新
                    DataRow row = dtCrm.Rows[0];
                    UserCode = row["UserCode"] == DBNull.Value ? "" : row["UserCode"].ToString().Trim();
                    sso.UserId = row["UserId"];
                    sso.UserName = row["UserName"];

                    //從CRM取得用戶訊息，要更新問卷平台的資料
                    var uptUser = $" UPDATE SSEC001_UserInfo SET  UserName = @UserName " +
                            $", Telephone =@Telephone  " +
                            $"   WHERE UserId=@UserId ";

                    SqlParameter[] sqlParam = new SqlParameter[] {
                            new SqlParameter("@UserName", SqlDbType.NVarChar),
                            new SqlParameter("@Telephone", SqlDbType.VarChar),
                            new SqlParameter("@UserId", SqlDbType.UniqueIdentifier),
                             };

                    sqlParam[0].Value = removeSpecialCharactersPath(row["UserName"].ToString());
                    sqlParam[1].Value = removeSpecialCharactersPath(row["Telephone"].ToString());
                    sqlParam[2].Value = new System.Data.SqlTypes.SqlGuid(row["UserId"].ToString());

                    var updateNum = _db.ExecuteSql(uptUser, sqlParam);
                    Log.Info($"更新CRM用戶訊息!" + $"UserName:{row["UserName"]}，Telephone:{row["Telephone"]}，UserId:{row["UserId"]}，筆數:{updateNum.ToString()}");

                }

                //產生Token: jwt.GenerateToken(UserCode)
                var token = jwt.GenerateToken(UserCode);

                sso.Token = token;
                //產生token後，還是需要寫入SYS001
                var Ip = "SSO"; //allen說，此處寫成SSO
                int i = WriteToken2DB(sso.UserId.ToString(), Ip, token);
                replyData.code = "200";
                replyData.message = $"SSO登入取得token成功。";
                replyData.data = sso;
                Log.Info($"SSO登入取得token成功！token='{token}' ");
                var result = JsonConvert.SerializeObject(replyData);
                return ReturnJsonBy(result);
            }
            catch (Exception ex)
            {
                //報告錯誤
                replyData.code = "-1";
                replyData.message = ex.Message;
                replyData.data = null;
                Log.Error("SSO登入取得token失敗！" + ex.Message);
                var result = JsonConvert.SerializeObject(replyData);
                return ReturnJsonBy(result);
            }
        }
        private DataTable GetCRMUserInfoBy(String userId)
        {
            string sSql = $" SELECT SystemUserId AS UserId, FullName AS UserName, SUBSTRING(DomainName,CHARINDEX('\\',DomainName)+1,LEN(DomainName)-CHARINDEX('\\',DomainName)) AS UserCode, MobilePhone AS Telephone " +
                " FROM SystemUserBase " +
                $" WHERE SystemUserId=@userId  ";
                //-------sql para----start
                SqlParameter[] sqlParams = new SqlParameter[] {
                    new SqlParameter("@userId", SqlDbType.UniqueIdentifier)
                };
                sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(userId);
                //-------sql para----end
            try
            {
                Log.Info("SSO登入取得token, GetCRMUserInfoBy sql:" + sSql);
                return _crmDB.GetQueryData(sSql, sqlParams);
            }
            catch (Exception ex)
            {
                Log.Error("SSO登入取得token: GetUserInfoBy fail！" + ex.Message);
                Log.Error(ex.StackTrace);
                throw ex;
            }
        }
        private DataTable GetUserInfoBy(String userId)
        {


            Guid resultId;
            var isGuid = Guid.TryParse(userId, out resultId);

            string sSql = "";
            if (!isGuid)
            {
                sSql += $" SELECT TOP 1  A.UserId, A.UserName, A.UserCode, B.RoleId, C.RoleName " +
                " FROM SSEC001_UserInfo A " +
                " LEFT JOIN SSEC005_UserRole B ON B.UserId = A.UserId AND B.UsedMark = '1' " +
                " LEFT JOIN SSEC004_RoleId C ON C.RoleId = B.RoleId AND C.UsedMark = '1' " +
                $" WHERE A.UserCode=@UserCode AND A.UsedMark = '1' ";
            }
            else
            {
                sSql += $" SELECT TOP 1  A.UserId, A.UserName, A.UserCode, B.RoleId, C.RoleName " +
                " FROM SSEC001_UserInfo A " +
                " LEFT JOIN SSEC005_UserRole B ON B.UserId = A.UserId AND B.UsedMark = '1' " +
                " LEFT JOIN SSEC004_RoleId C ON C.RoleId = B.RoleId AND C.UsedMark = '1' " +
                $" WHERE A.UserId=@UserId AND A.UsedMark = '1' ";
            }
            //-------sql para----start
            SqlParameter[] sqlParams = new SqlParameter[] {
                new SqlParameter("@UserCode", SqlDbType.VarChar),
                new SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
            };
            sqlParams[0].Value = new System.Data.SqlTypes.SqlChars(GetUserCodeBy(userId));
            sqlParams[1].Value = new System.Data.SqlTypes.SqlGuid(userId);
            //-------sql para----end
            try
            {
                Log.Info("SSO登入取得token, GetUserInfoBy sql:" + sSql);
                return _db.GetQueryData(sSql, sqlParams);
            }
            catch (Exception ex)
            {
                Log.Error("SSO登入取得token GetUserInfoBy fail！" + ex.Message);
                Log.Error(ex.StackTrace);
                throw ex;
            }
        }
        /// <summary>
        /// 依UserId取得UserCode
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>UserCode</returns>
        private String GetUserCodeBy(String userId)
        {
            string sSql = $"SELECT UserCode FROM SSEC001_UserInfo WHERE UserId=@UserId AND UsedMark='1' ";
            //-------sql para----start
            SqlParameter[] sqlParams = new SqlParameter[] {
                new SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
            };
            sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(userId);
            //-------sql para----end
            try
            {
                DataTable dtR = _db.GetQueryData(sSql, sqlParams);
                if (dtR.Rows.Count > 0)
                {
                    return dtR.Rows[0]["UserCode"] == DBNull.Value ? "" : dtR.Rows[0]["UserCode"].ToString().Trim();
                }
                return "";
            }
            catch (Exception ex)
            {
                Log.Error("SSO登入取得token:" + ex.StackTrace);
                Log.Error("SSO登入取得token:" + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 將生成的Token寫入SYS001
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private int WriteToken2DB(String userId, string ip, string token)
        {
            int i = -1;
            try
            {
                //傳入的這個token格式不對，寫不到db，所以用GUID即可，SYS001僅僅為了去LastLoginDateTime，所以，其他欄位沒關係
                token = Guid.NewGuid().ToString("D");

                var UserId = "";
                var UserName = "";
                var sSql1 = $"SELECT UserId, UserName FROM SSEC001_UserInfo WHERE UserId=@UserId ";
                //-------sql para----start
                SqlParameter[] sqlParams = new SqlParameter[] {
                    new SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
                };
                sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(userId);
                //-------sql para----end
                DataTable dtR;
                if (AppSettingsHelper.EnvSwitchToCRM.SwitchToCRM)
                {
                    dtR = CRMDbApiHelper.SystemController_GetCRMUserInfo(userId);
                }
                else
                {
                    dtR = _db.GetQueryData(sSql1, sqlParams);
                }
                if (dtR.Rows.Count > 0)
                {
                    DataRow dr = dtR.Rows[0];
                    UserId = dr["UserId"].ToString();
                    UserName = dr["UserName"].ToString();
                }
                //User Info在CRM DB為最新
                var sSql = "";

                sSql = " INSERT INTO SYS001_SystemToken (Token, UserId, UserName, Ip, ExpiredDate, UpdDate) VALUES " +
                        $"(@token, @UserId,@UserName,@ip,SYSDATETIME(),SYSDATETIME())";

                //-------sql para----start
                SqlParameter[] sqlParam_sys001 = new SqlParameter[] {
                    new SqlParameter("@token", SqlDbType.UniqueIdentifier),
                    new SqlParameter("@UserId", SqlDbType.UniqueIdentifier),
                    new SqlParameter("@UserName", SqlDbType.NVarChar),
                    new SqlParameter("@ip", SqlDbType.VarChar)
                };
                sqlParam_sys001[0].Value = token;
                sqlParam_sys001[1].Value = new System.Data.SqlTypes.SqlGuid(UserId);
                sqlParam_sys001[2].Value = removeSpecialCharactersPath(UserName);
                sqlParam_sys001[3].Value = ip;

                //-------sql para----end

                i = _db.ExecuteSql(sSql, sqlParam_sys001);
                return i;
            }
            catch (Exception ex)
            {
                Log.Error("SSO Write Token fail！" + ex.Message);
                Log.Error(ex.StackTrace);
                throw ex;
            }
        }


    }
    public class TokenInfo
    {
        /// <summary>
        /// Token
        /// </summary>
        public String Token { get; set; }
        /// <summary>
        /// User Id
        /// </summary>
        public Object UserId { get; set; }
        /// <summary>
        /// User Name
        /// </summary>
        public Object UserName { get; set; }
        /// <summary>
        /// User's Role Id
        /// </summary>
        public Object RoleId { get; set; }
        /// <summary>
        /// User's Role Name
        /// </summary>
        public Object RoleName { get; set; }


        public Object UserCode { get; set; }
    }
}

		