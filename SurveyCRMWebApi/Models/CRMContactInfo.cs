using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SurveyCRMWebApiV2.Models
{
    public class CRMContactInfo
    {
        public String code;
        public String message;
        public List<ContactInfo> data;
    }

    public class ContactInfo
    {
        public int ContactCount;
    }
}
