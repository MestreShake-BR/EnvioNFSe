using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NFSe.Class
{
    internal class MontaNFESefaz
    {
        Banco banco = new Banco();

        public async Task<string> EmitirSEFAZAsync(PedidoNfe pedido)
        {


            try
            {
                PedidoNfe pedidos = pedido;
                bool producao =  true;
                string pfx = @"";
                string senha = "";

                int idNota = pedido.iIdNotaFisc;

                if (pedido == null) throw new Exception($"Nota {idNota} não encontrada");


                string pastaEmitente = (pedido.Emitente.CNPJ == "") ? "" : "";
                
                string baseDir = $@"";

                Directory.CreateDirectory($@"{baseDir}\Envio");
                Directory.CreateDirectory($@"{baseDir}\Retorno");
                Directory.CreateDirectory($@"{baseDir}\Autorizadas");

                //3.GERAÇÃO E ASSINATURA
                string cNF = new Random().Next(10000000, 99999999).ToString();
                string chave = GerarChave(pedido, cNF);
                XmlDocument xml = GerarXmlNfe(pedido, producao, cNF, chave);

                AssinarXml(xml, pfx, senha);
                File.WriteAllText($@"{baseDir}\Envio\{chave}-nfe.xml", PrettyPrintXml(xml.OuterXml), Encoding.UTF8);

                //4.ENVIO SEFAZ
                string retornoSefaz = EnviarNfe(xml, producao, pfx, senha);
                File.WriteAllText($@"{baseDir}\Retorno\{chave}-nferet.xml", PrettyPrintXml(retornoSefaz), Encoding.UTF8);

                //5.XML DISTRIBUIÇÃO
                string xmlProc = GerarXmlDistribuicao(xml, retornoSefaz);
                if (!string.IsNullOrEmpty(xmlProc))
                {
                    File.WriteAllText($@"{baseDir}\Autorizadas\{chave}-procNFe.xml", PrettyPrintXml(xmlProc), Encoding.UTF8);
                }

                //6.ATUALIZAÇÃO NO SERVIDOR CORRETO
                banco.updateNFESefaz(idNota, retornoSefaz, chave);

                return "0";
            }
            catch (Exception)
            {
                return "1";
            }
        }

        static string GerarXmlDistribuicao(XmlDocument xmlAssinado, string xmlRetorno)
        {
            try
            {
                XmlDocument docRetorno = new XmlDocument();
                docRetorno.LoadXml(xmlRetorno);
                XmlNode protNFe = docRetorno.GetElementsByTagName("protNFe").Item(0);
                if (protNFe == null) return null;
                StringBuilder sb = new StringBuilder();
                sb.Append(@"<nfeProc xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">");
                sb.Append(xmlAssinado.GetElementsByTagName("NFe").Item(0).OuterXml);
                sb.Append(protNFe.OuterXml);
                sb.Append("</nfeProc>");
                return sb.ToString();
            }
            catch 
            { 
                return null; 
            }
        }

        static XmlDocument GerarXmlNfe(PedidoNfe p, bool prod, string cNF, string chave)
        {
            string dv = chave.Substring(chave.Length - 1);
            decimal vP = p.Produtos.Sum(x => x.ValorProd);
            decimal vT = vP + p.ValorFrete + p.ValorSeguro - p.ValorDesconto;
            string idDest = (p.Emitente.UF.Trim().ToUpper() == p.Destinatario.UF.Trim().ToUpper()) ? "1" : "2";

            StringBuilder xmlPr = new StringBuilder();
            foreach (var pr in p.Produtos)
            {
                xmlPr.Append($@"<det nItem=""{pr.Item + 1}""><prod><cProd>{pr.Codigo}</cProd><cEAN>SEM GTIN</cEAN><xProd>{pr.Descricao}</xProd><NCM>{pr.NCM}</NCM><CFOP>{pr.CFOP}</CFOP><uCom>{pr.Unidade}</uCom><qCom>{pr.Quantidade:F4}</qCom><vUnCom>{pr.ValorUnitario:F10}</vUnCom><vProd>{pr.ValorProd:F2}</vProd><cEANTrib>SEM GTIN</cEANTrib><uTrib>{pr.Unidade}</uTrib><qTrib>{pr.Quantidade:F4}</qTrib><vUnTrib>{pr.ValorUnitario:F10}</vUnTrib><indTot>1</indTot></prod><imposto><ICMS><ICMS40><orig>0</orig><CST>41</CST></ICMS40></ICMS><PIS><PISNT><CST>06</CST></PISNT></PIS><COFINS><COFINSNT><CST>06</CST></COFINSNT></COFINS></imposto></det>");
            }

            string d = p.Destinatario.CPF?.Trim() ?? "";
            bool isCNPJ = d.Length == 14;
            bool temIE = !string.IsNullOrWhiteSpace(p.Destinatario.IE);

            string xmlS = $@"<enviNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00""><idLote>1</idLote><indSinc>1</indSinc><NFe xmlns=""http://www.portalfiscal.inf.br/nfe""><infNFe versao=""4.00"" Id=""NFe{chave}""><ide><cUF>35</cUF><cNF>{cNF}</cNF><natOp>{p.NatOp}</natOp><mod>{p.Modelo}</mod><serie>{p.Serie}</serie><nNF>{(int)p.InNF}</nNF><dhEmi>{p.DataEmissao:yyyy-MM-ddTHH:mm:sszzz}</dhEmi><tpNF>1</tpNF><idDest>{idDest}</idDest><cMunFG>{p.Emitente.CodigoMunicipio}</cMunFG><tpImp>1</tpImp><tpEmis>1</tpEmis><cDV>{dv}</cDV><tpAmb>{(prod ? 1 : 2)}</tpAmb><finNFe>1</finNFe><indFinal>1</indFinal><indPres>0</indPres><procEmi>0</procEmi><verProc>1.0</verProc></ide><emit><CNPJ>{p.Emitente.CNPJ}</CNPJ><xNome>{p.Emitente.RazaoSocial}</xNome><enderEmit><xLgr>{p.Emitente.Logradouro}</xLgr><nro>{p.Emitente.Numero}</nro><xBairro>{p.Emitente.Bairro}</xBairro><cMun>{p.Emitente.CodigoMunicipio}</cMun><xMun>{p.Emitente.Municipio}</xMun><UF>{p.Emitente.UF}</UF><CEP>{p.Emitente.CEP}</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>{p.Emitente.Telefone}</fone></enderEmit><IE>{p.Emitente.IE}</IE><CRT>{p.Emitente.CRT}</CRT></emit><dest>{(isCNPJ ? $"<CNPJ>{d}</CNPJ>" : $"<CPF>{d}</CPF>")}<xNome>{p.Destinatario.Nome}</xNome><enderDest><xLgr>{p.Destinatario.Logradouro}</xLgr><nro>{p.Destinatario.Numero}</nro><xBairro>{p.Destinatario.Bairro}</xBairro><cMun>{p.Destinatario.CodigoMunicipio}</cMun><xMun>{p.Destinatario.Municipio}</xMun><UF>{p.Destinatario.UF}</UF><CEP>{p.Destinatario.CEP}</CEP><cPais>1058</cPais><xPais>BRASIL</xPais></enderDest><indIEDest>{(!isCNPJ ? 9 : (temIE ? 1 : 2))}</indIEDest>{(temIE ? $"<IE>{p.Destinatario.IE}</IE>" : "")}</dest>{xmlPr}<total><ICMSTot><vBC>0.00</vBC><vICMS>0.00</vICMS><vICMSDeson>0.00</vICMSDeson><vFCP>0.00</vFCP><vBCST>0.00</vBCST><vST>0.00</vST><vFCPST>0.00</vFCPST><vFCPSTRet>0.00</vFCPSTRet><vProd>{vP:F2}</vProd><vFrete>{p.ValorFrete:F2}</vFrete><vSeg>{p.ValorSeguro:F2}</vSeg><vDesc>{p.ValorDesconto:F2}</vDesc><vII>0.00</vII><vIPI>0.00</vIPI><vIPIDevol>0.00</vIPIDevol><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro><vNF>{vT:F2}</vNF></ICMSTot></total><transp><modFrete>9</modFrete></transp><pag><detPag><tPag>15</tPag><vPag>{vT:F2}</vPag></detPag></pag></infNFe></NFe></enviNFe>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlS);
            return doc;
        }
       
        public void AssinarXml(XmlDocument xml, string pfx, string senha)
        {
            X509Certificate2 cert = new X509Certificate2(pfx, senha, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            XmlElement inf = (XmlElement)xml.GetElementsByTagName("infNFe")[0];
            SignedXml sXml = new SignedXml(xml) { SigningKey = cert.GetRSAPrivateKey() };
            sXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            Reference r = new Reference("#" + inf.GetAttribute("Id"));
            r.AddTransform(new XmlDsigEnvelopedSignatureTransform()); r.AddTransform(new XmlDsigC14NTransform());
            r.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";
            sXml.AddReference(r);
            KeyInfo k = new KeyInfo(); k.AddClause(new KeyInfoX509Data(cert)); sXml.KeyInfo = k;
            sXml.ComputeSignature();
            inf.ParentNode.AppendChild(xml.ImportNode(sXml.GetXml(), true));
        }

        public string EnviarNfe(XmlDocument xml, bool prod, string pfx, string senha)
        {
            string url = prod ? "https://nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx" : "https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx";
            string soap = $@"<?xml version=""1.0"" encoding=""UTF-8""?><soap12:Envelope xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope""><soap12:Body><nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4"">{xml.DocumentElement.OuterXml}</nfeDadosMsg></soap12:Body></soap12:Envelope>";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST"; req.ContentType = "application/soap+xml; charset=utf-8";
            req.ClientCertificates.Add(new X509Certificate2(pfx, senha, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable));
            byte[] b = Encoding.UTF8.GetBytes(soap); req.ContentLength = b.Length;
            using (var st = req.GetRequestStream()) st.Write(b, 0, b.Length);
            var rs = req.GetResponse();
            var sr = new StreamReader(rs.GetResponseStream(), Encoding.UTF8);
            return sr.ReadToEnd();
        }

        public string GerarChave(PedidoNfe p, string cNF)
        {
            string cj = p.Emitente.CNPJ.Replace(".", "").Replace("-", "");
            string b = "35" + p.DataEmissao.ToString("yyMM") + cj + "55" + p.Serie.ToString("000") + ((int)p.InNF).ToString("000000000") + "1" + cNF;
            return b + CalcularDV(b);
        }

        public int CalcularDV(string c) { 
            int s = 0, p = 2; 
            for (int i = c.Length - 1; i >= 0; i--) 
            { s += (c[i] - '0') * p; p = p == 9 ? 2 : p + 1; } 
            int r = s % 11; 
            return r < 2 ? 0 : 11 - r; 
        }

        public string PrettyPrintXml(string xml) { 
            try 
            { 
                XmlDocument d = new XmlDocument(); 
                d.LoadXml(xml); 
                StringWriter s = new StringWriter(); 
                XmlTextWriter w = new XmlTextWriter(s) 
                { 
                    Formatting = Formatting.Indented 
                }; 
                d.WriteContentTo(w); return s.ToString(); 
            } 
            catch 
            {
                return xml; 
            } 
        }

        public static async Task Envio()
        {
            var banco = new Banco();
            DataTable sefaz = banco.selectNFESefaz();

            foreach (DataRow item in sefaz.Rows)
            {
                var dados1 = banco.ConvertToDadosSEFAZ(item);

                int idNota = dados1.iIdNotaFisc;

                DataTable tabelaProdutos = banco.selectProduto(idNota);
                var produtos = banco.MapearProdutos(tabelaProdutos);

                var pedido = new PedidoNfe
                {
                    iIdNotaFisc = idNota,
                    Serie = dados1.Serie,
                    InNF = dados1.InNF,
                    DataEmissao = dados1.DataEmissao,
                    NatOp = dados1.NatOp,
                    Modelo = dados1.Modelo,
                    Emitente = new Emitente
                    {
                        CNPJ = dados1.Emitente.CNPJ,
                        RazaoSocial = dados1.Emitente.RazaoSocial,
                        IE = dados1.Emitente.IE,
                        Logradouro = dados1.Emitente.Logradouro,
                        Numero = dados1.Emitente.Numero,
                        Bairro = dados1.Emitente.Bairro,
                        CodigoMunicipio = dados1.Emitente.CodigoMunicipio,
                        Municipio = dados1.Emitente.Municipio,
                        UF = dados1.Emitente.UF,
                        CEP = dados1.Emitente.CEP,
                        Telefone = dados1.Emitente.Telefone,
                        CRT = dados1.Emitente.CRT
                    },
                    Destinatario = new Destinatario
                    {
                        CPF = dados1.Destinatario.CPF,
                        Nome = dados1.Destinatario.Nome,
                        Logradouro = dados1.Destinatario.Logradouro,
                        Numero = dados1.Destinatario.Numero,
                        Bairro = dados1.Destinatario.Bairro,
                        Municipio = dados1.Destinatario.Municipio,
                        CodigoMunicipio = dados1.Destinatario.CodigoMunicipio,
                        UF = dados1.Destinatario.UF,
                        CEP = dados1.Destinatario.CEP,
                        IE = dados1.Destinatario.IE
                    },
                    ValorFrete = dados1.ValorFrete,
                    ValorSeguro = dados1.ValorSeguro,
                    ValorDesconto = dados1.ValorDesconto,
                    DiretorioBase = dados1.DiretorioBase,
                    Produtos = produtos
                };
                var service = new MontaNFESefaz();
                var resultado = await service.EmitirSEFAZAsync(pedido);
            }
        }
    }
}

