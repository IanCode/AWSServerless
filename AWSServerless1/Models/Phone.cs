using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWSServerless1
{
    public class Phone
    {
        public PhoneType PhoneNumberType { get; set; }

        public string CallingCode { get; set; }

        public string Number { get; set; }
    }
}
