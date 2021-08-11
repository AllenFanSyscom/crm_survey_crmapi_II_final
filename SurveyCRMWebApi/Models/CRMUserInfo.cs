using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SurveyCRMWebApiV2.Models
{
    public class CRMUserInfo
    {
        public String code;
        public String message;
        public List<UserInfo> data;
    }

    public class UserInfo
    {
        public String UserId;

        public String UserName;

        public String UserCode;

        public String Telephone;
    }
}
