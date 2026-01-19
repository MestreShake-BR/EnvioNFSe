using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NFSe.Class
{
    internal class MontaDPS
    {
        public class NfseNacionalService
        {
            Banco banco = new Banco();
            DataTable pendentes = new DataTable();
            DadosNfse DadosNfse = new DadosNfse();
            PrestadorNfse PrestadorNfse = new PrestadorNfse();
            TomadorNfse TomadorNfse = new TomadorNfse();
            ServicoNfse servicoNfse = new ServicoNfse();


            public class StringWriterWithEncoding : StringWriter
            {
                private readonly Encoding _encoding;
                public StringWriterWithEncoding(Encoding encoding) => _encoding = encoding;
                public override Encoding Encoding => _encoding;
            }

            private readonly string _urlProducao = "https://sefin.nfse.gov.br/SefinNacional/nfse";
            private readonly X509Certificate2 _certificado;
            private readonly bool _ambiente;

            public NfseNacionalService(X509Certificate2 certificado, bool producao = true)
            {
                _certificado = certificado;
                _ambiente = producao;
            }

            public async Task<string> EmitirNfseAsync(DadosNfse dados)
            {
                try
                {
                    string xml = GerarXmlDps(dados);
                    string xmlAssinado = AssinarXml(xml, dados.NumeroRps);
                    string xmlCompactado = CompactarXmlParaBase64(xmlAssinado);
                    string url = _urlProducao;

                    string resposta = await EnviarParaWebservice(url, xmlCompactado);

                    if (!string.IsNullOrWhiteSpace(resposta))
                    {
                        try
                        {
                            var retorno = System.Text.Json.JsonSerializer.Deserialize<RetornoNfse>(resposta);

                            if (retorno != null)
                            {
                                // DADO QUE JÁ TEM NO BANCO
                                string numeroRPS = dados.NumeroRps;
                                // DADOS DO RETORNO DO JSON
                                int tipoAmbiente = retorno.tipoAmbiente;
                                string versaoAplicativo = retorno.versaoAplicativo;
                                DateTime dataHoraProcessamento = retorno.dataHoraProcessamento;
                                string idDps = retorno.idDps;
                                string chaveAcesso = retorno.chaveAcesso;
                                string nfseXmlGZipB64 = retorno.nfseXmlGZipB64;

                                string xmlDescompactado = DescompactarBase64ParaXml(nfseXmlGZipB64);
                                var doc = XDocument.Parse(xmlDescompactado);
                                XNamespace ns = "http://www.sped.fazenda.gov.br/nfse";
                                string numeroNFSe = doc.Descendants(ns + "nNFSe")
                                                        .FirstOrDefault()?.Value;

                                Console.WriteLine(numeroNFSe);


                                Banco banco = new Banco();

                                banco.updateRPS(chaveAcesso, numeroNFSe, numeroRPS, dados.Emitente);
                            }
                            else
                            {
                                Console.WriteLine("Não foi possível converter o retorno JSON.");
                            }
                        }
                        catch (Exception exJson)
                        {
                            Console.WriteLine($"Erro ao ler o JSON: {exJson.Message}");
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
            private string GerarXmlDps(DadosNfse dados)
            {
                string id = "DPS" + dados.CodigoMunicipioEmissao + "2" + dados.Prestador.Cnpj + dados.Serie.PadLeft(5, '0') + dados.NumeroRps.PadLeft(15, '0');

                StringBuilder xmlBuilder = new StringBuilder();
                xmlBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                xmlBuilder.AppendLine("<DPS xmlns=\"http://www.sped.fazenda.gov.br/nfse\" versao=\"1.00\">");
                xmlBuilder.AppendLine($"<infDPS Id=\"{id}\">");
                xmlBuilder.AppendLine($"<tpAmb>1</tpAmb>");
                xmlBuilder.AppendLine($"<dhEmi>{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")}</dhEmi>");
                xmlBuilder.AppendLine($"<verAplic>Teste IM 1.0</verAplic>");
                xmlBuilder.AppendLine($"<serie>{dados.Serie.PadLeft(5, '0')}</serie>");
                xmlBuilder.AppendLine($"<nDPS>{dados.NumeroRps}</nDPS>");
                xmlBuilder.AppendLine($"<dCompet>{dados.DataCompetencia.ToString("yyyy-MM-dd")}</dCompet>");
                xmlBuilder.AppendLine($"<tpEmit>1</tpEmit>");
                xmlBuilder.AppendLine($"<cLocEmi>{dados.CodigoMunicipioEmissao}</cLocEmi>");
                xmlBuilder.AppendLine($"<prest>");
                xmlBuilder.AppendLine($"<CNPJ>{LimparCnpj(dados.Prestador.Cnpj)}</CNPJ>");
                xmlBuilder.AppendLine($"<regTrib>");
                if (dados.Prestador.OpcaoSimplesNacional == "1")
                {
                    xmlBuilder.AppendLine($"<opSimpNac>3</opSimpNac>");
                }
                else
                {
                    if (dados.Emitente == "10")
                    {
                        xmlBuilder.AppendLine($"<opSimpNac>1</opSimpNac>");
                    }
                    else
                    {
                        xmlBuilder.AppendLine($"<opSimpNac>2</opSimpNac>");
                    }
                }
                if (dados.Emitente != "10")
                {
                    xmlBuilder.AppendLine($"<regApTribSN>{dados.Prestador.RegimeAplicacaoTributacaoSN}</regApTribSN>");
                }

                if (dados.Emitente == "10")
                {
                    xmlBuilder.AppendLine($"<regEspTrib>0</regEspTrib>");
                }
                else
                {
                    xmlBuilder.AppendLine($"<regEspTrib>{dados.Prestador.RegimeEspecialTributacao}</regEspTrib>");
                }
                xmlBuilder.AppendLine($"</regTrib>");
                xmlBuilder.AppendLine($"</prest>");
                xmlBuilder.AppendLine($"<toma>");
                if (dados.Tomador.TipoPessoa == "1")
                    xmlBuilder.AppendLine($"<CPF>{LimparCpfCnpj(dados.Tomador.CpfCnpj)}</CPF>");
                else
                    xmlBuilder.AppendLine($"<CNPJ>{LimparCpfCnpj(dados.Tomador.CpfCnpj)}</CNPJ>");
                xmlBuilder.AppendLine($"<xNome>{dados.Tomador.Nome}</xNome>");
                xmlBuilder.AppendLine($"<end>");
                xmlBuilder.AppendLine($"<endNac>");
                xmlBuilder.AppendLine($"<cMun>{dados.Tomador.Endereco.CodigoMunicipio}</cMun>");
                xmlBuilder.AppendLine($"<CEP>{dados.Tomador.Endereco.Cep}</CEP>");
                xmlBuilder.AppendLine($"</endNac>");
                xmlBuilder.AppendLine($"<xLgr>{dados.Tomador.Endereco.Endereco}</xLgr>");
                xmlBuilder.AppendLine($"<nro>{dados.Tomador.Endereco.Numero}</nro>");
                if (!string.IsNullOrEmpty(dados.Tomador.Endereco.Complemento))
                {
                    xmlBuilder.AppendLine($"<xCpl>{dados.Tomador.Endereco.Complemento}</xCpl>");
                }
                xmlBuilder.AppendLine($"<xBairro>{dados.Tomador.Endereco.Bairro}</xBairro>");
                xmlBuilder.AppendLine($"</end>");
                xmlBuilder.AppendLine($"</toma>");
                xmlBuilder.AppendLine($"<serv>");
                xmlBuilder.AppendLine($"<locPrest>");
                xmlBuilder.AppendLine($"<cLocPrestacao>{dados.Servico.CodigoMunicipioPrestacao}</cLocPrestacao>");
                xmlBuilder.AppendLine($"</locPrest>");
                xmlBuilder.AppendLine($"<cServ>");
                if (dados.Emitente == "10")
                {
                    xmlBuilder.AppendLine($"<cTribNac>{dados.Servico.CodigoTributacao.PadLeft(6, '0')}</cTribNac>");
                    xmlBuilder.AppendLine($"<cTribMun>002</cTribMun>");
                }
                else
                {
                    xmlBuilder.AppendLine($"<cTribNac>{dados.Servico.CodigoTributacao.PadLeft(6, '0')}</cTribNac>");
                }
                xmlBuilder.AppendLine($"<xDescServ>{dados.Servico.DescricaoServico}</xDescServ>");
                xmlBuilder.AppendLine($"</cServ>");
                xmlBuilder.AppendLine($"</serv>");
                xmlBuilder.AppendLine($"<valores>");
                xmlBuilder.AppendLine($"<vServPrest>");
                xmlBuilder.AppendLine($"<vServ>{dados.Servico.ValorServico}</vServ>");
                xmlBuilder.AppendLine($"</vServPrest>");
                xmlBuilder.AppendLine($"<trib>");
                xmlBuilder.AppendLine($"<tribMun>");
                xmlBuilder.AppendLine($"<tribISSQN>1</tribISSQN>");
                xmlBuilder.AppendLine($"<tpRetISSQN>1</tpRetISSQN>");
                xmlBuilder.AppendLine($"</tribMun>");
                //TRIBUTAÇÃO FEDERAL
                xmlBuilder.AppendLine($"<tribFed>");
                xmlBuilder.AppendLine($"<piscofins>");
                xmlBuilder.AppendLine($"<CST>01</CST>");
                xmlBuilder.AppendLine($"<vBCPisCofins>{dados.Servico.BaseCalculo}</vBCPisCofins>");
                xmlBuilder.AppendLine($"<pAliqPis>{dados.Servico.ALIQ_PIS}</pAliqPis>");
                xmlBuilder.AppendLine($"<pAliqCofins>{dados.Servico.ALIQ_COFINS}</pAliqCofins>");
                xmlBuilder.AppendLine($"<vPis>{dados.Servico.ValorPIS}</vPis>");
                xmlBuilder.AppendLine($"<vCofins>{dados.Servico.ValorCofins}</vCofins>");
                xmlBuilder.AppendLine($"<tpRetPisCofins>2</tpRetPisCofins>");
                xmlBuilder.AppendLine($"</piscofins>");
                xmlBuilder.AppendLine($"</tribFed>");
                xmlBuilder.AppendLine($"<totTrib>");
                xmlBuilder.AppendLine($"<pTotTrib>");
                xmlBuilder.AppendLine($"<pTotTribFed>1.00</pTotTribFed>");
                xmlBuilder.AppendLine($"<pTotTribEst>1.00</pTotTribEst>");
                xmlBuilder.AppendLine($"<pTotTribMun>1.00</pTotTribMun>");
                xmlBuilder.AppendLine($"</pTotTrib>");
                xmlBuilder.AppendLine($"</totTrib>");
                xmlBuilder.AppendLine($"</trib>");
                xmlBuilder.AppendLine($"</valores>");

                xmlBuilder.AppendLine($"</infDPS>");
                xmlBuilder.Append($"</DPS>");

                return xmlBuilder.ToString();
            }
            private string LimparCnpj(string cnpj)
            {
                return new string(cnpj?.Where(char.IsDigit).ToArray());
            }
            private string LimparCpfCnpj(string cpfCnpj)
            {
                return new string(cpfCnpj?.Where(char.IsDigit).ToArray());
            }
            private string AssinarXml(string xml, string numeroRps)
            {

                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(xml);
                XmlElement infDps = doc.DocumentElement["infDPS"];
                if (infDps == null)
                    throw new Exception("Elemento infDPS não encontrado para assinatura");

                string id = infDps.GetAttribute("Id");

                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = _certificado.GetRSAPrivateKey();
                Reference reference = new Reference($"#{id}");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.AddTransform(new XmlDsigC14NTransform());
                reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";
                signedXml.AddReference(reference);
                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(_certificado));
                signedXml.KeyInfo = keyInfo;
                signedXml.ComputeSignature();
                XmlElement xmlDigitalSignature = signedXml.GetXml();
                xmlDigitalSignature.SetAttribute("xmlns", "http://www.w3.org/2000/09/xmldsig#");
                XmlDocument signedDoc = new XmlDocument { PreserveWhitespace = true };
                signedDoc.LoadXml(doc.OuterXml);
                XmlElement importedSignature = (XmlElement)signedDoc.ImportNode(xmlDigitalSignature, true);
                signedDoc.DocumentElement.AppendChild(importedSignature);

                using (var sw = new StringWriterWithEncoding(new UTF8Encoding(false)))
                using (var writer = XmlWriter.Create(sw, new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = false,
                    OmitXmlDeclaration = false
                }))
                {
                    signedDoc.Save(writer);
                    string result = sw.ToString();

                    if (!result.Contains("xmlns=\"http://www.w3.org/2000/09/xmldsig#\""))
                    {
                        result = result.Replace("<Signature>", "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");
                    }

                    return result;
                }
            }
            private string SerializarElemento(XmlElement element, int indentLevel)
            {
                StringBuilder sb = new StringBuilder();
                string indent = new string(' ', indentLevel * 2);

                sb.Append(indent);
                sb.Append("<");
                sb.Append(element.LocalName);

                foreach (XmlAttribute attr in element.Attributes)
                {
                    sb.Append(" ");
                    sb.Append(attr.Name);
                    sb.Append("=\"");
                    sb.Append(attr.Value);
                    sb.Append("\"");
                }

                if (element.HasChildNodes)
                {
                    bool hasElementChildren = false;
                    foreach (XmlNode child in element.ChildNodes)
                    {
                        if (child.NodeType == XmlNodeType.Element)
                        {
                            hasElementChildren = true;
                            break;
                        }
                    }

                    if (hasElementChildren)
                    {
                        sb.AppendLine(">");

                        foreach (XmlNode child in element.ChildNodes)
                        {
                            if (child.NodeType == XmlNodeType.Element)
                            {
                                sb.AppendLine(SerializarElemento((XmlElement)child, indentLevel + 1));
                            }
                            else if (child.NodeType == XmlNodeType.Text)
                            {
                                sb.Append(child.InnerText);
                            }
                        }

                        sb.Append(indent);
                        sb.Append("</");
                        sb.Append(element.LocalName);
                        sb.Append(">");
                    }
                    else
                    {
                        sb.Append(">");
                        sb.Append(element.InnerText);
                        sb.Append("</");
                        sb.Append(element.LocalName);
                        sb.Append(">");
                    }
                }
                else
                {
                    sb.Append(" />");
                }

                return sb.ToString();
            }
            private string CompactarXmlParaBase64(string xml)
            {
                byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        gzipStream.Write(xmlBytes, 0, xmlBytes.Length);
                    }

                    byte[] compressedBytes = memoryStream.ToArray();
                    string base64 = Convert.ToBase64String(compressedBytes);
                    return base64;
                }
            }
            public static string DescompactarBase64ParaXml(string base64Compressed)
            {
                try
                {
                    byte[] compressedBytes = Convert.FromBase64String(base64Compressed);

                    using (var memoryStream = new MemoryStream(compressedBytes))
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    using (var resultStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(resultStream);
                        byte[] decompressedBytes = resultStream.ToArray();
                        return Encoding.UTF8.GetString(decompressedBytes);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Erro ao descompactar XML: {ex.Message}", ex);
                }
            }
            private async Task<string> EnviarParaWebservice(string url, string xmlCompactado)
            {

                var handler = new HttpClientHandler
                {
                    ClientCertificates = { _certificado },
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                    {
                        return true;
                    }
                };

                using (handler)
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(120);
                    var payload = new
                    {
                        dpsXmlGZipB64 = xmlCompactado
                    };

                    var options = new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var jsonContent = JsonSerializer.Serialize(payload, options);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    Console.WriteLine($"Tamanho do payload: {jsonContent.Length} bytes");

                    try
                    {
                        var response = await client.PostAsync(url, content);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        Console.WriteLine("Headers da resposta POST:");
                        foreach (var header in response.Headers)
                        {
                            Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                        }

                        Console.WriteLine($"Resposta: {responseBody}");

                        return responseBody;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro no POST: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public static async Task Envio()
        {
            try
            {
                Banco banco = new Banco();
                DataTable dps = banco.selectRPS();

                if (dps.Rows.Count > 0)
                {
                    foreach (DataRow item in dps.Rows)
                    {
                        var dados1 = banco.ConvertToDados(item);

                        var dados = new DadosNfse
                        {
                            Emitente = dados1.Emitente,
                            NumeroRps = dados1.NumeroRps,
                            Serie = dados1.Serie,
                            DataEmissao = dados1.DataEmissao,
                            DataCompetencia = dados1.DataCompetencia,
                            CodigoMunicipioEmissao = dados1.CodigoMunicipioEmissao,
                            Prestador = new PrestadorNfse
                            {
                                Cnpj = dados1.Prestador.Cnpj,
                                InscricaoMunicipal = dados1.Prestador.InscricaoMunicipal,
                                OpcaoSimplesNacional = dados1.Prestador.OpcaoSimplesNacional,
                                RegimeAplicacaoTributacaoSN = dados1.Prestador.RegimeAplicacaoTributacaoSN,
                                RegimeEspecialTributacao = dados1.Prestador.RegimeEspecialTributacao
                            },
                            Tomador = new TomadorNfse
                            {
                                TipoPessoa = dados1.Tomador.TipoPessoa,
                                CpfCnpj = dados1.Tomador.CpfCnpj,
                                Nome = dados1.Tomador.Nome,
                                Endereco = new EnderecoNfse
                                {
                                    Endereco = dados1.Tomador.Endereco.Endereco,
                                    Numero = dados1.Tomador.Endereco.Numero,
                                    Complemento = dados1.Tomador.Endereco.Complemento,
                                    Bairro = dados1.Tomador.Endereco.Bairro,
                                    CodigoMunicipio = dados1.Tomador.Endereco.CodigoMunicipio,
                                    Uf = dados1.Tomador.Endereco.Uf,
                                    Cep = dados1.Tomador.Endereco.Cep
                                },
                                Contato = new ContatoNfse
                                {
                                    Email = dados1.Tomador.Contato.Email
                                }

                            },
                            Servico = new ServicoNfse
                            {
                                CodigoTributacao = dados1.Servico.CodigoTributacao,
                                DescricaoServico = dados1.Servico.DescricaoServico,
                                CodigoMunicipioPrestacao = dados1.Servico.CodigoMunicipioPrestacao,
                                ValorServico = dados1.Servico.ValorServico,
                                TipoRetencaoISSQN = dados1.Servico.TipoRetencaoISSQN,
                                ValorServicolq = dados1.Servico.ValorServicolq,
                                BaseCalculo = dados1.Servico.BaseCalculo,
                                Aliquota = dados1.Servico.Aliquota,
                                ValorISS = dados1.Servico.ValorISS,
                                ValorPIS = dados1.Servico.ValorPIS,
                                ValorCofins = dados1.Servico.ValorCofins,
                                ValorDeducoes = dados1.Servico.ValorDeducoes,
                                BaseDeCalculoRetencoes = dados1.Servico.BaseDeCalculoRetencoes,
                                PercentualIR = dados1.Servico.PercentualIR,
                                ValorIR = dados1.Servico.ValorIR,
                                PercentualContribuicaoSocial = dados1.Servico.PercentualContribuicaoSocial,
                                ValorContribuicaoSocial = dados1.Servico.ValorContribuicaoSocial,
                                CSTPIS = dados1.Servico.CSTPIS,
                                CSTCOFINS = dados1.Servico.CSTCOFINS,
                                ALIQ_PIS = dados1.Servico.ALIQ_PIS,
                                ALIQ_COFINS = dados1.Servico.ALIQ_COFINS,
                                ALIQ_ISS = dados1.Servico.ALIQ_ISS
                            }
                        };

                        X509Certificate2 certificado = CarregarCertificado(dados1.Emitente);

                        var service = new NfseNacionalService(certificado, producao: true);
                        string resposta = await service.EmitirNfseAsync(dados);

                        Console.WriteLine(resposta);
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== ERRO ===");
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
            else if (emitente == "")
            {
                CERT_PATH = "";
                CERT_PASSWORD = "";
            }        
            try
            {
                var certificado = new X509Certificate2(CERT_PATH, CERT_PASSWORD, X509KeyStorageFlags.Exportable);
                if (!certificado.HasPrivateKey)
                    throw new Exception("Certificado não possui chave privada");

                Console.WriteLine($"Certificado válido: {certificado.Subject}");
                Console.WriteLine($"Válido de: {certificado.NotBefore} até: {certificado.NotAfter}");

                return certificado;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao carregar certificado: {ex.Message}");
            }
        }
    }
}
