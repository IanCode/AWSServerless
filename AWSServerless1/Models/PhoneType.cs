using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;


namespace AWSServerless1
{
    public enum PhoneType
    {
        [EnumMember(Value = "unknown")]
        Unknown = 0,

        [EnumMember(Value = "landline")]
        Landline,

        [EnumMember(Value = "mobile")]
        Mobile,

        [EnumMember(Value = "fax")]
        Fax
    }
}
