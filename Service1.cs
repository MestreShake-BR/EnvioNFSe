using NFSe.Class;
using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace NFSe
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer _timer;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        // Referência à tarefa em execução para aguardar no OnStop
        private Task _tarefaEmAndamento = Task.CompletedTask;

        // Configurações lidas do App.config
        private static readonly int _intervaloMs =
            int.TryParse(ConfigurationManager.AppSettings["Service.IntervaloMs"], out int iv) ? iv : 60000;

        private static readonly int _onStopTimeoutMs =
            int.TryParse(ConfigurationManager.AppSettings["Service.OnStopTimeoutMs"], out int ot) ? ot : 120000;

        private static readonly string _logPath =
            ConfigurationManager.AppSettings["Service.LogPath"] ?? @"C:\Logs\";

        private static readonly int _logRetencaoDias =
            int.TryParse(ConfigurationManager.AppSettings["Service.LogRetencaoDias"], out int rd) ? rd : 30;

        protected override void OnStart(string[] args)
        {
            try
            {
                _timer = new System.Timers.Timer(_intervaloMs) { AutoReset = false };
                _timer.Elapsed += Timer_Elapsed;
                _timer.Start();
                GravarLog("Serviço iniciado");

                // Dispara imediatamente sem esperar o primeiro intervalo
                _tarefaEmAndamento = Task.Run(() => ExecutarTarefa());
            }
            catch (Exception ex)
            {
                GravarLog("Falha no OnStart: " + ex);
            }
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _tarefaEmAndamento = ExecutarTarefa();
                await _tarefaEmAndamento;
            }
            catch (Exception ex)
            {
                GravarLog("Erro não tratado no Timer_Elapsed: " + ex);
            }
            finally
            {
                _timer?.Start(); // reagenda somente após concluir
            }
        }

        public async Task ExecutarTarefa()
        {
            if (!await _gate.WaitAsync(0))
            {
                GravarLog("Execução ignorada: ainda há processamento em andamento.");
                return;
            }

            try
            {
                GravarLog("Iniciando execução da tarefa");
                await MontaDPS.Envio();
                GravarLog("MontaDPS executado com sucesso");
                await MontaRPSSP.EnvioSP();
                GravarLog("MontaRPSSP executado com sucesso");
                await MontaNFESefaz.Envio();
                GravarLog("MontaNFESefaz executado com sucesso");
                GravarLog("Execução finalizada com sucesso");
            }
            catch (Exception ex)
            {
                GravarLog("Erro geral na execução: " + ex);
            }
            finally
            {
                _gate.Release();
            }
        }

        protected override void OnStop()
        {
            GravarLog("Parando serviço — aguardando execução em andamento...");

            try { _timer?.Stop(); _timer?.Dispose(); } catch { }

            // Aguarda a tarefa atual terminar antes de encerrar (até o timeout definido)
            if (!_tarefaEmAndamento.IsCompleted)
            {
                bool concluiu = _tarefaEmAndamento.Wait(_onStopTimeoutMs);
                if (!concluiu)
                    GravarLog($"Aviso: execução não concluiu dentro de {_onStopTimeoutMs / 1000}s — serviço encerrado forçadamente.");
            }

            GravarLog("Serviço parado");
        }

        private void GravarLog(string mensagem)
        {
            try
            {
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);

                // Arquivo diário: lognfse_2026-05-05.txt
                string nomeArquivo = $"lognfse_{DateTime.Now:yyyy-MM-dd}.txt";
                string arquivo = Path.Combine(_logPath, nomeArquivo);

                using (StreamWriter sw = new StreamWriter(arquivo, append: true))
                {
                    sw.WriteLine($"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {mensagem}");
                }

                LimparLogsAntigos();
            }
            catch
            {
                // Falha de log nunca deve derrubar o serviço
            }
        }

        private void LimparLogsAntigos()
        {
            try
            {
                DateTime limite = DateTime.Now.AddDays(-_logRetencaoDias);
                foreach (string arquivo in Directory.GetFiles(_logPath, "lognfse_*.txt"))
                {
                    if (File.GetLastWriteTime(arquivo) < limite)
                        File.Delete(arquivo);
                }
            }
            catch
            {
                
            }
        }
    }
}
