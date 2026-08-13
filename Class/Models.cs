using RestSharp.Serializers.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;


namespace NFSe.Class
{
    #region Models para DPS Nacional
    public class DadosNfse
    {
        public string NumeroRps { get; set; }
        public string Serie { get; set; }
        public DateTime DataEmissao { get; set; }
        public DateTime DataCompetencia { get; set; }
        public string CodigoMunicipioEmissao { get; set; }
        public PrestadorNfse Prestador { get; set; }
        public TomadorNfse Tomador { get; set; }
        public ServicoNfse Servico { get; set; }
        public string Emitente { get; set; }
    }

    public class PrestadorNfse
    {
        public string Cnpj { get; set; }
        public string InscricaoMunicipal { get; set; }
        public string OpcaoSimplesNacional { get; set; } = "3"; // 3 = Optante
        public string RegimeAplicacaoTributacaoSN { get; set; } = "1"; // 1 = Microempresa
        public string RegimeEspecialTributacao { get; set; } = "0"; // 0 = Nenhum
    }

    public class TomadorNfse
    {
        public string TipoPessoa { get; set; }
        public string CpfCnpj { get; set; }
        public string Nome { get; set; }
        public EnderecoNfse Endereco { get; set; }
        public ContatoNfse Contato { get; set; }
    }

    public class EnderecoNfse
    {
        public string Endereco { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Bairro { get; set; }
        public string CodigoMunicipio { get; set; }
        public string Uf { get; set; }
        public string Cep { get; set; }
    }

    public class ContatoNfse
    {
        public string Email { get; set; }
    }

    public class ServicoNfse
    {
        public string CodigoTributacao { get; set; }
        public string DescricaoServico { get; set; }
        public string CodigoMunicipioPrestacao { get; set; }
        public string ValorServico { get; set; }
        public string ValorServicolq { get; set; }
        public string BaseCalculo { get; set; }
        public string Aliquota { get; set; }
        public string TributacaoISSQN { get; set; } = "1"; // 1 = Sim
        public string TipoRetencaoISSQN { get; set; } = "1"; // 1 = Não retido
        public string PercentualTotalTributosFederais { get; set; } = "";
        public string PercentualTotalTributosEstaduais { get; set; } = "";
        public string PercentualTotalTributosMunicipais { get; set; } = "";
        public string ValorISS { get; set; } = "";
        public string ValorPIS { get; set; } = "";
        public string ValorPISRET { get; set; } = "";
        public string ValorCofins { get; set; } = "";
        public string ValorCofinsRET { get; set; } = "";
        public string ValorDeducoes { get; set; } = "";
        public string BaseDeCalculoRetencoes { get; set; } = "";
        public string PercentualIR { get; set; } = "";
        public string ValorIR { get; set; } = "";
        public string PercentualContribuicaoSocial { get; set; } = "";
        public string ValorContribuicaoSocial { get; set; } = "";
        public string ValorContribuicaoSocialRET { get; set; } = "";
        public string CSTPIS { get; set; } = "";
        public string CSTCOFINS { get; set; } = "";
        public string Natureza_Retencao_Fonte { get; set; } = "";
        public string ALIQ_PIS { get; set; } = "";
        public string ALIQ_PISRET { get; set; } = "";
        public string ALIQ_COFINS { get; set; } = "";
        public string ALIQ_COFINSRET { get; set; } = "";
        public string ALIQ_ISS { get; set; } = "";

    }

    public class RetornoNfse
    {
        internal string nNFSe { get; set; }
        public int tipoAmbiente { get; set; }
        public string versaoAplicativo { get; set; }
        public DateTime dataHoraProcessamento { get; set; }
        public string idDps { get; set; }
        public string chaveAcesso { get; set; }
        public string nfseXmlGZipB64 { get; set; }
        public string Mensagem { get; set; } = "";
        public object alertas { get; set; }
        public List<ErroNfse> erros { get; set; }
        public bool sucesso { get; set; }
    }

    public class ErroNfse
    {
        public string Codigo { get; set; }
        public string Descricao { get; set; }
    }
    #endregion

