using System;
using System.Collections.Generic;
using System.Text;

namespace Zebra_RFID_Scanner_Android.Services
{
    public class EpctoUpc
    {
        public  static string EpctoUpcs(string epc) {
            string EPC = epc;
            string EPCIN = "";
            string SGTIN = "";
            string ItemRef = "";
            string Result = "";
            string UPC = "";
            int i, SGTINResult, ItemRefResult, CheckDigit = 0;

            for (i = 0; i < EPC.Length; i++)
            {
                EPCIN += Convert.ToString(Convert.ToInt32(EPC.Substring(i, 1), 16), 2).PadLeft(4, '0');
            }

            EPCIN = EPCIN.Substring(EPCIN.Length - 82);
            SGTIN = EPCIN.Substring(0, 24);
            ItemRef = EPCIN.Substring(24, 20);

            SGTINResult = 0;
            for (i = 1; i < SGTIN.Length; i++)
            {
                SGTINResult += Convert.ToInt32(SGTIN.Substring(i, 1)) * (int)Math.Pow(2, SGTIN.Length - i - 1);
            }

            ItemRefResult = 0;
            for (i = 1; i < ItemRef.Length; i++)
            {
                ItemRefResult += Convert.ToInt32(ItemRef.Substring(i, 1)) * (int)Math.Pow(2, ItemRef.Length - i - 1);
            }

            Result = SGTINResult.ToString() + ("00000" + ItemRefResult).Substring(Math.Max(0, ("00000" + ItemRefResult).Length - 5));

            CheckDigit = 0;
            for (i = 1; i <= 17; i++)
            {
                if (Result.Length > Math.Abs(i - 17))
                {
                    if (i % 2 != 0)
                    {
                        CheckDigit += 3 * Convert.ToInt32(Result.Substring(Result.Length - Math.Abs(i - 17) - 1, 1));
                    }
                    else
                    {
                        CheckDigit += Convert.ToInt32(Result.Substring(Result.Length - Math.Abs(i - 17) - 1, 1));
                    }
                }
            }

            CheckDigit = Convert.ToInt32(Math.Ceiling((double)CheckDigit / 10) * 10) - CheckDigit;
            UPC = Result + CheckDigit;

            return UPC;
        }

    }
}
