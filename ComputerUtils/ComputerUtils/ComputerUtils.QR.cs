using ComputerUtils.Logging;
using QRCoder;
using System;
using System.Collections;

namespace ComputerUtils.QR
{
    public class QRCodeGeneratorWrapper
    {
        public static void Display(string data)
        {
            QRCodeData qr = new QRCodeGenerator().CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            string qRCode = data + "\n";
            foreach (BitArray b in qr.ModuleMatrix)
            {
                foreach (bool bb in b)
                {
                    qRCode += bb ? "\u2588\u2588" : "  ";
                }
                qRCode += "\n";
            }
            Logger.Log(qRCode);
            if(!Logger.displayLogInConsole)
            {
                Console.WriteLine(qRCode);
            }
        }
    }
}