    #region Models para RPS SP
    public class RetornoNfseSP
    {
        public string NumeroNFe { get; set; } = "";
        public string CodigoVerificacao { get; set; } = "";
        public string ChaveNotaNacional { get; set; } = "";
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = "";
    }

    public class RpsData
    {
        public string iID_Emitente { get; set; }
        public string nNumero { get; set; }
        public string sSerie { get; set; }
        public string sCdMunicipio { get; set; }
        public string sCNPJPrestador { get; set; }
        public string sIMPrestador { get; set; }
        public string sOpSimples { get; set; }
        public string sTributacaoRPS { get; set; }
        public string iIndCPFCNPJToma { get; set; }
        public string sCNPJCPFTomador { get; set; }
        public string sRazSociTomador { get; set; }
        public string sEndTomador { get; set; }
        public string sNumeroToma { get; set; }
        public string sCompToma { get; set; }
        public string sBairroToma { get; set; }
        public string sCdMunicToma { get; set; }
        public string sUFToma { get; set; }
        public string sCEPTomador { get; set; }
        public string sEmailTomador { get; set; }
        public string sItemListaServi { get; set; }
        public string sDiscriminacao { get; set; }
        public decimal nVlServicos { get; set; }
        public decimal ValorInicialCobrado { get; set; }
        public string nAliquota { get; set; }
        public string nBaseCalculo { get; set; }
        public string iISSRetido { get; set; }
        public string nVlISS { get; set; }
        public decimal nVlPis { get; set; }
        public decimal nVlCofins { get; set; }
        public decimal nVlPis_RET { get; set; }
        public decimal nVlCofins_RET { get; set; }
        public decimal nVlDeducoes { get; set; }
        public decimal nVlIR { get; set; }
        public decimal nVlCsll { get; set; }
        public string indFinal { get; set; }
        public string cIndOp { get; set; }
        public string finNFSe { get; set; }
        public string indDest { get; set; }
        public string cClassTrib { get; set; }
        public string Natureza_Retencao_Fonte { get; set; }
        public string Zerar_Impostos { get; set; }
        public DateTime DataEmissao { get; set; }

        // IBS  CBS
        public string COD_NBS { get; set; }
        public string CST_IBS_CBS { get; set; }
        public string CLASSIF_TRIBUTARIA_IBS_CBS { get; set; }
        public decimal REDUCAO_PERCENT_IBS_CBS { get; set; }
        public decimal ALIQUOTA_IBS { get; set; }
        public decimal ALIQUOTA_CBS { get; set; }
    }
    #endregion

    #region Models para SEFAZ SP
    public class PedidoNfe
    {
        public string iIdEmitente { get; set; }
        public int iIdNotaFisc { get; set; }
        public int Serie { get; set; }
        public decimal InNF { get; set; }
        public DateTime DataEmissao { get; set; }
        public string NatOp { get; set; }
        public string Modelo { get; set; }
        public Emitente Emitente { get; set; }
        public Destinatario Destinatario { get; set; }
        public List<Produto> Produtos { get; set; }
        public decimal ValorFrete { get; set; }
        public decimal ValorSeguro { get; set; }
        public decimal ValorDesconto { get; set; }
        public string DiretorioBase { get; set; }
        

    }
    public class Emitente
    {
        public string CNPJ { get; set; }
        public string RazaoSocial { get; set; }
        public string IE { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Bairro { get; set; }
        public string CodigoMunicipio { get; set; }
        public string Municipio { get; set; }
        public string UF { get; set; }
        public string CEP { get; set; }
        public string Telefone { get; set; }
        public string CRT { get; set; }
    }
    public class Destinatario
    {
        public string CPF { get; set; }
        public string Nome { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Bairro { get; set; }
        public string Municipio { get; set; }
        public string CodigoMunicipio { get; set; }
        public string UF { get; set; }
        public string CEP { get; set; }
        public string IE { get; set; }
    }
    public class Produto
    {
        public int Item { get; set; }
        public string Codigo { get; set; }
        public string Descricao { get; set; }
        public string NCM { get; set; }
        public string Unidade { get; set; }
        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorProd { get; set; }
        public int CFOP { get; set; }
        public string CST { get; set; }
    }
}
    #endregion
