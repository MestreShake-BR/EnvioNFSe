using NFSe.Class;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace NFSe
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        private System.Timers.Timer timer;

        protected override void OnStart(string[] args)
        {
            timer = new System.Timers.Timer();
            timer.Interval = 60000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            GravarLog("Serviço iniciado");
            EventLog.WriteEntry("NFSeService", "Serviço iniciado", EventLogEntryType.Information);
        }

        public async Task ExecutarTarefa()
        {
            try
            {
                GravarLog("Iniciando execução da tarefa");
                await MontaDPS.Envio();
                await MontaRPSSP.EnvioSP();
                GravarLog("Execução finalizada com sucesso");
            }
            catch (Exception ex)
            {
                GravarLog("Erro: " + ex.ToString());
                EventLog.WriteEntry("NFSeService", ex.ToString(), EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            GravarLog("Serviço parado");
            EventLog.WriteEntry("NFSeService", "Serviço parado", EventLogEntryType.Information);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(() => ExecutarTarefa());
        }
        private void GravarLog(string mensagem)
        {
            try
            {
                string caminho = @"C:\Logs\";

                if (!Directory.Exists(caminho))
                    Directory.CreateDirectory(caminho);

                string arquivo = Path.Combine(caminho, "log.txt");

                using (StreamWriter sw = new StreamWriter(arquivo, true))
                {
                    sw.WriteLine($"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {mensagem}");
                }
            }
            catch
            {

            }
        }

    }
}
