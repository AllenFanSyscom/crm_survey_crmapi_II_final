using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SurveyCRMWebApiV2.Models
{
    public class ContactList
    {
        /// <summary>
        /// 行銷活動方式識別碼
        /// </summary>
        public Guid ActivityId { get; set; }
        /// <summary>
        /// 行銷名單名稱
        /// </summary>
        public string ListName { get; set; }
    }

    public class CheckValidDateTimeFromCRM
    {
        public String SysDateTime { get; set; }
        public DateTime New_effectivestart { get; set; }
        public DateTime New_effectiveend { get; set; }
        public int New_statuscode { get; set; }
        public int statuscode { get; set; } //執行狀態
    }
    public class GetCRMUserInfoBy
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserCode { get; set; }
        public string Telephone { get; set; }
    }
    public class GetCRMUserInfoBy1
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        
    }
    public class GetCRMUserInfoBy2
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
    }
    public class GetCrmStatusBy
    {
        public DateTime New_effectivestart { get; set; }
        public DateTime New_effectiveend { get; set; }
        public int New_statuscode { get; set; } // 行銷活動方式狀態
 
    }
    
    public class VerifyBy
    {
        public int ContactCount { get; set; }
    };
}