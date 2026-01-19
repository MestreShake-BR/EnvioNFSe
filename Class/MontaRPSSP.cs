using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NFSe.Class
{
    internal class MontaRPSSP
    {
        const string URL_PADRAO = "https://nfews.prefeitura.sp.gov.br/lotenfe.asmx";
        const string NAMESPACE_SP = "http://www.prefeitura.sp.gov.br/nfe";
        const string PASTA_LOGS_BASE = @"C:\NOTAS_FISCAIS\NFSE\LogsXML";

        public class RPDSSPService
        {
            Banco banco = new Banco();
            DataTable pendentes = new DataTable();
            RpsData rpsData = new RpsData();

            public class StringWriterWithEncoding : StringWriter
            {
                private readonly Encoding _encoding;
                public StringWriterWithEncoding(Encoding encoding) => _encoding = encoding;
                public override Encoding Encoding => _encoding;
            }

            private readonly X509Certificate2 _certificado;
            private readonly bool _ambiente;

            public RPDSSPService(X509Certificate2 certificado, bool producao = true)
            {
                _certificado = certificado;
                _ambiente = producao;
            }

            static RetornoNfseSP LerRetornoXml(string soapResponse)
            {
                var r = new RetornoNfseSP();
                try
                {
                    XmlDocument soapDoc = new XmlDocument(); soapDoc.LoadXml(soapResponse);
                    XmlNode retNode = soapDoc.GetElementsByTagName("RetornoXML")[0];
                    if (retNode == null) return r;
                    XmlDocument retDoc = new XmlDocument(); retDoc.LoadXml(retNode.InnerText);
                    r.Sucesso = retDoc.GetElementsByTagName("Sucesso")[0]?.InnerText.ToLower() == "true";
                    if (r.Sucesso)
                    {
                        var node = retDoc.GetElementsByTagName("ChaveNFe")[0];
                        r.NumeroNFe = node?["NumeroNFe"]?.InnerText ?? "";
                        r.CodigoVerificacao = node?["CodigoVerificacao"]?.InnerText ?? "";
                        r.ChaveNotaNacional = node?["ChaveNotaNacional"]?.InnerText ?? "";
                        r.Mensagem = "Sucesso";
                    }
                    else
                    {
                        var erro = retDoc.GetElementsByTagName("Erro")[0];
                        r.Mensagem = $"[{erro?["Codigo"]?.InnerText}]: {erro?["Descricao"]?.InnerText}";
                    }
                }
                catch { r.Mensagem = "Erro no Parse do Retorno"; }
                return r;
            }

            public async Task<string> EmitirNfseAsync(X509Certificate2 cert, RpsData dados)
            {
                try
                {
                    string xml = GerarXmlRPSSP(cert, dados);

                    string tagMetodo = "EnvioLoteRPSRequest";
                    string actionName = "envioLoteRPS";
                    string resposta = await EnviarSoap11(URL_PADRAO, xml, cert, tagMetodo, actionName);

                    string pastaEmitente = (dados.sCNPJPrestador == "02011984000131") ? "Monitor_Editorial" : "Monitor";
                    string baseDir = Path.Combine(PASTA_LOGS_BASE, pastaEmitente);


                    SalvarLog(baseDir, "Envio", $"Envio_RPS_{dados.nNumero}", xml);

                    SalvarLog(baseDir, "Retorno", $"Retorno_RPS_{dados.nNumero}", resposta);

                    if (!string.IsNullOrWhiteSpace(resposta))
                    {
                        try
                        {
                            var retorno = LerRetornoXml(resposta);

                            string situacao = (retorno.Sucesso || retorno.Mensagem.Contains("[224]")) ? "4" : "3";
                            string ChaveNotaNacional = retorno.ChaveNotaNacional;
                            string NumeroNFe = retorno.NumeroNFe;
                            string Mensagem = retorno.Mensagem;
                            string CodigoVerificacao = retorno.CodigoVerificacao;
                            string nNumero = dados.nNumero;
                            string iID_Emitente = dados.iID_Emitente;

                            banco.updateRPSSP(
                                ChaveNotaNacional,
                                NumeroNFe,
                                Mensagem,
                                CodigoVerificacao,
                                situacao,
                                nNumero,
                                iID_Emitente
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao ler retorno SP: {ex.Message}");
                        }
                    }

                    return resposta;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nErro: {ex.Message}");
                    Console.WriteLine($"Stack: {ex.StackTrace}");
                    throw;
                }
            }


            private string GerarXmlRPSSP(X509Certificate2 cert, RpsData dados)
            {
                var assinaturaRps = GerarAssinaturaRps(cert, dados);
                string tagTomador = dados.sCNPJCPFTomador.Length > 11 ? "CNPJ" : "CPF";

                string xmlRaw = $@"<PedidoEnvioLoteRPS xmlns=""{NAMESPACE_SP}"">
<Cabecalho Versao=""2"" xmlns="""">
    <CPFCNPJRemetente><CNPJ>{dados.sCNPJPrestador}</CNPJ></CPFCNPJRemetente>
    <transacao>true</transacao>
    <dtInicio>{DateTime.Now:yyyy-MM-dd}</dtInicio>
    <dtFim>{DateTime.Now:yyyy-MM-dd}</dtFim>
    <QtdRPS>1</QtdRPS>
</Cabecalho>
<RPS xmlns="""">
    <Assinatura>{assinaturaRps}</Assinatura>
    <ChaveRPS>
        <InscricaoPrestador>{dados.sIMPrestador}</InscricaoPrestador>
        <SerieRPS>{dados.sSerie}</SerieRPS>
        <NumeroRPS>{dados.nNumero}</NumeroRPS>
    </ChaveRPS>
    <TipoRPS>RPS</TipoRPS>
    <DataEmissao>{DateTime.Now:yyyy-MM-dd}</DataEmissao>
    <StatusRPS>N</StatusRPS>
    <TributacaoRPS>{dados.sTributacaoRPS}</TributacaoRPS>
    <ValorDeducoes>{dados.nVlDeducoes}</ValorDeducoes>
    <ValorPIS>{dados.nVlPis}</ValorPIS>
    <ValorCOFINS>{dados.nVlCofins}</ValorCOFINS>
    <ValorINSS>0.00</ValorINSS>
    <ValorIR>{dados.nVlIR}</ValorIR>
    <ValorCSLL>{dados.nVlCsll}</ValorCSLL>
    <CodigoServico>{dados.sItemListaServi.PadLeft(5, '0')}</CodigoServico>
    <AliquotaServicos>{dados.nAliquota}</AliquotaServicos>
    <ISSRetido>{(dados.iISSRetido == "1").ToString().ToLower()}</ISSRetido>
    <CPFCNPJTomador><{tagTomador}>{dados.sCNPJCPFTomador}</{tagTomador}></CPFCNPJTomador>
    <RazaoSocialTomador>{dados.sRazSociTomador}</RazaoSocialTomador>
    <EnderecoTomador>
        <Logradouro>{dados.sEndTomador}</Logradouro>
        <NumeroEndereco>{dados.sNumeroToma}</NumeroEndereco>
        <Bairro>{dados.sBairroToma}</Bairro>
        <Cidade>{dados.sCdMunicToma}</Cidade>
        <UF>{dados.sUFToma}</UF>
        <CEP>{dados.sCEPTomador}</CEP>
    </EnderecoTomador>
    <Discriminacao>{dados.sDiscriminacao}</Discriminacao>
    <ValorInicialCobrado>{dados.ValorInicialCobrado}</ValorInicialCobrado>
    <ValorIPI>0.00</ValorIPI>
    <ExigibilidadeSuspensa>0</ExigibilidadeSuspensa>
    <PagamentoParceladoAntecipado>0</PagamentoParceladoAntecipado>
    <NBS>000000000</NBS>
    <cLocPrestacao>3550308</cLocPrestacao>
    <IBSCBS>
        <finNFSe>{dados.finNFSe}</finNFSe>
        <indFinal>{dados.indFinal}</indFinal>
        <cIndOp>{dados.cIndOp}</cIndOp>
        <indDest>{dados .indDest}</indDest>
        <valores>
            <trib>
                <gIBSCBS>
                    <cClassTrib>{dados.cClassTrib}</cClassTrib>
                </gIBSCBS>
            </trib>
        </valores>
    </IBSCBS>
</RPS>
</PedidoEnvioLoteRPS>";

                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = false;
                doc.LoadXml(xmlRaw);

                return AssinarXml(doc, cert);
            }
            static string GerarAssinaturaRps(X509Certificate2 cert, RpsData rps)
            {
                string im = rps.sIMPrestador.PadLeft(12, '0');
                string serie = rps.sSerie.PadRight(5, ' ');
                string num = rps.nNumero.PadLeft(12, '0');
                string data = DateTime.Now.ToString("yyyyMMdd");
                string iss = rps.iISSRetido == "1" ? "S" : "N";
                string vServ = ((long)Math.Round(rps.ValorInicialCobrado * 100)).ToString().PadLeft(15, '0');
                //string vDed = ((long)Math.Round(rps.nVlDeducoes * 100)).ToString().PadLeft(15, '0');
                string vDed = ((long)Math.Round(rps.nVlDeducoes * 100)).ToString().PadLeft(15, '0');
                string cod = rps.sItemListaServi.PadLeft(5, '0');
                string tTom = rps.sCNPJCPFTomador.Length > 11 ? "2" : "1";
                string docTom = rps.sCNPJCPFTomador.PadLeft(14, '0');

                string texto = im + serie + num + data + rps.sTributacaoRPS + "N" + iss + vServ + vDed + cod + tTom + docTom;
                var rsa = cert.GetRSAPrivateKey();
                return Convert.ToBase64String(rsa.SignData(Encoding.ASCII.GetBytes(texto), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
            }
            static string AssinarXml(XmlDocument doc, X509Certificate2 cert)
            {
                SignedXml signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                Reference reference = new Reference { Uri = "" };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.AddTransform(new XmlDsigC14NTransform());
                reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";
                signedXml.AddReference(reference);
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;
                signedXml.ComputeSignature();
                doc.DocumentElement.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
                return doc.OuterXml;
            }
            static async Task<string> EnviarSoap11(string url, string xmlAssinado, X509Certificate2 cert, string tagMetodo, string actionName)
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);
                var client = new HttpClient(handler);
                string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                  <soap:Body>
                    <{tagMetodo} xmlns=""{NAMESPACE_SP}"">
                      <VersaoSchema>2</VersaoSchema>
                      <MensagemXML>{System.Web.HttpUtility.HtmlEncode(xmlAssinado)}</MensagemXML>
                    </{tagMetodo}>
                  </soap:Body>
                </soap:Envelope>";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(soap, Encoding.UTF8, "text/xml");
                request.Headers.TryAddWithoutValidation("SOAPAction", $"{NAMESPACE_SP}/ws/{actionName}");
                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }                       

            static void SalvarLog(string baseDir, string subFolder, string id, string content)
            {
                try
                {
                    string path = Path.Combine(baseDir, subFolder);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    File.WriteAllText(Path.Combine(path, $"{DateTime.Now:yyyyMMdd_HHmmss}_{id}.xml"), content, Encoding.UTF8);
                }
                catch { }
            }
        }

        public static async Task EnvioSP()
        {
            try
            {
                Banco banco = new Banco();
                DataTable dps = banco.selectRPSSP();

                if (dps.Rows.Count > 0)
                {
                    foreach (DataRow item in dps.Rows)
                    {
                        var dados1 = banco.ConvertToDadosSP(item);

                        var dados = new RpsData
                        {
                            iID_Emitente = dados1.iID_Emitente,
                            nNumero = dados1.nNumero,
                            sSerie = dados1.sSerie,
                            sCdMunicipio = dados1.sCdMunicipio,
                            sCNPJPrestador = dados1.sCNPJPrestador,
                            sIMPrestador = dados1.sIMPrestador,
                            sOpSimples = dados1.sOpSimples,

                            // Tomador
                            iIndCPFCNPJToma = dados1.iIndCPFCNPJToma,
                            sCNPJCPFTomador = dados1.sCNPJCPFTomador,
                            sRazSociTomador = dados1.sRazSociTomador,
                            sEndTomador = dados1.sEndTomador,
                            sNumeroToma = dados1.sNumeroToma,
                            sCompToma = dados1.sCompToma,
                            sBairroToma = dados1.sBairroToma,
                            sCdMunicToma = dados1.sCdMunicToma,
                            sUFToma = dados1.sUFToma,
                            sCEPTomador = dados1.sCEPTomador,
                            sEmailTomador = dados1.sEmailTomador,

                            // Serviço
                            sItemListaServi = dados1.sItemListaServi,
                            sDiscriminacao = dados1.sDiscriminacao,
                            nVlServicos = dados1.nVlServicos,
                            nAliquota = dados1.nAliquota,
                            nBaseCalculo = dados1.nBaseCalculo,
                            iISSRetido = dados1.iISSRetido,
                            nVlIR = dados1.nVlIR,
                            nVlPis = dados1.nVlPis,
                            nVlCofins = dados1.nVlCofins,
                            nVlDeducoes = dados1.nVlDeducoes,
                            nVlCsll = dados1.nVlCsll,

                            // Faltou
                            sTributacaoRPS = dados1.sTributacaoRPS,
                            ValorInicialCobrado = dados1.ValorInicialCobrado,
                            finNFSe = dados1.finNFSe,
                            indFinal = dados1.indFinal,
                            cIndOp = dados1.cIndOp,
                            indDest = dados1.indDest,
                            cClassTrib = dados1.cClassTrib
                        };

                        X509Certificate2 certificado = CarregarCertificado(dados1.iID_Emitente);

                        var service = new RPDSSPService(certificado, producao: true);
                        string resposta = await service.EmitirNfseAsync(certificado, dados);

                        Console.WriteLine(resposta);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mensagem: {ex.Message}");
            }
        }

        static X509Certificate2 CarregarCertificado(string emitente)
        {
            string CERT_PATH = "";
            string CERT_PASSWORD = "";

            if (emitente == "")
            {
                CERT_PATH = "";
                CERT_PASSWORD = "";
            }
            try
            {
                var certificado = new X509Certificate2(CERT_PATH, CERT_PASSWORD, X509KeyStorageFlags.Exportable);
                if (!certificado.HasPrivateKey) {
                    throw new Exception("Certificado não possui chave privada");
                    
                }
                    

                Console.WriteLine($"Certificado válido: {certificado.Subject}");
                Console.WriteLine($"Válido de: {certificado.NotBefore} até: {certificado.NotAfter}");
                return certificado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar certificado: {ex.Message}");
            }
        }
        public void GravarLog(string mensagem)
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
