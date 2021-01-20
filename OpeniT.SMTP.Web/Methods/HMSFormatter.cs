using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Methods
{
    public class HMSFormatter : ICustomFormatter, IFormatProvider
    {
        // list of Formats, with a P customformat for pluralization
        static Dictionary<string, string> timeformats = new Dictionary<string, string> {
            {"S", "{0:P:Seconds:Second}"},
            {"M", "{0:P:Minutes:Minute}"},
            {"H","{0:P:Hours:Hour}"},
            {"D", "{0:P:Days:Day}"}
        };

        static Dictionary<string, string> timeformatsShort = new Dictionary<string, string> {
            {"S", "{0:P:s:s}"},
            {"M", "{0:P:m:m}"},
            {"H","{0:P:h:h}"},
            {"D", "{0:P:d:d}"}
        };

        public bool FormatShort = false;

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (FormatShort)
            {
                return String.Format(new PluralFormatter(), timeformatsShort[format], arg);
            }
            else
            {
                return String.Format(new PluralFormatter(), timeformats[format], arg);
            }
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }

}
