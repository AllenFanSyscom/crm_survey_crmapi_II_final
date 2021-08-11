using SurveyCRMWebApiV2.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SurveyCRMWebApiV2.Models;
using System.Data;
using Common;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace SurveyCRMWebApiV2.Controller
{


	
	[RoutePrefix("api/Survey/Question/CollectionWay")]
	public class OutsideSurveyQuesionCollectionWayController : ApiBaseController
	{


		private DBHelper _db;
		public OutsideSurveyQuesionCollectionWayController()
		{
			_db = new DBHelper(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MS_Survey"].ConnectionString.Replace("Passingword", "Password"));
			
		}

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



		/// <summary>
		/// 收集回覆-查詢
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[Route("Query")]
		[HttpGet]
		public HttpResponseMessage QueryBy(string SurveyId)
		{
			ReplyData replyData = new ReplyData();
			List<QUE009_QuestionnaireProvideType> lstDataInfo = new List<QUE009_QuestionnaireProvideType>();

			//-------sql para----start
			List<SqlParameter> sqlParams = new List<SqlParameter>();
			//-------sql para----end

			string sSql = "SELECT * FROM QUE009_QuestionnaireProvideType ";
			string sWhereCondition = " WHERE 1=1 ";
			if (!String.IsNullOrWhiteSpace(SurveyId))
			{
				sWhereCondition += $" AND SurveyId=@SurveyId ";
				//-------sql para----start
                var obj = new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier);
                obj.Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
                sqlParams.Add(obj);
                //-------sql para----end
			}

			sSql += sWhereCondition;
			Log.Info("收集回覆-查詢:" + sSql);
			try
			{
				lstDataInfo = ExecuteQuery(sSql, sqlParams.ToArray());
				replyData.code = "200";
				replyData.message = $"查詢記錄完成。共{lstDataInfo.Count}筆。";
				replyData.data = lstDataInfo;// JsonConvert.SerializeObject(lstUserInfo);

				var result = JsonConvert.SerializeObject(replyData);

				return ReturnJsonBy(result);
			}
			catch (Exception ex)
			{
				replyData.code = "-1";
				replyData.message = $"查詢記錄失敗！{ex.Message}.";
				replyData.data = "";
				Log.Error("收集回覆-查詢失敗!" + ex.Message);
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnErrorJsonBy(result);
			}


		}


		private List<QUE009_QuestionnaireProvideType> ExecuteQuery(String sSql)
		{
			List<QUE009_QuestionnaireProvideType> lstDataInfo = new List<QUE009_QuestionnaireProvideType>();
			try
			{
				DataTable dtR = _db.GetQueryData(sSql);
				foreach (DataRow dr in dtR.Rows)
				{
					QUE009_QuestionnaireProvideType datainfo = new QUE009_QuestionnaireProvideType();
					datainfo.SurveyId = dr["SurveyId"];
					datainfo.ProvideType = dr["ProvideType"];
					datainfo.FinalUrl = dr["FinalUrl"];
					datainfo.ReplyMaxNum = dr["ReplyMaxNum"];
					datainfo.MultiProvideType = dr["MultiProvideType"];
					datainfo.ValidRegister = dr["ValidRegister"];
					datainfo.FullEndFlag = dr["FullEndFlag"];
					datainfo.CreateUserId = dr["CreateUserId"];
					datainfo.CreateDateTime = dr["CreateDateTime"];
					datainfo.UpdUserId = dr["UpdUserId"];
					datainfo.UpdDateTime = dr["UpdDateTime"];
					datainfo.TestUrl = dr["TestUrl"];
					datainfo.ValidField = dr["ValidField"];
					//填寫次數ReplyNum：依據SurveyId+ProvideType count（1）from QUE021
					datainfo.ReplyNum = GetReplyNumBy(datainfo.SurveyId.ToString(), Convert.ToInt32(datainfo.ProvideType));
					lstDataInfo.Add(datainfo);
				}
			}
			catch (Exception)
			{
				
				throw;
			}
			return lstDataInfo;
		}

		private List<QUE009_QuestionnaireProvideType> ExecuteQuery(String sSql, SqlParameter[] cmdParams)
		{
			List<QUE009_QuestionnaireProvideType> lstDataInfo = new List<QUE009_QuestionnaireProvideType>();
			try
			{
				DataTable dtR = _db.GetQueryData(sSql, cmdParams);
				foreach (DataRow dr in dtR.Rows)
				{
					QUE009_QuestionnaireProvideType datainfo = new QUE009_QuestionnaireProvideType();
					datainfo.SurveyId = dr["SurveyId"];
					datainfo.ProvideType = dr["ProvideType"];
					datainfo.FinalUrl = dr["FinalUrl"];
					datainfo.ReplyMaxNum = dr["ReplyMaxNum"];
					datainfo.MultiProvideType = dr["MultiProvideType"];
					datainfo.ValidRegister = dr["ValidRegister"];
					datainfo.FullEndFlag = dr["FullEndFlag"];
					datainfo.CreateUserId = dr["CreateUserId"];
					datainfo.CreateDateTime = dr["CreateDateTime"];
					datainfo.UpdUserId = dr["UpdUserId"];
					datainfo.UpdDateTime = dr["UpdDateTime"];
					datainfo.TestUrl = dr["TestUrl"];
					datainfo.ValidField = dr["ValidField"];
					//填寫次數ReplyNum：依據SurveyId+ProvideType count（1）from QUE021
					datainfo.ReplyNum = GetReplyNumBy(datainfo.SurveyId.ToString(), Convert.ToInt32(datainfo.ProvideType));
					lstDataInfo.Add(datainfo);
				}
			}
			catch (Exception)
			{
				
				throw;
			}
			return lstDataInfo;
		}

		private int GetReplyNumBy(String SurveyId, int ProvideType)
		{
			var sSql = $" SELECT COUNT(1) FROM QUE021_AnwserCollection WHERE SurveyId=@SurveyId AND ProvideType=@ProvideType " +
					   $" AND Env = 2 AND (DelFlag IS NULL OR DelFlag<>1) "; // 20201021_填寫次數只計算正式環境Env=2 & 在QUE021取得資料時，都要加上判斷DelFlag的條件。

			//-------sql para----start
			SqlParameter[] sqlParams = new SqlParameter[] {
				new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
				new SqlParameter("@ProvideType", SqlDbType.Int)
			};
			sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
			sqlParams[1].Value = removeSpecialCharactersPath(ProvideType.ToString());
			//-------sql para----end
			try
			{
				var result = _db.GetSingle(sSql, sqlParams);
				if (String.IsNullOrWhiteSpace(result))
					return 0;
				else
					return Convert.ToInt32(result);
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				Log.Error(ex.StackTrace);
				throw;
			}
		}

		/// <summary>
		/// 收集回覆-陳核
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[Route("Proposal")]
		[HttpPut]
		public HttpResponseMessage Proposal([FromBody] Object value)
		{
			//依據SurveyId修改QUE001.Audit
			////"Newtonsoft.Json.Linq.JArray"
			////"Newtonsoft.Json.Linq.Newtonsoft.Json.Linq.JObject"
			////多筆資料的話，此處需要處理，暫不管
			////if(value.GetType().Name=="JArray")
			////{
			////    foreach (var val in value as JArray)
			////    {
			////        InsertOne(val);
			////    }
			////}
			Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(value.ToString());
			var replyData = new ReplyData();

			//SurveyId 必須有?
			if (jo["SurveyId"] == null || String.IsNullOrWhiteSpace(jo["SurveyId"].ToString()))
			{
				/* 註解一下GUID：GUID，Globally Unique Identifier ,全局唯一標識，
                 * C#產生時，有下列4種格式：
                 * 格式 xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 每個x表0-9或者a-f的十六進制
                 * string guid1 = Guid.NewGuid().ToString("N"); d468954e22a145f8806ae41fb938e79e
                 * string guid2 = Guid.NewGuid().ToString("D"); c05d1709-0361-4304-8b2c-58fadcc4ae08
                 * string guid3 = Guid.NewGuid().ToString("P"); (d3a300a7-144d-4587-9e22-3a7699013f01)
                 * string guid4 = Guid.NewGuid().ToString("B"); {3351ca09-5302-400a-aea8-2a8be6c12b06}
                 * SQL Server 的 NEWID()產生的格式 c05d1709-0361-4304-8b2c-58fadcc4ae08 和C# D參數產生的一致。
                 */
				// var uuid = Guid.NewGuid().ToString();

				//報告錯誤
				replyData.code = "-1";
				replyData.message = $"陳核失敗！參數SurveyId不能為空！";
				replyData.data = "";
				Log.Error("陳核失敗!" + "參數SurveyId不能為空！");
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnJsonBy(result);
			}
			var SurveyId = jo["SurveyId"].ToString();



			//SurveyId 必須有?
			if (jo["UserId"] == null || String.IsNullOrWhiteSpace(jo["UserId"].ToString()))
			{
				/* 註解一下GUID：GUID，Globally Unique Identifier ,全局唯一標識，
                 * C#產生時，有下列4種格式：
                 * 格式 xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 每個x表0-9或者a-f的十六進制
                 * string guid1 = Guid.NewGuid().ToString("N"); d468954e22a145f8806ae41fb938e79e
                 * string guid2 = Guid.NewGuid().ToString("D"); c05d1709-0361-4304-8b2c-58fadcc4ae08
                 * string guid3 = Guid.NewGuid().ToString("P"); (d3a300a7-144d-4587-9e22-3a7699013f01)
                 * string guid4 = Guid.NewGuid().ToString("B"); {3351ca09-5302-400a-aea8-2a8be6c12b06}
                 * SQL Server 的 NEWID()產生的格式 c05d1709-0361-4304-8b2c-58fadcc4ae08 和C# D參數產生的一致。
                 */
				// var uuid = Guid.NewGuid().ToString();

				//報告錯誤
				replyData.code = "-1";
				replyData.message = $"陳核失敗！參數UserId不能為空！";
				replyData.data = "";
				Log.Error("陳核失敗!" + "參數UserId不能為空！");
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnJsonBy(result);
			}
			var UserId = jo["UserId"].ToString();
			Log.Info("陳核 uid!" + UserId);

			//獲取操作員資訊
			var key = UserId;
			if (key == null)
			{
				//報告錯誤
				replyData.code = "-1";
				replyData.message = $"用戶不存在！";
				replyData.data = "";
				Log.Error("發送OTP失敗!" + "用戶不存在！");
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnJsonBy(result);
			}
			var info = Utility.Common.GetConnectionInfo(key);
			if (info == null)
			{
				//報告錯誤
				replyData.code = "-1";
				replyData.message = $"用戶不存在！";
				replyData.data = "";
				Log.Error("發送OTP失敗!" + "用戶不存在！");
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnJsonBy(result);
			}
			var UpdUserId = info.UserId;

			//UpdUserId 會有程式依據Token取得，所以,目前暫時寫成00000000-0000-0000-0000-000000000000
			//var UpdUserId = jo["UpdUserId"] == null ? "00000000-0000-0000-0000-000000000000" : jo["UpdUserId"].ToString();
			//var UpdUserId = "00000000-0000-0000-0000-000000000000";
			//UpdDateTime 為datetime2: yyyy-MM-dd HH:mm:ss.ffffffff
			//var UpdDateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff");
			string sSql = $"UPDATE QUE001_QuestionnaireBase " +
				$" SET Audit=1 , UpdUserId=@UpdUserId, UpdDateTime=SYSDATETIME() " +
				$" WHERE SurveyId=@SurveyId";
			//-------sql para----start
			SqlParameter[] sqlParams = new SqlParameter[] {
				new SqlParameter("@UpdUserId", SqlDbType.UniqueIdentifier),
				new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier)
			};
			sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(UpdUserId);
			sqlParams[1].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
			//-------sql para----end
			Log.Error("收集回覆-陳核:" + sSql);
			try
			{
				int iR = _db.ExecuteSql(sSql, sqlParams);

				replyData.code = "200";
				replyData.message = $"陳核完成。";

				replyData.data = iR;
				var result = JsonConvert.SerializeObject(replyData); 
				return ReturnJsonBy(result);
			}
			catch (Exception ex)
			{
				replyData.code = "-1";
				replyData.message = $"陳核失敗！{ex.Message}.";
				replyData.data = "";
				Log.Error("陳核失敗!" + ex.Message);
				var result = JsonConvert.SerializeObject(replyData); 
				return ReturnErrorJsonBy(result);
			}
			

		}

		[Route("Reject")]
		[HttpPut]
		public HttpResponseMessage Reject([FromBody] Object value)
		{
			Newtonsoft.Json.Linq.JObject jo = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(value.ToString());
			var replyData = new ReplyData();

			//SurveyId 必須有?
			if (jo["SurveyId"] == null)
			{
				//報告錯誤
				replyData.code = "-1";
				replyData.message = $"參數SurveyId不能為空！";
				replyData.data = "";
				Log.Error("參數SurveyId不能為空！");
				var result = JsonConvert.SerializeObject(replyData); 
				return ReturnJsonBy(result);
			}
			var SurveyId = jo["SurveyId"].ToString();



			//SurveyId 必須有?
			if (jo["UserId"] == null)
			{
				//報告錯誤
				replyData.code = "-1";
				replyData.message = $"參數UserId不能為空！";
				replyData.data = "";
				Log.Error("參數UserId不能為空！");
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnJsonBy(result);
			}
			var UserId = jo["UserId"].ToString();


			var key = UserId;
			var info = Utility.Common.GetConnectionInfo(key);
			if (info == null)
			{
				//報告錯誤
				replyData.code = "-1";
				replyData.message = "用戶不存在!";
				replyData.data = "";
				var result = JsonConvert.SerializeObject(replyData); 
				return ReturnJsonBy(result);
			}
			var userId = info.UserId;
			string sSql = "UPDATE QUE001_QuestionnaireBase SET Audit='0',UpdUserId=@UpdUserId,UpdDateTime=SYSDATETIME() where SurveyId=@SurveyId";
			//-------sql para----start
			SqlParameter[] sqlParams = new SqlParameter[] {
				new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
				new SqlParameter("@UpdUserId", SqlDbType.UniqueIdentifier)
			};
			sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
			sqlParams[1].Value = new System.Data.SqlTypes.SqlGuid(userId);
			//-------sql para----end
			
			Log.Info("收集回覆-退回:" + sSql);
			try
			{
				int iR = _db.ExecuteSql(sSql, sqlParams);

				replyData.code = "200";
				replyData.message = $"退回記錄完成。";
				replyData.data = iR;
				var result = JsonConvert.SerializeObject(replyData); 
				return ReturnJsonBy(result);
			}
			catch (Exception ex)
			{
				replyData.code = "-1";
				replyData.message = $"退回記錄失敗！{ex.Message}.";
				replyData.data = "";
				Log.Error("退回記錄失敗!" + ex.Message);
				var result = JsonConvert.SerializeObject(replyData);
				return ReturnErrorJsonBy(result);
			}
			
		}

	}
}

