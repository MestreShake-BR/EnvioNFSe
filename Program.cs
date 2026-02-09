using NFSe.Class;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.ModelBinding;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NFSe
{

    internal static class Program
    {
        #region
        #region DEBUGAR UM METODO
        static void Main()
        {
#if (!DEBUG)
            ServiceBase[] ServicesToRun;

            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };

            ServiceBase.Run(ServicesToRun);

#else
            MontaDPS.Envio().GetAwaiter().GetResult();
            MontaRPSSP.EnvioSP().GetAwaiter().GetResult();
#endif
        }
        #endregion DEBUGAR UM METODO
        #endregion
    }
}