﻿using SurveyCRMWebApiV2.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SurveyCRMWebApiV2.Models;
using Common;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using NPOI.SS.UserModel;
using System.Data;
using SurveyCRMWebApiV2.Utility;
using System.IO;
using System.Text;
using System.Net.Http.Headers;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SurveyCRMWebApiV2.Controller
{
    
    [RoutePrefix("api/Analytics")]
    public class AnalyticsController : ApiBaseController
    {
        /// <summary>
        /// 問卷系統後臺--成效分析
        /// </summary>
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

        public AnalyticsController()
        {
            _db = new DBHelper(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["MS_Survey"].ConnectionString.Replace("Passingword", "Password"));
            
        }

        #region "數據顯示查詢"

        private int GetTotalReplyNumBy(String surveyId, String env)
        {
            try
            {
                //成效分析的兩個API，要過濾QUE021的delflag  true不要取
                var sSql = "select COUNT(1) AS TotalReplyNum from QUE021_AnwserCollection " +
                    $"WHERE SurveyId = @surveyId AND Env = @env AND (DelFlag IS NULL OR DelFlag<>1) ";
                //-------sql para----start
                SqlParameter[] sqlParams = new SqlParameter[] {
                    new SqlParameter("@surveyId", SqlDbType.UniqueIdentifier),
                    new SqlParameter("@env", SqlDbType.Int)
                };
                sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(surveyId);
                sqlParams[1].Value = Convert.ToInt32(removeSpecialCharactersPath(env));
                //-------sql para----end
                var result = _db.GetSingle(sSql, sqlParams);
                return result == "" ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Log.Error("成效分析-數據顯示查詢失敗!" + ex.Message);

                throw ex;
            }
        }


        /// <summary>
        /// 將QuestionSubject中所有標籤text:後面的文字取出並串接起來
        /// </summary>
        /// <param name="QuestionSubject"></param>
        /// <returns></returns>
        private Object GetQuestionSubjectTextContent(Object QuestionSubject)
        {
            if (QuestionSubject == null || QuestionSubject == DBNull.Value)
                return QuestionSubject;
            if (QuestionSubject.ToString().Trim().Length == 0)
                return "";
            //DB裡存nvarchar,類似{"text":"我們","color":"#0fff"},{"text":"他們"}  所以 以"text:"分割
            //var splitStr = String.Concat("\"", "text", "\"", ":");
            string[] splitStr = { "\"", "text", "\"", ":" };
            var arrStr = QuestionSubject.ToString().Split(splitStr, StringSplitOptions.RemoveEmptyEntries);
            if (arrStr.Length < 2)
                return QuestionSubject; //沒有"text:"字眼，返回原subject
            var fullStr = "";
            //從第一個text後開始
            for (int i = 1; i < arrStr.Length; i++)
            {
                try
                {
                    //有可能有 {"text":"我們","color":"#0fff"} 或者 {"text":"他們"} 所以，哪個在先，認為text後的內容以它結束
                    var endpos = (arrStr[i].IndexOf(',') > -1 && arrStr[i].IndexOf(',') < arrStr[i].IndexOf('}')) ? arrStr[i].IndexOf(',') : arrStr[i].IndexOf('}');
                    var tmp = "";
                    if (endpos < 0)  //沒有,或者}作為text後內容的結束，直接取到結尾
                        tmp = arrStr[i].Substring(0).Replace("\"", "");
                    else
                        tmp = arrStr[i].Substring(0, endpos).Replace("\"", "");
                    //然後把本筆QestionSubject的文字內容串起來
                    fullStr += tmp;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }
            }
            return fullStr;
        }

        #endregion


        #region 數據下載
        /// <summary>
        /// 成效分析-數據下載(csv)
        /// </summary>
        /// <param name="SurveyId"></param>
        /// <returns></returns>
        [Route("ReportCSV")]
        [HttpGet]
        public HttpResponseMessage ReportCSV(String SurveyId, string token) //, String Env)
        {
            ReplyData replyData = new ReplyData();
            if (String.IsNullOrWhiteSpace(SurveyId))
            {
                //報告錯誤
                replyData.code = "-1";
                replyData.message = $"參數SurveyId不能為空！";
                Log.Error("成效分析-數據下載!" + "參數SurveyId不能為空！");
                var result = JsonConvert.SerializeObject(replyData);
                return ReturnJsonBy(result);
            }
            //if (String.IsNullOrWhiteSpace(Env))
            //{
            //    //報告錯誤
            //    replyData.code = "-1";
            //    replyData.message = $"參數Env不能為空！";
            //    Log.LogFile("成效分析-數據下載!" + "參數Env不能為空！");
            //    return JsonConvert.SerializeObject(replyData);
            //}

            var Env = "1";
            Survey4ReportCsv survey = new Survey4ReportCsv(); //SurveyId
            //題型中文
            var CodeCode = "0100";
            string sSql = ""; //$"SELECT * FROM GEN004_AllCode WHERE CodeCode='{CodeCode}' Order by Cast(CodeSubCode as int) ";
            var Title = "";
            try
            {
                //如果QUE021沒有資料請回傳無填答資訊?
                sSql = " SELECT DISTINCT A.SurveyId, B.Title FROM QUE021_AnwserCollection A " +
                    " INNER JOIN QUE001_QuestionnaireBase B ON B.SurveyId=A.SurveyId " +
                    $" WHERE A.SurveyId=@surveyId AND (A.DelFlag IS NULL OR A.DelFlag<>1) and A.Env=@env";  //成效分析的兩個API，要過濾QUE021的delflag  true不要取
                //-------sql para----start
                SqlParameter[] sqlParams = new SqlParameter[] {
                    new SqlParameter("@surveyId", SqlDbType.UniqueIdentifier),
                    new SqlParameter("@env", SqlDbType.Int)
                };
                sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
                sqlParams[1].Value = Convert.ToInt32(Env);
                //-------sql para----end
                DataTable dt = _db.GetQueryData(sSql, sqlParams);
                if (dt.Rows.Count < 1)
                {
                    //報告錯誤
                    ErrorCode.Code = "501";
                    replyData.code = ErrorCode.Code;
                    replyData.message = ErrorCode.Message;
                    //replyData.code = "-1";
                    //replyData.message = $"問卷{SurveyId}無填答資訊！";
                    Log.Info("成效分析-數據下載!" + "無填答資訊！");
                    
                    return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
                }
                Title = dt.Rows[0]["Title"].ToString().Trim();

                sSql = $"SELECT * FROM GEN004_AllCode WHERE CodeCode=@CodeCode Order by Cast(CodeSubCode as int) ";
                //-------sql para----start
                SqlParameter[] sqlParamsA = new SqlParameter[] {
                    new SqlParameter("@CodeCode", SqlDbType.Char)
                };
                sqlParamsA[0].Value = CodeCode;
                //-------sql para----end
                DataTable dtCode = _db.GetQueryData(sSql, sqlParamsA);
                if (dtCode.Rows.Count < 1)
                {
                    //報告錯誤
                    replyData.code = "-1";
                    replyData.message = $"GEN004_AllCode未設定題型參數[{CodeCode}]！";
                    Log.Info("成效分析-數據下載!" + $"GEN004_AllCode未設定題型參數[{CodeCode}]！");
                    return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
                }
                // TotalReplyNum =問卷填寫數量：統計QUE_21數量BY SurveyID
                survey.SurveyId = SurveyId;
                survey.Title = Title;

                //處理一般題型
                List<Question4ReportCsv> lstQuestion = new List<Question4ReportCsv>();   //QuestionId
                foreach (DataRow drCode in dtCode.Rows)
                {
                    //只處理單選題:1、多選題:2、填空題:3、矩陣題:4、基本資料:5
                    var QuestionType = Convert.ToInt32(drCode["CodeSubCode"]);
                    if (QuestionType == 1 || QuestionType == 2 || QuestionType == 3 || QuestionType == 5)
                    {
                        //題目
                        var normal = ProcessCommonQuestion4ReportCsv(SurveyId, QuestionType);
                        if (normal.Count > 0)
                            lstQuestion.AddRange(normal);
                    }
                    else if (QuestionType == 4)  //矩陣題
                    {
                        //處理矩陣題型
                        var matrix = ProcessMatrixQuestion4ReportCsv(SurveyId, QuestionType);
                        if (matrix.Count > 0)
                            lstQuestion.AddRange(matrix);
                    }
                    else
                        continue;
                }

                List<Question4ReportCsv> lstQuestionReal = lstQuestion.OrderBy(x => Convert.ToInt32(x.QuestionSeq)).ToList<Question4ReportCsv>();
                //有Question才設定Survey.QuestionList， 否則 Survey.QuestionList=null
                if (lstQuestionReal.Count > 0)
                    survey.QuestionList = lstQuestionReal;
                //再加答題部分
                List<Reply4ReportCsv> lstReply = new List<Reply4ReportCsv>();   //QuestionId
                lstReply = GetReplyData4ReportCsv(SurveyId, Env);
                if (lstReply.Count > 0)
                    survey.ReplyList = lstReply;

                replyData.code = "200";
                replyData.message = $"查詢記錄完成。";
                replyData.data = survey;
                var result = JsonConvert.SerializeObject(replyData);
                return ReturnJsonBy(result);
            }
            catch (Exception ex)
            {
                replyData.code = "-1";
                replyData.message = $"成效分析-數據下載失敗！{ex.Message}.";
                replyData.data = "";
                Log.Error("成效分析-數據下載查詢失敗!" + ex.Message);
                var result = JsonConvert.SerializeObject(replyData); 
                return ReturnJsonBy(result);
            }

        }

       
        /// <summary>
        /// 查詢 矩陣題:4
        /// </summary>
        /// <param name="SurveyId"></param>
        /// <param name="Env"></param>
        private List<Question4ReportCsv> ProcessMatrixQuestion4ReportCsv(String SurveyId, int questionType)
        {
            //資料結構為 surveyId->QuestionList->OptionList
            List<Question4ReportCsv> lstQuestion = new List<Question4ReportCsv>();   //QuestionId
            Question4ReportCsv question = new Question4ReportCsv();

            List<Option4ReportCsv> lstOption = new List<Option4ReportCsv>();   //OptionId
            Option4ReportCsv option = new Option4ReportCsv();
            //非矩陣題不處理
            if (questionType != 4)
                return lstQuestion;
            //MatrixItems 是多個MatrixFields串接起來用;隔開，所以, 先拆開成單個的MatrixField，故一筆可能會變成多筆
            string QUE002_Table = "";

            // allen說： Gem {{hostip}}/api/Analytics/ReportCSV 這個API有誤，需要去掉OptionList內的MatrixField改在QuestionList新增MatrixItems
            // 因為要直接取MatrixItems, 所以，這裡不用去拆分了,QUE002_Table=QUE002_QuestionnaireDetail
            //QUE002_Table = "( " +
            //             "   SELECT SurveyId, QuestionId, QuestionSeq, QuestionType, IsRequired, QuestionSubject, MatrixItems, " +
            //             "          BlankMaxLimit, BlankMinLimit, QuestionImage, QuestionVideo, MultiOptionLimit," +
            //             "          MatrixField " +
            //             "   FROM(SELECT SurveyId, QuestionId, QuestionSeq, QuestionType, IsRequired, QuestionSubject, MatrixItems, " +
            //             "               BlankMaxLimit, BlankMinLimit, QuestionImage, QuestionVideo, MultiOptionLimit," +
            //             "               [value] = CONVERT(XML, '<v>' + REPLACE(MatrixItems, ';', '</v><v>') + '</v>') " +
            //             "        FROM  QUE002_QuestionnaireDetail " +
            //             $"       WHERE SurveyId = '{SurveyId}' AND QuestionType = {questionType} " +
            //             $"      ) A " +
            //             "   OUTER APPLY(SELECT  MatrixField = N.v.value('.', 'varchar(100)') " +
            //             "               FROM    A.[value].nodes('/v') N(v)" +
            //             "              ) B " +
            //             ")";
            QUE002_Table = " QUE002_QuestionnaireDetail ";
            //查詢資料
            string sSql = "SELECT A.SurveyId, " +
                " B.QuestionId, B.QuestionSeq, B.QuestionType, B.IsRequired, B.QuestionSubject, B.MatrixItems, " +
                " B.BlankMaxLimit, B.BlankMinLimit, B.QuestionImage, B.QuestionVideo, B.MultiOptionLimit, " +
                " C.*, F.CodeSubName AS TypeContent  FROM QUE001_QuestionnaireBase A" +
                    " LEFT JOIN " + QUE002_Table + " B " +
                    " ON B.SurveyId=A.SurveyId " +
                    " LEFT JOIN QUE003_QuestionnaireOptions C ON C.QuestionId=B.QuestionId " +
                    " LEFT JOIN GEN004_AllCode F ON F.CodeSubCode=B.QuestionType AND F.CodeCode='0100'" +
                    $" WHERE A.SurveyId=@SurveyId AND B.QuestionType=@QuestionType " +
                    " ORDER BY B.QuestionSeq, C.OptionSeq ";

            //-------sql para----start
            SqlParameter[] sqlParams = new SqlParameter[] {
                new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
                new SqlParameter("@QuestionType", SqlDbType.Int)
            };
            sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
            sqlParams[1].Value = Convert.ToInt32(removeSpecialCharactersPath(questionType.ToString()));
            //-------sql para----end

            try
            {
                DataTable dtR = _db.GetQueryData(sSql, sqlParams);
                foreach (DataRow dr in dtR.Rows)
                {
                    option = new Option4ReportCsv();
                    if (question.QuestionId == null || (question.QuestionId.ToString() != dr["QuestionId"].ToString())) //同一個questionId處理完
                    {
                        if (question.QuestionId != null)       //不是第一筆資料, 把前面的加入
                        {
                            //當有option時，Question.OptionList才顯示，否則，顯示null
                            if (lstOption.Count > 0)
                                question.OptionList = lstOption;  //Question不同了, 則將Option list 給上一個QuestionId
                            //當question有值才加
                            if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                                lstQuestion.Add(question);            //上一個QuestionId完成,加入QuestionId list裡
                        }
                        lstOption = new List<Option4ReportCsv>();       //clear optionId list
                        question = new Question4ReportCsv();            //init questionId
                                                                        //開始賦值給新的QuestionId各欄位(因一個QuestionId 可能對應多個OptionId, QuestionId只在不同時set value一次即可
                        question.QuestionId = dr["QuestionId"];
                        question.QuestionSeq = dr["QuestionSeq"];
                        question.QuestionType = dr["QuestionType"];
                        question.TypeContent = dr["TypeContent"];
                        question.QuestionSubject = GetQuestionSubjectTextContent(dr["QuestionSubject"]);
                        question.IsRequired = dr["IsRequired"];
                        question.MatrixItems = dr["MatrixItems"];
                        //question.BlankMaxLimit = dr["BlankMaxLimit"];
                        //question.BlankMinLimit = dr["BlankMinLimit"];
                        //question.QuestionImage = dr["QuestionImage"];
                        //question.QuestionVideo = dr["QuestionVideo"];
                        //question.MultiOptionLimit = dr["MultiOptionLimit"];
                        //if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                        //    question.QuestionReplyNum = GetQuestionReplyNumBy(question.QuestionId.ToString(), Convert.ToInt32(Env));
                        //else
                        //    question.QuestionReplyNum = 0;
                        //QD
                    }
                    //每一筆的OptionId 都不同,紀錄下來加入 optionId list
                    option.OptionId = dr["OptionId"];
                    option.OptionSeq = dr["OptionSeq"];
                    option.OptionContent = dr["OptionContent"];
                    option.OtherFlag = dr["OtherFlag"];
                    //option.OptionImage = dr["OptionImage"];
                    //option.OptionVideo = dr["OptionVideo"];
                    //option.MatrixField = dr["MatrixField"];
                    //if (option.OptionId != null && option.OptionId != DBNull.Value)
                    //    option.OptionReplyNum = GetOptionReplyNumBy(option.OptionId.ToString(), Convert.ToInt32(Env));
                    //else
                    //    option.OptionReplyNum = 0;
                    if (option.OptionId != null && option.OptionId != DBNull.Value)  //如果沒有，不要加
                        lstOption.Add(option);
                }
                if (dtR.Rows.Count > 0)
                {
                    //最後一筆加入
                    //如果Option才加，否則，就顯示null?
                    if (lstOption.Count > 0)
                        question.OptionList = lstOption;
                    //只加不為null的question
                    if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                        lstQuestion.Add(question);
                }
                return lstQuestion;
            }
            catch (Exception ex)
            {
                Log.Error("成效分析-數據顯示查詢失敗!" + ex.Message);
                throw ex;
            }
        }
        /// <summary>
        /// 查詢 單選題:1、多選題:2、填空題:3、基本資料:5
        /// </summary>
        /// <param name="SurveyId"></param>
        /// <param name="Env"></param>
        /// <param name="questionType"></param>
        /// <returns></returns>
        private List<Question4ReportCsv> ProcessCommonQuestion4ReportCsv(String SurveyId, int questionType)
        {
            //資料結構為 surveyId->QuestionList->OptionList
            List<Question4ReportCsv> lstQuestion = new List<Question4ReportCsv>();   //QuestionId
            Question4ReportCsv question = new Question4ReportCsv();

            List<Option4ReportCsv> lstOption = new List<Option4ReportCsv>();   //OptionId
            Option4ReportCsv option = new Option4ReportCsv();
            //處理非矩陣題
            string sSql = "SELECT A.SurveyId, B.*, C.*,F.CodeSubName AS TypeContent FROM QUE001_QuestionnaireBase A" +
                    " LEFT JOIN QUE002_QuestionnaireDetail B ON B.SurveyId=A.SurveyId " +
                    " LEFT JOIN QUE003_QuestionnaireOptions C ON C.QuestionId=B.QuestionId " +
                    " LEFT JOIN GEN004_AllCode F ON F.CodeSubCode=B.QuestionType AND F.CodeCode='0100'" +
                    $" WHERE A.SurveyId=@SurveyId AND B.QuestionType=@QuestionType " +
                    " ORDER BY B.QuestionSeq, C.OptionSeq ";

            //-------sql para----start
            SqlParameter[] sqlParams = new SqlParameter[] {
                new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
                new SqlParameter("@QuestionType", SqlDbType.Int)
            };
            sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
            sqlParams[1].Value = Convert.ToInt32(removeSpecialCharactersPath(questionType.ToString()));

            //-------sql para----end 
            try
            {
                //查詢資料
                DataTable dtR = _db.GetQueryData(sSql, sqlParams);
                foreach (DataRow dr in dtR.Rows)
                {
                    option = new Option4ReportCsv();
                    if (question.QuestionId == null || (question.QuestionId.ToString() != dr["QuestionId"].ToString())) //同一個questionId處理完
                    {
                        if (question.QuestionId != null)      //不是第一筆資料, 把前面的加入
                        {
                            //當有option時，Question.OptionList才顯示，否則，顯示null
                            if (lstOption.Count > 0)
                                question.OptionList = lstOption;  //Question不同了, 則將Option list 給上一個QuestionId
                            //當question有值才加
                            if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                                lstQuestion.Add(question);            //上一個QuestionId完成,加入QuestionId list裡
                        }
                        lstOption = new List<Option4ReportCsv>();       //clear optionId list
                        question = new Question4ReportCsv();            //init questionId
                                                                        //開始賦值給新的QuestionId各欄位(因一個QuestionId 可能對應多個OptionId, QuestionId只在不同時set value一次即可
                        question.QuestionId = dr["QuestionId"];
                        question.QuestionSeq = dr["QuestionSeq"];
                        question.QuestionType = dr["QuestionType"];
                        question.TypeContent = dr["TypeContent"];
                        question.QuestionSubject = GetQuestionSubjectTextContent(dr["QuestionSubject"]);
                        question.IsRequired = dr["IsRequired"];
                        question.BaseDataValidType = dr["BaseDataValidType"];
                        question.MatrixItems = dr["MatrixItems"];  //其他題應該沒有MatrixItems
                        question.HasOther = dr["HasOther"];
                        question.OtherIsShowText = dr["OtherIsShowText"];
                        //question.BlankMaxLimit = dr["BlankMaxLimit"];
                        //question.BlankMinLimit = dr["BlankMinLimit"];
                        //question.QuestionImage = dr["QuestionImage"];
                        //question.QuestionVideo = dr["QuestionVideo"];
                        //question.MultiOptionLimit = dr["MultiOptionLimit"];
                        //if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                        //    question.QuestionReplyNum = GetQuestionReplyNumBy(question.QuestionId.ToString(), Convert.ToInt32(Env));
                        //else
                        //    question.QuestionReplyNum = 0;
                        //QD
                    }
                    //每一筆的OptionId 都不同,紀錄下來加入 optionId list
                    option.OptionId = dr["OptionId"];
                    option.OptionSeq = dr["OptionSeq"];
                    option.OptionContent = dr["OptionContent"];
                    option.OtherFlag = dr["OtherFlag"];
                    //option.OptionImage = dr["OptionImage"];
                    //option.OptionVideo = dr["OptionVideo"];
                    //if (option.OptionId != null && option.OptionId != DBNull.Value)
                    //    option.OptionReplyNum = GetOptionReplyNumBy(option.OptionId.ToString(), Convert.ToInt32(Env));
                    //else
                    //    option.OptionReplyNum = 0;
                    if (option.OptionId != null && option.OptionId != DBNull.Value)  //如果沒有，不要加
                        lstOption.Add(option);
                }
                if (dtR.Rows.Count > 0)
                {
                    //最後一筆加入
                    //如果Option才加，否則，就顯示null?
                    if (lstOption.Count > 0)
                        question.OptionList = lstOption;
                    //只加不為null的question
                    if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                        lstQuestion.Add(question);
                }
                return lstQuestion;
            }
            catch (Exception ex)
            {
                Log.Error("成效分析-數據下載失敗!" + ex.Message);
                throw ex;
            }
        }


        private List<Reply4ReportCsv> GetReplyData4ReportCsv(String SurveyId, String Env)
        {
            //資料結構為 surveyId->QuestionList->OptionList
            List<Reply4ReportCsv> lstReply = new List<Reply4ReportCsv>();   //ReplyId
            Reply4ReportCsv reply = new Reply4ReportCsv();

            List<Answer4ReportCsv> lstAnswer = new List<Answer4ReportCsv>();   //OptionId
            Answer4ReportCsv answer = new Answer4ReportCsv();
            //
            string OrderTbl = " (SELECT ROW_NUMBER() OVER (PARTITION BY SurveyId ORDER BY  SubmitTime ) Seq ,* " +
                $" FROM QUE021_AnwserCollection  WHERE SurveyId=SurveyId AND (DelFlag IS NULL OR DelFlag<>1) AND Env=@Env )";  //成效分析的兩個API，要過濾QUE021的delflag true不要取

            string sSql = "SELECT  A.*, B.id, B.QuestionId, B.OptionId, B.MatrixField, B.BlankAnwer, C.MatrixItems, D.OptionSeq, D.OtherFlag FROM " + OrderTbl + " A " +
                " LEFT JOIN QUE022_AnwserCollectionDetail B ON B.ReplyKey = A.ReplyKey " +
                " LEFT JOIN QUE002_QuestionnaireDetail C ON C.QuestionId = B.QuestionId AND C.SurveyId = A.SurveyId " +
                " LEFT JOIN QUE003_QuestionnaireOptions D ON D.QuestionId = B.QuestionId AND D.OptionId = B.OptionId " +
                $" WHERE A.SurveyId = @SurveyId ORDER BY A.Seq";

            //-------sql para----start
            SqlParameter[] sqlParams = new SqlParameter[] {
                new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
                new SqlParameter("@Env", SqlDbType.Int)
            };
            sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
            sqlParams[1].Value = Convert.ToInt32(Env);
            //-------sql para----end 
            try
            {
                //查詢資料
                DataTable dtR = _db.GetQueryData(sSql, sqlParams);
                foreach (DataRow dr in dtR.Rows)
                {
                    answer = new Answer4ReportCsv();
                    if (reply.ReplyKey == null || (reply.ReplyKey.ToString() != dr["ReplyKey"].ToString())) //同一個ReplyId處理完
                    {
                        if (reply.ReplyKey != null)      //不是第一筆資料, 把前面的加入
                        {
                            //當有 answer 時，reply.AnswerList才顯示，否則，顯示null
                            if (lstAnswer.Count > 0)
                                reply.AnswerList = lstAnswer;  //ReplyId不同了, 則將answer list 給上一個ReplyId
                            //當Reply有值才加
                            if (reply.ReplyKey != null && reply.ReplyKey != DBNull.Value)
                                lstReply.Add(reply);            //上一個QuestionId完成,加入QuestionId list裡
                        }
                        lstAnswer = new List<Answer4ReportCsv>();       //clear answerId list
                        reply = new Reply4ReportCsv();            //init questionId
                                                                  //開始賦值給新的QuestionId各欄位(因一個QuestionId 可能對應多個OptionId, QuestionId只在不同時set value一次即可
                        reply.Seq = dr["Seq"];
                        reply.ReplyKey = dr["ReplyKey"];
                        reply.ProvideType = dr["ProvideType"];
                        reply.ExtenField = dr["ExtenField"];
                        reply.VerifyInfo = dr["VerifyInfo"];
                        reply.ParameterInfo = dr["ParameterInfo"];
                        reply.Device = GetDeviceJugement(dr["Device"].ToString());
                        reply.ForceEnd = dr["ForceEnd"];
                        reply.TimePeriod = dr["TimePeriod"];
                        reply.SubmitTime = dr["SubmitTime"];
                        //if (question.QuestionId != null && question.QuestionId != DBNull.Value)
                        //    question.QuestionReplyNum = GetQuestionReplyNumBy(question.QuestionId.ToString(), Convert.ToInt32(Env));
                        //else
                        //    question.QuestionReplyNum = 0;
                        //QD
                    }
                    //每一筆的answer id 都不同,紀錄下來加入 answer list
                    answer.id = dr["id"];
                    answer.QuestionId = dr["QuestionId"];
                    answer.OptionId = dr["OptionId"];
                    answer.MatrixField = dr["MatrixField"];
                    answer.BlankAnwer = dr["BlankAnwer"];
                    answer.ReplyKey = dr["ReplyKey"];
                    answer.OptionSeq = dr["OptionSeq"];
                    answer.OtherFlag = dr["OtherFlag"];
                    //option.OptionImage = dr["OptionImage"];
                    //option.OptionVideo = dr["OptionVideo"];
                    //if (option.OptionId != null && option.OptionId != DBNull.Value)
                    //    option.OptionReplyNum = GetOptionReplyNumBy(option.OptionId.ToString(), Convert.ToInt32(Env));
                    //else
                    //    option.OptionReplyNum = 0;
                    if (answer.id != null && answer.id != DBNull.Value)  //如果沒有，不要加
                        lstAnswer.Add(answer);
                }
                if (dtR.Rows.Count > 0)
                {
                    //最後一筆加入
                    //如果Option才加，否則，就顯示null?
                    if (lstAnswer.Count > 0)
                        reply.AnswerList = lstAnswer;
                    //只加不為null的question
                    if (reply.ReplyKey != null && reply.ReplyKey != DBNull.Value)
                        lstReply.Add(reply);
                }
                return lstReply;
            }
            catch (Exception ex)
            {
                Log.Error("成效分析-數據下載失敗!" + ex.Message);
                throw ex;
            }
        }

        private string GetDeviceJugement(string Device)
        {
            string[] DeviceLists = new string[] { "Windows NT", "Macintosh", "Android", "iPhone", "iPad", "iPod" };
            string DeviceName = "其他";
            foreach (string name in DeviceLists)
            {
                if (Device.IndexOf(name, 0) > -1)
                {
                    DeviceName = name;
                    break;
                }
            }
            return DeviceName;
        }

        #endregion

        #region 下載
        protected string Decrypt(string str, string encryptKey, string encryptIV)
        {
            try
            {
                //byte[] key = System.Text.Encoding.Unicode.GetBytes(encryptKey);
                //byte[] data = Convert.FromBase64String(str);

                //System.Security.Cryptography.DESCryptoServiceProvider descsp = new System.Security.Cryptography.DESCryptoServiceProvider();
                //System.IO.MemoryStream MStream = new System.IO.MemoryStream();

                //System.Security.Cryptography.CryptoStream CStream = new System.Security.Cryptography.CryptoStream(MStream, descsp.CreateDecryptor(key, key), System.Security.Cryptography.CryptoStreamMode.Write);
                //CStream.Write(data, 0, data.Length);
                //CStream.FlushFinalBlock();
                //byte[] temp = MStream.ToArray();
                //CStream.Close();
                //MStream.Close();
                //return System.Text.Encoding.Unicode.GetString(temp);//返回解密后的字符串
                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(encryptKey);
                byte[] IVArray = UTF8Encoding.UTF8.GetBytes(encryptIV);
                byte[] data = UTF8Encoding.UTF8.GetBytes(str);

                using (RijndaelManaged rDel = new RijndaelManaged())
                {
                    rDel.Key = keyArray;
                    rDel.IV = IVArray;
                    //rDel.Mode = CipherMode.ECB;
                    //rDel.Padding = PaddingMode.PKCS7;
                    ICryptoTransform cTransform = rDel.CreateDecryptor(keyArray, IVArray);
                    byte[] resultArray = cTransform.TransformFinalBlock(data, 0, data.Length);
                    return UTF8Encoding.UTF8.GetString(resultArray);
                }
            }
            catch
            {
                return str;
            }
        }
        [AllowAnonymous]
        [Route("ExportSurvey")]
        [HttpGet]
        //[FileDownload]
        //[ServiceFilter(typeof(FileDownloadAttribute))]
        public HttpResponseMessage ExportSurvey(string SurveyId, string token)
        {
            try
            {
                Log.Info("ExportSurvey Start:" + SurveyId);
                string userId = Decrypt(token, AppSettingsHelper.DownloadKey, AppSettingsHelper.DownloadIV);
                var info = Utility.Common.GetConnectionInfo(userId);
                int queNo = 1; //匯出問卷時使用的題號,不使用DB裡面的題號
                if (info == null)
                {
                    //報告錯誤
                    Log.Info("發送OTP失敗!" + "用戶不存在！");
                    throw new Exception("用戶不存在!");
                }

                Survey4ReportCsv excelData = PrepareExcelData(SurveyId);
                string title = (excelData == null) ? "" : excelData.Title.ToString();

                string fileName = title + "_" + DateTime.Now.ToString("yyyyMMdd");
                //var rootPath = _hostingEnvironment.ContentRootPath + "/ExportSurvey/";
                var rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data") + "/ExportSurvey/";
                if (System.IO.Directory.Exists(rootPath) == false)
                    System.IO.Directory.CreateDirectory(rootPath);
                var newFile = rootPath + fileName + ".xlsx";
                if (System.IO.File.Exists(Utility.Common.removeSpecialCharactersPath(newFile)))
                {
                    System.IO.File.Delete(Utility.Common.removeSpecialCharactersPath(newFile));
                }

                string sheetName = title;
                string headerName = string.Format("行銷活動方式：{0}", title);
                using (var fs = new FileStream(Utility.Common.removeSpecialCharactersPath(newFile), FileMode.Create, FileAccess.Write))
                {
                    //HSSFWorkbook workbook = new HSSFWorkbook();
                    //HSSFSheet sheet = (HSSFSheet)workbook.CreateSheet(sheetName);
                    XSSFWorkbook workbook = new XSSFWorkbook();
                    XSSFSheet sheet = (XSSFSheet)workbook.CreateSheet(sheetName);

                    sheet.DefaultColumnWidth = 16;

                    NPOI.SS.UserModel.IFont font = workbook.CreateFont();
                    font.FontHeightInPoints = 12;
                    font.FontName = "新細明體";


                    int rowIndex = 0;
                    int cellIndex = 0;

                    var header0 = sheet.CreateRow(rowIndex);
                    rowIndex++;
                    header0.CreateCell(0).SetCellValue(headerName);
                    header0.GetCell(0).CellStyle.SetFont(font);
                    var header1 = sheet.CreateRow(rowIndex);
                    rowIndex++;
                    header0.CreateCell(0).SetCellValue("");
                    if (excelData.QuestionList != null && excelData.QuestionList.Count > 0)
                    {
                        var datarow2 = sheet.CreateRow(rowIndex);
                        rowIndex++;
                        var datarow3 = sheet.CreateRow(rowIndex);
                        rowIndex++;
                        //var datarow4 = sheet.CreateRow(rowIndex);//ID Row
                        //rowIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("填答序號");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 100);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        //datarow4.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("問卷登入方式-資格驗證-HN號碼");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 250);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        //datarow4.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("問卷登入方式-參數傳遞");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 200);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        //datarow4.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        foreach (var questionS in excelData.QuestionList)
                        {
                            var cellValue = queNo.ToString() + " " + questionS.QuestionSubject + "(" + questionS.TypeContent + ")";
                            queNo++;
                            datarow2.CreateCell(cellIndex).SetCellValue(cellValue);
                            datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                            //datarow4.CreateCell(cellIndex).SetCellValue(questionS.QuestionId.ToString());
                            if (Convert.ToInt32(questionS.QuestionType) == 3 || Convert.ToInt32(questionS.QuestionType) == 5)
                            {
                                datarow3.CreateCell(cellIndex).SetCellValue("");
                                cellIndex++;
                            }
                            else if (Convert.ToInt32(questionS.QuestionType) == 4)
                            {
                                if (!string.IsNullOrEmpty(questionS.MatrixItems.ToString()))
                                {
                                    string[] fields = questionS.MatrixItems.ToString().Split(';');
                                    foreach (string field in fields)
                                    {
                                        if (string.IsNullOrEmpty(field.Trim()))
                                            continue;
                                        datarow3.CreateCell(cellIndex).SetCellValue(field);
                                        datarow3.GetCell(cellIndex).CellStyle.SetFont(font);
                                        cellIndex++;
                                    }
                                }
                            }
                            else
                            {
                                if (questionS.OptionList != null && questionS.OptionList.Count > 0)
                                {
                                    switch (questionS.QuestionType)
                                    {
                                        case 1://單選題
                                            datarow3.CreateCell(cellIndex).SetCellValue("選項編號");
                                            datarow3.GetCell(cellIndex).CellStyle.SetFont(font);
                                            cellIndex++;
                                            if ((bool)questionS.HasOther && (bool)questionS.OtherIsShowText)
                                            {
                                                datarow3.CreateCell(cellIndex).SetCellValue("其他選項內容");
                                                datarow3.GetCell(cellIndex).CellStyle.SetFont(font);
                                                cellIndex++;
                                            }

                                            break;
                                        case 2://多選題

                                            //foreach (var optionS in questionS.OptionList)
                                            //{
                                            //    if (optionS.OtherFlag == null || string.IsNullOrEmpty(optionS.OtherFlag.ToString().Trim()) || !(bool)optionS.OtherFlag)
                                            //    {
                                            //        datarow3.CreateCell(cellIndex).SetCellValue(optionS.OptionSeq.ToString() + optionS.OptionContent.ToString());
                                            //        datarow3.GetCell(cellIndex).CellStyle.SetFont(font);
                                            //        cellIndex++;
                                            //    }
                                            //}
                                            foreach (var optionS in questionS.OptionList)
                                            {

                                                datarow3.CreateCell(cellIndex).SetCellValue(optionS.OptionSeq.ToString() + optionS.OptionContent.ToString());
                                                datarow3.GetCell(cellIndex).CellStyle.SetFont(font);
                                                cellIndex++;

                                            }
                                            if ((bool)questionS.HasOther && (bool)questionS.OtherIsShowText)
                                            {
                                                datarow3.CreateCell(cellIndex).SetCellValue("其他選項內容");
                                                datarow3.GetCell(cellIndex).CellStyle.SetFont(font);
                                                cellIndex++;
                                            }

                                            break;
                                        case 3://填空題
                                        case 5://基本資料題
                                            datarow3.CreateCell(cellIndex).SetCellValue("");
                                            cellIndex++;
                                            break;
                                        case 4://矩陣題
                                            break;
                                    }

                                }
                            }
                        }
                        datarow2.CreateCell(cellIndex).SetCellValue("作答時間");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 100);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("送出時間");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 150);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("回覆管道");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 100);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("無效問卷");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 100);
                        datarow3.CreateCell(cellIndex).SetCellValue("");
                        cellIndex++;
                        datarow2.CreateCell(cellIndex).SetCellValue("裝置");
                        datarow2.GetCell(cellIndex).CellStyle.SetFont(font);
                        //sheet.SetColumnWidth(cellIndex, 24 * 100);
                        datarow3.CreateCell(cellIndex).SetCellValue("");

                        for (int i = 1; i <= cellIndex; i++)
                        {
                            header0.CreateCell(i);
                            header1.CreateCell(i);
                        }
                        CellRangeAddress region = new CellRangeAddress(0, 0, 0, cellIndex);
                        header0.GetCell(0).SetCellValue(headerName);
                        sheet.AddMergedRegion(region);

                        if (excelData.ReplyList != null && excelData.ReplyList.Count > 0)
                        {
                            int replySeq = 1;
                            foreach (var replyS in excelData.ReplyList)
                            {

                                Guid replyKey = (Guid)replyS.ReplyKey;
                                var replyRow = sheet.CreateRow(rowIndex);
                                rowIndex++;
                                int replyCellIndex = 0;

                                replyRow.CreateCell(replyCellIndex).SetCellValue(replySeq.ToString().PadLeft(4, '0'));
                                replyCellIndex++;
                                replyRow.CreateCell(replyCellIndex).SetCellValue("'" + replyS.VerifyInfo.ToString().Trim());
                                replyCellIndex++;
                                replyRow.CreateCell(replyCellIndex).SetCellValue("'" + replyS.ParameterInfo.ToString().Trim());
                                replyCellIndex++;
                                foreach (var questionS in excelData.QuestionList)
                                {
                                    //無填答資料，則答案皆放空，不需做處理
                                    if (replyS.AnswerList == null || replyS.AnswerList.Count < 1)
                                    {
                                        Log.Info("無填答資料:" + replyS.ReplyKey.ToString());
                                        break;
                                    }

                                    string questionId = questionS.QuestionId.ToString().Trim();
                                    switch (questionS.QuestionType)
                                    {
                                        case 1://單選題
                                            if (questionS.OptionList != null && questionS.OptionList.Count > 0)
                                            {
                                                bool isFind = false;
                                                foreach (var optionS in questionS.OptionList)
                                                {
                                                    var query1 = from answer in replyS.AnswerList
                                                                 where answer.QuestionId.ToString().Trim().Equals(questionId) && (Guid)answer.ReplyKey == replyKey && answer.OptionId.ToString().Trim().Equals(optionS.OptionId.ToString().Trim())
                                                                 select answer;
                                                    if (query1.Count() == 1)
                                                    {
                                                        isFind = true;

                                                        replyRow.CreateCell(replyCellIndex).SetCellValue(optionS.OptionSeq.ToString());
                                                        replyCellIndex++;
                                                        if ((bool)questionS.HasOther && (bool)questionS.OtherIsShowText)
                                                        {
                                                            replyRow.CreateCell(replyCellIndex).SetCellValue(query1.FirstOrDefault().BlankAnwer.ToString().Trim());
                                                            replyCellIndex++;
                                                        }

                                                        break;
                                                    }
                                                    else
                                                    {
                                                        continue;
                                                    }
                                                }
                                                if (!isFind)
                                                {
                                                    replyRow.CreateCell(replyCellIndex).SetCellValue("");
                                                    replyCellIndex++;
                                                    if ((bool)questionS.HasOther && (bool)questionS.OtherIsShowText)
                                                    {
                                                        replyRow.CreateCell(replyCellIndex).SetCellValue("");
                                                        replyCellIndex++;
                                                    }
                                                }
                                            }
                                            break;
                                        case 2://多選題
                                            var otherAnswer = "";
                                            bool noAnswer = false;
                                            if (questionS.OptionList != null && questionS.OptionList.Count > 0)
                                            {
                                                foreach (var optionS in questionS.OptionList)
                                                {
                                                    var queryChkReply = from answer in replyS.AnswerList
                                                                        where answer.QuestionId.ToString().Trim().Equals(questionId)
                                                                        select answer;
                                                    if (queryChkReply.Count() == 0)
                                                    {
                                                        noAnswer = true;
                                                    }

                                                    var query2 = from answer in replyS.AnswerList
                                                                 where answer.QuestionId.ToString().Trim().Equals(questionId) && (Guid)answer.ReplyKey == replyKey && answer.OptionId.ToString().Trim().Equals(optionS.OptionId.ToString().Trim())
                                                                 select answer;
                                                    if (query2.Count() == 1)
                                                    {
                                                        if (optionS.OtherFlag == null || string.IsNullOrEmpty(optionS.OtherFlag.ToString().Trim()) || !(bool)optionS.OtherFlag)
                                                        {
                                                            replyRow.CreateCell(replyCellIndex).SetCellValue("1");
                                                            replyCellIndex++;
                                                        }
                                                        else
                                                        {
                                                            replyRow.CreateCell(replyCellIndex).SetCellValue("1");
                                                            replyCellIndex++;
                                                            otherAnswer = query2.FirstOrDefault().BlankAnwer.ToString().Trim();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        string cellValue = noAnswer ? "" : "0";
                                                        replyRow.CreateCell(replyCellIndex).SetCellValue(cellValue);
                                                        replyCellIndex++;
                                                    }
                                                }
                                                if ((bool)questionS.HasOther && (bool)questionS.OtherIsShowText)
                                                {
                                                    replyRow.CreateCell(replyCellIndex).SetCellValue(otherAnswer);
                                                    replyCellIndex++;
                                                }
                                            }
                                            break;
                                        case 3://填空題
                                            try
                                            {
                                                Log.Info("填空題 start!" + questionId);
                                                var query3 = from answer in replyS.AnswerList
                                                             where answer.QuestionId.ToString().Trim().Equals(questionId) && (Guid)answer.ReplyKey == replyKey
                                                             select answer;
                                                if (query3.Count() == 1)
                                                {
                                                    replyRow.CreateCell(replyCellIndex).SetCellValue(query3.FirstOrDefault().BlankAnwer.ToString().Trim());
                                                    replyCellIndex++;
                                                }
                                                else
                                                {
                                                    replyRow.CreateCell(replyCellIndex).SetCellValue("");
                                                    replyCellIndex++;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error("填空題異常:(questionId)" + questionId + "(replyKey)" + replyKey + "\n");
                                                foreach (var item in replyS.AnswerList)
                                                {
                                                    Log.Error(item.QuestionId + ";" + item.ReplyKey + "\n");
                                                }
                                                Log.Error(ex.ToString() + "\n");
                                            }

                                            break;
                                        case 4://矩陣題
                                            if (!string.IsNullOrEmpty(questionS.MatrixItems.ToString()))
                                            {
                                                string[] fields = questionS.MatrixItems.ToString().Split(';');
                                                foreach (string field in fields)
                                                {
                                                    if (string.IsNullOrEmpty(field.Trim()))
                                                        continue;


                                                    var query2 = from answer in replyS.AnswerList
                                                                 where answer.QuestionId.ToString().Trim().Equals(questionId) && (Guid)answer.ReplyKey == replyKey && answer.MatrixField.ToString().Trim().Equals(field.Trim())
                                                                 select answer;

                                                    if (query2.Count() == 1)
                                                    {
                                                        var query2R = from option in questionS.OptionList
                                                                      where option.OptionId.ToString().Trim().Equals(query2.FirstOrDefault().OptionId.ToString().Trim())
                                                                      select option;
                                                        if (query2R.Count() == 1)
                                                        {
                                                            replyRow.CreateCell(replyCellIndex).SetCellValue(query2R.FirstOrDefault().OptionSeq.ToString());
                                                        }
                                                        else
                                                        {
                                                            replyRow.CreateCell(replyCellIndex).SetCellValue("");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        replyRow.CreateCell(replyCellIndex).SetCellValue("");
                                                    }
                                                    replyCellIndex++;
                                                }
                                            }
                                            break;
                                        case 5://基本資料題
                                            var query4 = from answer in replyS.AnswerList
                                                         where answer.QuestionId.ToString().Trim().Equals(questionId) && (Guid)answer.ReplyKey == replyKey
                                                         select answer;
                                            if (query4.Count() == 1)
                                            {
                                                replyRow.CreateCell(replyCellIndex).SetCellValue(query4.FirstOrDefault().BlankAnwer.ToString().Trim());
                                                replyCellIndex++;
                                            }
                                            else
                                            {
                                                replyRow.CreateCell(replyCellIndex).SetCellValue("");
                                                replyCellIndex++;
                                            }
                                            break;
                                    }
                                }

                                replyRow.CreateCell(replyCellIndex).SetCellValue(replyS.TimePeriod.ToString().Trim());
                                replyCellIndex++;
                                replyRow.CreateCell(replyCellIndex).SetCellValue(replyS.SubmitTime.ToString().Trim());
                                replyCellIndex++;
                                replyRow.CreateCell(replyCellIndex).SetCellValue(replyS.ProvideType.ToString().Trim());
                                replyCellIndex++;
                                replyRow.CreateCell(replyCellIndex).SetCellValue(((bool)replyS.ForceEnd) ? "Y" : "N");
                                replyCellIndex++;
                                replyRow.CreateCell(replyCellIndex).SetCellValue(replyS.Device.ToString().Trim());
                                replyCellIndex++;

                                replySeq++;
                            }
                        }
                    }

                    workbook.Write(fs);
                }

                //讀取寫好的xls檔案,再寫成csv檔
                XSSFWorkbook xssfwb;
                MemoryStream csvStream = new MemoryStream();
                using (FileStream file = new FileStream(Utility.Common.removeSpecialCharactersPath(newFile), FileMode.Open, FileAccess.Read))
                {
                    xssfwb = new XSSFWorkbook(file);
                    ISheet sheet = xssfwb.GetSheetAt(0);
                    string strLine;
                    string strCol;
                    StreamWriter csvWriter = new StreamWriter(csvStream, Encoding.UTF8);
                    for (int row = 0; row <= sheet.LastRowNum; row++)
                    {
                        strLine = "";

                        if (sheet.GetRow(row) != null) //null is when the row only contains empty cells 
                        {
                            for (int collumn = 0; collumn <= sheet.GetRow(row).LastCellNum; collumn++)
                            {

                                strCol = sheet.GetRow(row).GetCell(collumn) is null ? "" : sheet.GetRow(row).GetCell(collumn).StringCellValue;
                                if (collumn != sheet.GetRow(row).LastCellNum)
                                {
                                    if (collumn <= 2)
                                    {
                                        strCol = strCol.Replace("'", "");
                                    }
                                    strLine += strCol.Replace(",", "，") + ",";
                                    //strLine += strCol;
                                }
                            }
                            csvWriter.WriteLine(strLine);
                        }
                    }
                    csvWriter.Flush();
                }

                csvStream.Position = 0;

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(csvStream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName + ".csv"
                };

                Log.Info("ExportSurvey FileName:" + fileName + ".csv");
                Log.Info("ExportSurvey Content:" + response.Content);
                return response;

             
            }
            catch (Exception ex)
            {
                Log.Error("ExportSurvey:" + ex);
                throw ex;
            }
        }
        public Survey4ReportCsv PrepareExcelData(String SurveyId)
        {
            if (String.IsNullOrWhiteSpace(SurveyId))
            {
                Log.Info("成效分析-數據下載!" + "參數SurveyId不能為空!");
                throw new Exception("參數SurveyId不能為空!");
            }
            var Env = "2";
            Survey4ReportCsv survey = new Survey4ReportCsv(); //SurveyId
            //題型中文
            var CodeCode = "0100";
            string sSql = "";
            var Title = "";
            try
            {
                //如果QUE021沒有資料請回傳無填答資訊?
                sSql = " SELECT DISTINCT A.SurveyId, B.Title FROM QUE021_AnwserCollection A " +
                    " INNER JOIN QUE001_QuestionnaireBase B ON B.SurveyId=A.SurveyId " +
                    $" WHERE A.SurveyId=@SurveyId AND (A.DelFlag IS NULL OR A.DelFlag<>1) and A.Env=@Env ";  //成效分析的兩個API，要過濾QUE021的delflag  true不要取
                //-------sql para----start
                SqlParameter[] sqlParams = new SqlParameter[] {
                    new SqlParameter("@SurveyId", SqlDbType.UniqueIdentifier),
                    new SqlParameter("@Env", SqlDbType.Int)
                };
                sqlParams[0].Value = new System.Data.SqlTypes.SqlGuid(SurveyId);
                sqlParams[1].Value = Convert.ToInt32(Env);
                //-------sql para----end
                DataTable dt = _db.GetQueryData(sSql, sqlParams);
                if (dt.Rows.Count < 1)
                {
                    Log.Error("成效分析-數據下載!" + "無填答資訊！");
                    return null;
                }
                Title = dt.Rows[0]["Title"].ToString().Trim();

                sSql = $"SELECT * FROM GEN004_AllCode WHERE CodeCode=@CodeCode Order by Cast(CodeSubCode as int) ";
                //-------sql para----start
                SqlParameter[] sqlParamsA = new SqlParameter[] {
                    new SqlParameter("@CodeCode", SqlDbType.VarChar)
                };
                sqlParamsA[0].Value = CodeCode;
                //-------sql para----end
                DataTable dtCode = _db.GetQueryData(sSql, sqlParamsA);
                if (dtCode.Rows.Count < 1)
                {
                    Log.Error("成效分析-數據下載!" + $"GEN004_AllCode未設定題型參數[{CodeCode}]！");
                    return null;
                }
                // TotalReplyNum =問卷填寫數量：統計QUE_21數量BY SurveyID
                survey.SurveyId = SurveyId;
                survey.Title = Title;

                //處理一般題型
                List<Question4ReportCsv> lstQuestion = new List<Question4ReportCsv>();   //QuestionId
                foreach (DataRow drCode in dtCode.Rows)
                {
                    //只處理單選題:1、多選題:2、填空題:3、矩陣題:4、基本資料:5
                    var QuestionType = Convert.ToInt32(drCode["CodeSubCode"]);
                    if (QuestionType == 1 || QuestionType == 2 || QuestionType == 3 || QuestionType == 5)
                    {
                        //題目
                        var normal = ProcessCommonQuestion4ReportCsv(SurveyId, QuestionType);
                        if (normal.Count > 0)
                            lstQuestion.AddRange(normal);
                    }
                    else if (QuestionType == 4)  //矩陣題
                    {
                        //處理矩陣題型
                        var matrix = ProcessMatrixQuestion4ReportCsv(SurveyId, QuestionType);
                        if (matrix.Count > 0)
                            lstQuestion.AddRange(matrix);
                    }
                    else
                        continue;
                }
                List<Question4ReportCsv> lstQuestionReal = lstQuestion.OrderBy(x => Convert.ToInt32(x.QuestionSeq)).ToList<Question4ReportCsv>();
                //有Question才設定Survey.QuestionList， 否則 Survey.QuestionList=null
                if (lstQuestionReal.Count > 0)
                    survey.QuestionList = lstQuestionReal;
                //再加答題部分
                List<Reply4ReportCsv> lstReply = new List<Reply4ReportCsv>();   //QuestionId
                lstReply = GetReplyData4ReportCsv(SurveyId, Env);
                if (lstReply.Count > 0)
                    survey.ReplyList = lstReply;
            }
            catch (Exception ex)
            {
                Log.Error("成效分析-數據下載查詢失敗!" + ex.Message);
                return null;
            }

            //返回結果
            return survey;
        }
        #endregion

        #region "查詢回覆數量"
        [Route("QueryReplyNum")]
        [HttpGet]
        public HttpResponseMessage QueryReplyNum(String SurveyId, String Env)
        {
            ReplyData replyData = new ReplyData();
            if (String.IsNullOrWhiteSpace(SurveyId))
            {
                //報告錯誤
                replyData.code = "-1";
                replyData.message = $"參數SurveyId不能為空！";
                Log.Error("成效分析-查詢回覆數量!" + "參數SurveyId不能為空！");
                return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
            }
            if (String.IsNullOrWhiteSpace(Env))
            {
                //報告錯誤
                replyData.code = "-1";
                replyData.message = $"參數Env不能為空！";
                Log.Error("成效分析-查詢回覆數量!" + "參數Env不能為空！");
                return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
            }
            try
            {
                SurveyQueryReplyNum survey = new SurveyQueryReplyNum();
                survey.SurveyId = SurveyId;
                survey.TotalReplyNum = GetTotalReplyNumBy(SurveyId, Env);
                replyData.code = "200";
                replyData.message = $"查詢記錄完成。";
                replyData.data = survey;
                Log.Info("成效分析-查詢回覆數量!" + survey.TotalReplyNum);
            }
            catch (Exception ex)
            {
                replyData.code = "-1";
                replyData.message = $"成效分析-查詢回覆數量失敗！{ex.Message}.";
                replyData.data = "";
                Log.Error("成效分析-查詢回覆數量失敗!" + ex.Message);
            }

            //返回結果
            return ReturnJsonBy(JsonConvert.SerializeObject(replyData));
        }
        #endregion
    }
    #region For下載
    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    //public class FileDownloadAttribute : Attribute
    //{
    //    public FileDownloadAttribute(string cookieName = "fileDownload", string cookiePath = "/")
    //    {
    //        CookieName = cookieName;
    //        CookiePath = cookiePath;
    //    }

    //    public string CookieName { get; set; }

    //    public string CookiePath { get; set; }

    //    /// <summary>
    //    /// If the current response is a FileResult (an MVC base class for files) then write a
    //    /// cookie to inform jquery.fileDownload that a successful file download has occured
    //    /// </summary>
    //    /// <param name="filterContext"></param>
    //    private void CheckAndHandleFileResult(Actio filterContext)
    //    {
    //        var httpContext = filterContext.HttpContext;
    //        var response = httpContext.Response;

    //        if (filterContext.Result is FileResult)
    //            //jquery.fileDownload uses this cookie to determine that a file download has completed successfully
    //            response.Cookies.Append(CookieName, "true", new CookieOptions() { Path = CookiePath });
    //        else
    //            //ensure that the cookie is removed in case someone did a file download without using jquery.fileDownload
    //            if (httpContext.Request.Cookies[CookieName] != null)
    //        {
    //            response.Cookies.Append(CookieName, "true", new CookieOptions() { Expires = DateTime.Now.AddYears(-1), Path = CookiePath });
    //        }
    //    }

    //    public override void OnActionExecuted(ActionExecutedContext filterContext)
    //    {
    //        CheckAndHandleFileResult(filterContext);

    //        base.OnActionExecuted(filterContext);
    //    }
    //}
    #endregion

    #region 數據顯示查詢--資料結構
    public class Survey4Analysis
    {
        public Object SurveyId { get; set; }
        /// <summary>
        /// 問卷填寫數量，統計QUE_21數量BY SurveyID
        /// </summary>
        public Object TotalReplyNum { get; set; }
        public List<Question4Analysis> QuestionList { get; set; }

    }
    public class Question4Analysis
    {
        /// <summary>
        /// 題目ID
        /// </summary>
        public Object QuestionId { get; set; }
        /// <summary>
        /// 題目順序
        /// </summary>
        public Object QuestionSeq { get; set; }
        /// <summary>
        /// 題目類型
        /// </summary>
        public Object QuestionType { get; set; }
        /// <summary>
        /// 題目類型說明
        /// </summary>
        public Object TypeContent { get; set; }
        public Object IsRequired { get; set; }
        /// <summary>
        /// 題目標題文字說明
        /// </summary>
        public Object QuestionTitle { get; set; }
        /// <summary>
        /// 題目標題
        /// </summary>
        public Object QuestionSubject { get; set; }
        //以下為後來新增欄位，需要時可以放開
        //public Object BlankMaxLimit { get; set; }
        //public Object BlankMinLimit { get; set; }
        public Object QuestionImage { get; set; }
        public Object QuestionVideo { get; set; }
        //public Object MultiOptionLimit { get; set; }
        /// <summary>
        /// 題目填寫數量，統計QUE_22數量 BY QuestionID
        /// </summary>
        public Object QuestionReplyNum { get; set; }
        /// <summary>
        /// X Axis
        /// </summary>
        public Object X_Axis { get; set; }

        public List<Option4Analysis> OptionList { get; set; }
    }
    public class Option4Analysis
    {
        public Object OptionId { get; set; }
        public Object OptionSeq { get; set; }
        public Object OptionContent { get; set; }
        //以下為後來新增欄位，需要時放開
        public Object OptionImage { get; set; }
        public Object OptionVideo { get; set; }
        /// <summary>
        /// 是否其他選項
        /// </summary>
        public Object OtherFlag { get; set; }
        /// <summary>
        /// 選項選擇數量，統計QUE_22 數量 by QuestionID, OptionId
        /// </summary>
        public Object OptionReplyNum { get; set; }
        /// <summary>
        /// 當題回覆百分比（四捨五入保留一位小數，不加%）
        /// </summary>
        public Object OptionReplyPercent { get; set; }
        /// <summary>
        /// 矩陣欄位
        /// </summary>
        public Object MatrixField { get; set; }
    }
    //public class Matrix4Analysis
    //{
    //    public Object MatrixField { get; set;}
    //    public Object OptionId { get; set; }
    //    public Object OptionContent { get; set; }
    //    /// <summary>
    //    /// 選項選擇數量，統計QUE_22 數量 by QuestionID, OptionId
    //    /// </summary>
    //    public Object OptionReplyNum { get; set; }
    //}
    //Matrix 有MatrixField，沒有OptionSeq; 其他沒有Matrix field, 有optionSeq
    //可以用泛型
    #region 泛型測試
    //public class Question4Analysis<T>
    //{
    //    public Object QuestionId { get; set; }
    //    public Object QuestionSeq { get; set; }
    //    public Object QuestionType { get; set; }
    //    public Object TypeContent { get; set; }
    //    public Object IsRequired { get; set; }
    //    public Object QuestionSubject { get; set; }
    //    /// <summary>
    //    /// 題目填寫數量，統計QUE_22數量 BY QuestionID
    //    /// </summary>
    //    public Object QuestionReplyNum { get; set; }

    //    public List<T> OptionList { get; set; }
    //}
    //使用時 new Question4Analysis<類型>, OptionList就是該類型
    //MatrixAnalysis ma = new MatrixAnalysis();
    //Question4Analysis<Matrix4Analysis> question = new Question4Analysis<Matrix4Analysis>()
    //question.OptionList=ma;
    #endregion
    #endregion

    #region 成效分析-數據下載(csv)--回傳資料結構
    public class Survey4ReportCsv
    {
        /// <summary>
        /// 問卷ID  uniqueidentifier
        /// </summary>
        public Object SurveyId { get; set; }
        /// <summary>
        /// 問卷名稱 nvarchar
        /// </summary>
        public Object Title { get; set; }
        /// <summary>
        /// 題目
        /// </summary>
        public List<Question4ReportCsv> QuestionList { get; set; }
        public List<Reply4ReportCsv> ReplyList { get; set; }
    }
    public class Question4ReportCsv
    {
        /// <summary>
        /// 題目ID   uniqueidentifier
        /// </summary>
        public Object QuestionId { get; set; }
        /// <summary>
        /// 題目標號(題目順序) int
        /// </summary>
        public Object QuestionSeq { get; set; }
        /// <summary>
        /// 題目標題 nvarchar
        /// </summary>
        public Object QuestionSubject { get; set; }
        /// <summary>
        /// 題目類型 int
        /// </summary>
        public Object QuestionType { get; set; }
        /// <summary>
        /// 題目類型說明 nvarchar
        /// </summary>
        public Object TypeContent { get; set; }
        /// <summary>
        /// 基本資料驗證方式 int
        /// </summary>
        public Object BaseDataValidType { get; set; }
        /// <summary>
        /// 是否必填
        /// </summary>
        public Object IsRequired { get; set; }
        // allen: Gem {{hostip}}/api/Analytics/ReportCSV 這個API有誤，
        // 需要去掉OptionList內的MatrixField改在QuestionList新增MatrixItems
        /// <summary>
        /// 矩陣欄位
        /// </summary>
        public Object MatrixItems { get; set; }

        public Object HasOther { get; set; }

        public Object OtherIsShowText { get; set; }


        /// <summary>
        /// 選項
        /// </summary>
        public List<Option4ReportCsv> OptionList { get; set; }

    }
    public class Option4ReportCsv
    {
        /// <summary>
        /// 選項ID --uniqueidentifier
        /// </summary>
        public Object OptionId { get; set; }
        /// <summary>
        /// 選項代號
        /// </summary>
        public Object OptionSeq { get; set; }
        /// <summary>
        /// 選項內容
        /// </summary>
        public Object OptionContent { get; set; }
        /// <summary>
        /// 是否其他選項
        /// </summary>
        public Object OtherFlag { get; set; }
        //Gem {{hostip}}/api/Analytics/ReportCSV 這個API有誤，需要去掉OptionList內的MatrixField改在QuestionList新增MatrixItems
        ///// <summary>
        ///// 矩陣題欄位  --
        ///// </summary>
        //public Object MatrixField { get; set; }
    }
    public class Reply4ReportCsv
    {
        /// <summary>
        /// 序號
        /// </summary>
        public Object Seq { get; set; }
        /// <summary>
        /// 回復ID（自增）string
        /// </summary>
        public Object ReplyKey { get; set; }
        /// <summary>
        /// 問卷管道 int
        /// </summary>
        public Object ProvideType { get; set; }
        /// <summary>
        /// 記名註記欄位 nvarchar
        /// </summary>
        public Object ExtenField { get; set; }
        /// <summary>
        /// 問卷登入方式-資格驗證 nvarchar
        /// </summary>
        public Object VerifyInfo { get; set; }
        /// <summary>
        /// 問卷登入方式-參數傳遞  nvarchar
        /// </summary>
        public Object ParameterInfo { get; set; }
        /// <summary>
        /// 裝置  --nvarchar
        /// </summary>
        public Object Device { get; set; }
        /// <summary>
        /// 強制結束問卷  --bit
        /// </summary>
        public Object ForceEnd { get; set; }
        /// <summary>
        /// 作答時間   --char
        /// </summary>
        public Object TimePeriod { get; set; }

        /// <summary>
        /// 填寫時間   --datetime2
        /// </summary>
        public Object SubmitTime { get; set; }
        public List<Answer4ReportCsv> AnswerList { get; set; }
    }
    public class Answer4ReportCsv
    {
        /// <summary>
        /// 序號 int 自增
        /// </summary>
        public Object id { get; set; }
        /// <summary>
        /// 回覆Id string
        /// </summary>
        public Object ReplyKey { get; set; }
        /// <summary>
        /// 題目Id   --uniqueidentifier
        /// </summary>
        public Object QuestionId { get; set; }
        /// <summary>
        /// 答題選項Id  --uniqueidentifier
        /// </summary>
        public Object OptionId { get; set; }
        /// <summary>
        /// 矩陣欄位  --nvarchar
        /// </summary>
        public Object MatrixField { get; set; }
        /// <summary>
        /// 填空答案  --nvarchar
        /// </summary>
        public Object BlankAnwer { get; set; }
        /// <summary>
        /// 選項順序 --int
        /// </summary>
        public Object OptionSeq { get; set; }
        /// <summary>
        /// 是否其他選項
        /// </summary>
        public Object OtherFlag { get; set; }
    }
    #endregion

    public class SurveyQueryReplyNum
    {
        public Object SurveyId { get; set; }
        public Object TotalReplyNum { get; set; }

    }

}
