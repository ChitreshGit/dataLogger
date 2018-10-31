using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;

namespace adcUartBar
{
    class ADC_class
    {
        // Constants
        const char ADC_START_KEY = ':';
        const char ADC_STOP_KEY = ';';
        int oldAdcValue = 0;

        public int[] adc;
        // default construct
        public ADC_class()
        {
            adc = new int[1] { 0 };
        }

        // parameter construct
        public ADC_class(int adcCh)
        {
            switch (adcCh)
            {
                case 1:
                    adc = new int[1] { 0 };
                    break;
                case 4:
                    adc = new int[4] { 0,0,0,0 };
                    break;
                case 5:
                    adc = new int[5] { 0,0,0,0,0 };
                    break;
                case 6:
                    adc = new int[6] { 0,0,0,0,0,0 };
                    break;
                default:
                    adc = new int[1] { 0 };
                    break;
            }
        }

        // parse ADC read string
        // input string Format:
        // c:<CRC>;1:<ADC1>;2:<ADC2>;3:<ADC3>;4:<ADC4>...\n
        bool Parse_com_string(ref string pComStr, ref int[] pAdc)
        {
            int adcChMax = pAdc.GetUpperBound(0) + 1;
            string adcValueStr = new string('0',20);
            string startKey = new string('0', 10);
            int adcValueStartLoc = 0;
            int adcValueStopLoc = 0;
            int adcValueLen = 0;


            // loop to extract the ADC values TODO: to test this 
            for (int i = 0; i < adcChMax; i++)
            {
                startKey = Convert.ToString(i + 1) + ADC_START_KEY;
                adcValueStartLoc = pComStr.IndexOf(startKey) + 2;
                adcValueStopLoc = pComStr.IndexOf(ADC_STOP_KEY);
                adcValueLen = adcValueStopLoc - adcValueStartLoc;
                if (((adcValueStartLoc-2) < 0) || (adcValueStopLoc < 0) || (adcValueLen < 0))
                    return false;
                adcValueStr = pComStr.Substring(adcValueStartLoc, adcValueLen);
                // Set int ADC value
                pAdc[i] = Convert.ToInt32(adcValueStr);
                // move COM string ahead
                pComStr = pComStr.Substring(adcValueStopLoc+1, pComStr.Length - (adcValueStopLoc+1));
            }
            return true;
        }

        // Get ADC values from COM port
        public bool Get_ADC_Values(SerialPort comPort, ref int[] pAdc)
        {
            int adcChMax = pAdc.GetUpperBound(0) + 1;
            string receiveBufer = null;

            if ((adcChMax <= 0) || (comPort.IsOpen == false))
                return false;

            // Create buffer
            // receiveBufer = new string('\n', 100);

            // read line from comport
            try
            {
                // Clear read buffer first as there will be many unaddressed recieve data
                comPort.DiscardInBuffer();
                // Issue a fresh read
                receiveBufer = comPort.ReadLine();
                // parse the read string for adc
                if ((Parse_com_string(ref receiveBufer, ref pAdc) == false))
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
