using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWSServerless1
{

    public class Contact
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string PrimaryEmail { get; set; }

        public List<string> SecondaryEmails { get; set; }

        public List<Phone> Phones { get; set; }

        public Address StreetAddress { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        /// <summary>
        /// Overriding the default comparision for ease of use. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as Contact;

            if (other == null)
            {
                return false;
            }

            if(Name != other.Name || PrimaryEmail != other.PrimaryEmail)
            {
                return false;
            }

            if(StreetAddress.State != other.StreetAddress.State || StreetAddress.City != other.StreetAddress.City || StreetAddress.Street != other.StreetAddress.Street || StreetAddress.ZipCode != other.StreetAddress.ZipCode)
            {
                return false;
            }

            if (SecondaryEmails.Count != other.SecondaryEmails.Count || Phones.Count != other.Phones.Count)
            {
                return false;
            }

            for(int i = 0; i < SecondaryEmails.Count; i++)
            {
                if(SecondaryEmails[i] != other.SecondaryEmails[i])
                {
                    return false;
                }
            }

            for(int i = 0; i < Phones.Count; i++)
            {
                if(Phones[i].PhoneNumberType != other.Phones[i].PhoneNumberType || Phones[i].CallingCode != other.Phones[i].CallingCode || Phones[i].Number != other.Phones[i].Number)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
