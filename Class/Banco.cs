using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace NFSe.Class
{
    internal class Banco
    {
        #region CONEXÃO
        static readonly string stringConection = ConfigurationManager.ConnectionStrings["SGM_GERAL"]?.ConnectionString
            ?? throw new InvalidOperationException("String de conexão 'SGM_GERAL' não encontrada no App.config.");
        SqlConnection cn = new SqlConnection(stringConection);
        private SqlConnection AbrirConexao()
        {
            try
            {
                return new SqlConnection(stringConection);
            }
            catch
            {
                throw new Exception("Sem conexação");
            }

        }
        #endregion

        #region MONTADPS.CS

        #region SELECT DA CLASSE MONTADPS.CS
        public DataTable selectRPS()
        {

            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = @"SELECT TOP 10 
	iID_Emitente,
    RTRIM(nNumero) as nNumero,
    RTRIM(sSerie) as sSerie,
    RTRIM(sCdMunicipio) as sCdMunicipio,
    RTRIM(sCNPJPrestador) as sCNPJPrestador,
    RTRIM(sIMPrestador) as sIMPrestador,
    RTRIM(sOpSimples) as sOpSimples,
    RTRIM(sTributacaoRPS) as sTributacaoRPS,
    RTRIM(iIndCPFCNPJToma) as iIndCPFCNPJToma,
    RTRIM(sCNPJCPFTomador) as sCNPJCPFTomador,
    RTRIM(sRazSociTomador) as sRazSociTomador,
    RTRIM(sEndTomador) as sEndTomador,
    RTRIM(sNumeroToma) as sNumeroToma,
    RTRIM(sCompToma) as sCompToma,
    RTRIM(sBairroToma) as sBairroToma,
    RTRIM(sCdMunicToma) as sCdMunicToma,
    RTRIM(sUFToma) as sUFToma,
    replace(RTRIM(sCEPTomador),'-','') as sCEPTomador,
    RTRIM(sEmailTomador) as sEmailTomador,
    replace(RTRIM(sItemListaServi),'.','') as sItemListaServi,
    sDiscriminacao,
    nVlServicos,
    nVlLiqNFSe,
    nAliquota,
    nBaseCalculo,
    iISSRetido,
	nVlISS,
    nVlPis,
    nVlCofins,
    nVlDeducoes,
    nBase_Calculo_Retencoes,
    nPerc_IR,
    nVlIR,
    nPerc_CSLL,
    nVlCsll,
	sCSTPIS,
	sCSTCOFINS,
    T.ALIQ_PIS
    ,T.ALIQ_COFINS
    ,T.ALIQ_ISS
FROM [SGM_GERAL].[dbo].rps RPS
INNER JOIN [SGM_GERAL].[dbo].TABELA_SERVICOS T ON T.COD_SERVICO = RPS.sItemListaServi AND T.ID_ESCOLA = RPS.iID_Emitente
WHERE iID_Emitente in (3,10) AND iSituacao in (0,1,3)
ORDER BY RPS.recnum DESC";
                    DataTable dados = new DataTable();
                    SqlDataAdapter adaptador = new SqlDataAdapter(query, stringConection);
                    adaptador.Fill(dados);
                    return dados;
                }
                catch (Exception ex)
                {
                    throw new Exception(" " + ex);
                }
            }
        }
        #endregion

        #region UPDATE DA CLASSE MONTADPS.CS
        public void updateRPS(string chaveAcesso, string sNrNFSe, string numeroRPS, string emitente, string situacao, string mensagem)
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = @"
                    UPDATE RPS
                       SET iSituacao = @Situacao,                           
                           CHAVE_ACESSO_NFSE = @ChaveAcesso,
                           sUltimaOperacao = @Mensagem,
                           sNrNFSe = @sNrNFSe
                     WHERE NNUMERO = @NumeroRPS
                       AND iID_Emitente = @iID_Emitente";
                    using (var comando = conexao.CreateCommand())
                    {
                        comando.CommandText = query;

                        // Parâmetros
                        comando.Parameters.AddWithValue("@ChaveAcesso", chaveAcesso);
                        comando.Parameters.AddWithValue("@sNrNFSe", sNrNFSe);
                        comando.Parameters.AddWithValue("@NumeroRPS", numeroRPS);
                        comando.Parameters.AddWithValue("@iID_Emitente", emitente);

                        comando.Parameters.AddWithValue("@Situacao", situacao);
                        comando.Parameters.AddWithValue("@Mensagem", mensagem);
                        // Executa o UPDATE
                        int linhasAfetadas = comando.ExecuteNonQuery();

                        Console.WriteLine($"{linhasAfetadas} linha(s) atualizada(s) com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro ao atualizar RPS: " + ex.Message, ex);
                }
            }
        }
        #endregion

        #region POPULA CLASSE MONTADPS.CS
        public DadosNfse ConvertToDados(DataRow dr)
        {
            DadosNfse dados = new DadosNfse();
            dados.Prestador = new PrestadorNfse();
            dados.Tomador = new TomadorNfse();
            dados.Tomador.Endereco = new EnderecoNfse();
            dados.Tomador.Contato = new ContatoNfse();
            dados.Servico = new ServicoNfse();

            dados.Emitente = dr["iID_Emitente"]?.ToString() ?? "";
            dados.NumeroRps = dr["nNumero"]?.ToString() ?? "";
            dados.Serie = dr["sSerie"]?.ToString() ?? "";
            dados.DataEmissao = DateTime.Now;
            dados.DataCompetencia = DateTime.Now;
            dados.CodigoMunicipioEmissao = dr["sCdMunicipio"]?.ToString() ?? "";
            dados.Prestador.Cnpj = dr["sCNPJPrestador"]?.ToString() ?? "";
            dados.Prestador.InscricaoMunicipal = dr["sIMPrestador"]?.ToString() ?? "";
            dados.Prestador.OpcaoSimplesNacional = dr["sOpSimples"]?.ToString() ?? "";
            dados.Prestador.RegimeAplicacaoTributacaoSN = dr["sOpSimples"]?.ToString() ?? "0";

            // Tomador
            dados.Tomador.TipoPessoa = dr["iIndCPFCNPJToma"]?.ToString() ?? "";
            dados.Tomador.CpfCnpj = dr["sCNPJCPFTomador"]?.ToString() ?? "";
            dados.Tomador.Nome = dr["sRazSociTomador"]?.ToString() ?? "";
            dados.Tomador.Endereco.Endereco = dr["sEndTomador"]?.ToString() ?? "";
            dados.Tomador.Endereco.Numero = dr["sNumeroToma"]?.ToString() ?? "";
            dados.Tomador.Endereco.Complemento = dr["sCompToma"]?.ToString() ?? "";
            dados.Tomador.Endereco.Bairro = dr["sBairroToma"]?.ToString() ?? "";
            dados.Tomador.Endereco.CodigoMunicipio = dr["sCdMunicToma"]?.ToString() ?? "";
            dados.Tomador.Endereco.Uf = dr["sUFToma"]?.ToString() ?? "";
            dados.Tomador.Endereco.Cep = dr["sCEPTomador"]?.ToString() ?? "";
            dados.Tomador.Contato.Email = dr["sEmailTomador"]?.ToString() ?? "";

            // Serviço - use valores temporários por enquanto
            dados.Servico.CodigoTributacao = dr["sItemListaServi"]?.ToString() + "01" ?? "080101"; // código temporário
            dados.Servico.DescricaoServico = dr["sDiscriminacao"]?.ToString() ?? "Serviço";
            dados.Servico.CodigoMunicipioPrestacao = dr["sCdMunicToma"]?.ToString() ?? "4106902";
            dados.Servico.ValorServico = dr["nVlServicos"].ToString() ?? "1.00";
            dados.Servico.ValorServicolq = dr["nVlLiqNFSe"].ToString() ?? "1.00";
            dados.Servico.Aliquota = dr["nAliquota"].ToString() ?? "0.00";
            dados.Servico.BaseCalculo = dr["nBaseCalculo"].ToString() ?? "1.00";
            dados.Servico.TipoRetencaoISSQN = dr["iISSRetido"]?.ToString() ?? "1";
            dados.Servico.ValorISS = dr["nVlISS"]?.ToString() ?? "";
            dados.Servico.ValorPIS = dr["nVlPis"]?.ToString() ?? "";
            dados.Servico.ValorCofins = dr["nVlCofins"]?.ToString() ?? "";
            dados.Servico.ValorDeducoes = dr["nVlDeducoes"]?.ToString() ?? "";
            dados.Servico.BaseDeCalculoRetencoes = dr["nBase_Calculo_Retencoes"]?.ToString() ?? "";
            dados.Servico.PercentualIR = dr["nPerc_IR"]?.ToString() ?? "";
            dados.Servico.ValorIR = dr["nVlIR"]?.ToString() ?? "";
            dados.Servico.PercentualContribuicaoSocial = dr["nPerc_CSLL"]?.ToString() ?? "";
            dados.Servico.ValorContribuicaoSocial = dr["nVlCsll"]?.ToString() ?? "";
            dados.Servico.CSTPIS = dr["sCSTPIS"]?.ToString() ?? "";
            dados.Servico.CSTCOFINS = dr["sCSTCOFINS"]?.ToString() ?? "";
            // ALQUOTA PIS COFINS
            dados.Servico.ALIQ_PIS = dr["ALIQ_PIS"]?.ToString() ?? "";
            dados.Servico.ALIQ_COFINS = dr["ALIQ_COFINS"]?.ToString() ?? "";
            dados.Servico.ALIQ_ISS = dr["ALIQ_ISS"]?.ToString() ?? "";
            return dados;
        }
        #endregion

        #endregion

        #region MONTARPDSSP.CS

        #region SELECT DA CLASSE MONTARPDSSP.CS
        public DataTable selectRPSSP()
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = @"SELECT 
	 iID_Emitente,
                        RTRIM(nNumero)                AS nNumero,
                        RTRIM(sSerie)                 AS sSerie,
                        RTRIM(sCdMunicipio)           AS sCdMunicipio,
                        RTRIM(sCNPJPrestador)         AS sCNPJPrestador,
                        RTRIM(sIMPrestador)           AS sIMPrestador,
                        RTRIM(sOpSimples)             AS sOpSimples,
                        RTRIM(sTributacaoRPS)         AS sTributacaoRPS,
                        RTRIM(iIndCPFCNPJToma)        AS iIndCPFCNPJToma,
                        RTRIM(sCNPJCPFTomador)        AS sCNPJCPFTomador,
                        RTRIM(REPLACE(sRazSociTomador, '&', '&amp;'))        AS sRazSociTomador,
                        left(RTRIM(sEndTomador),45)            AS sEndTomador,
                        RTRIM(sNumeroToma)            AS sNumeroToma,
                        RTRIM(sCompToma)              AS sCompToma,
                        left(RTRIM(sBairroToma),30)            AS sBairroToma,
                        RTRIM(sCdMunicToma)           AS sCdMunicToma,
                        RTRIM(sUFToma)                AS sUFToma,
                        replace(RTRIM(sCEPTomador),'-','') as sCEPTomador,
                        RTRIM(sEmailTomador)          AS sEmailTomador,
                        REPLACE(RTRIM(sItemListaServi), '.', '') AS sItemListaServi,
                        REPLACE(REPLACE(REPLACE(CAST(sDiscriminacao as varchar(8000)), '&', '&amp;'), '<', '&lt;'), '>', '&gt;') AS sDiscriminacao,
                        nVlServicos,
                        (nVlLiqNFSe) AS ValorInicialCobrado,
                        CAST(nAliquota AS DECIMAL(10,4)) / 100 AS nAliquota,
                        nBaseCalculo,
                        CASE 
                            WHEN iISSRetido = 2 THEN 0 
                            ELSE iISSRetido 
                        END                           AS iISSRetido,
                        nVlISS,
                        nVlPis,
                        nVlCofins,
                        nVlDeducoes,
                        RPS.nVlIR,
                        RPS.nVlCsll,
                        '1'                           AS indFinal,
                        '100301'                      AS cIndOp,
                        '0'                           AS finNFSe,
                        '0'                           AS indDest,
                        '000001'                      AS cClassTrib
FROM [SGM_GERAL].[dbo].rps 
WHERE iID_Emitente in (1,2) AND iSituacao in (0,1,3)
ORDER BY iID_Emitente, iSituacao asc
";
                    DataTable dados = new DataTable();
                    SqlDataAdapter adaptador = new SqlDataAdapter(query, stringConection);
                    adaptador.Fill(dados);
                    return dados;
                }
                catch (Exception ex)
                {
                    throw new Exception(" " + ex);
                }
            }
        }
        #endregion

        #region UPDATE DA CLASSE MONTARPDSSP.CS
        public void updateRPSSP(string chaveAcesso, string NumeroNFe, string mensagem, string codigoVerificacao, string situacao, string nNumero, string IdEmitente)
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = @"UPDATE RPS SET
    sNrNFSe = @NumeroNFe, 
    sCdVerificao = @CodigoVerificacao, 
    iSituacao = @Situacao,
    CHAVE_ACESSO_NFSE = @ChaveAcesso, 
    sUltimaOperacao = @Mensagem,
    DATA_RETORNO_PREFEITURA = GETDATE()
WHERE iID_Emitente = @IdEmitente 
  AND nNumero = @nNumero";
                    using (var comando = conexao.CreateCommand())
                    {
                        comando.CommandText = query;

                        // Parâmetros
                        comando.Parameters.AddWithValue("@NumeroNFe", NumeroNFe);
                        comando.Parameters.AddWithValue("@CodigoVerificacao", codigoVerificacao);
                        comando.Parameters.AddWithValue("@Situacao", situacao);
                        comando.Parameters.AddWithValue("@ChaveAcesso", chaveAcesso);
                        comando.Parameters.AddWithValue("@Mensagem", mensagem);
                        comando.Parameters.AddWithValue("@nNumero", nNumero);
                        comando.Parameters.AddWithValue("@IdEmitente", IdEmitente);

                        // Executa o UPDATE
                        int linhasAfetadas = comando.ExecuteNonQuery();

                        Console.WriteLine($"{linhasAfetadas} linha(s) atualizada(s) com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Erro ao atualizar RPS: " + ex.Message, ex);
                }
            }
        }
        #endregion

        #region POPULA CLASSE MONTARPDSSP.CS
        public RpsData ConvertToDadosSP(DataRow dr)
        {
            RpsData dados = new RpsData();


            dados.iID_Emitente = dr["iID_Emitente"]?.ToString() ?? "";
            dados.nNumero = dr["nNumero"]?.ToString() ?? "";
            dados.sSerie = dr["sSerie"]?.ToString() ?? "";
            dados.sCdMunicipio = dr["sCdMunicipio"]?.ToString() ?? "";
            dados.sCNPJPrestador = dr["sCNPJPrestador"]?.ToString() ?? "";
            dados.sIMPrestador = dr["sIMPrestador"]?.ToString() ?? "";
            dados.sOpSimples = dr["sOpSimples"]?.ToString() ?? "";

            // Tomador
            dados.iIndCPFCNPJToma = dr["iIndCPFCNPJToma"]?.ToString() ?? "";
            dados.sCNPJCPFTomador = dr["sCNPJCPFTomador"]?.ToString() ?? "";
            dados.sRazSociTomador = dr["sRazSociTomador"]?.ToString() ?? "";
            dados.sEndTomador = dr["sEndTomador"]?.ToString() ?? "";
            dados.sNumeroToma = dr["sNumeroToma"]?.ToString() ?? "";
            dados.sCompToma = dr["sCompToma"]?.ToString() ?? "";
            dados.sBairroToma = dr["sBairroToma"]?.ToString() ?? "";
            dados.sCdMunicToma = dr["sCdMunicToma"]?.ToString() ?? "";
            dados.sUFToma = dr["sUFToma"]?.ToString() ?? "";
            dados.sCEPTomador = dr["sCEPTomador"]?.ToString() ?? "";
            dados.sEmailTomador = dr["sEmailTomador"]?.ToString() ?? "";

            // Serviço
            dados.sItemListaServi = dr["sItemListaServi"]?.ToString() ?? "080101";
            dados.sDiscriminacao = dr["sDiscriminacao"]?.ToString() ?? "Serviço";
            dados.sCdMunicToma = dr["sCdMunicToma"]?.ToString() ?? "4106902";
            dados.nVlServicos = dr["nVlServicos"] == DBNull.Value ? 1.00m : Convert.ToDecimal(dr["nVlServicos"]);
            dados.nAliquota = dr["nAliquota"].ToString() ?? "0.00";
            dados.nBaseCalculo = dr["nBaseCalculo"].ToString() ?? "1.00";
            dados.iISSRetido = dr["iISSRetido"]?.ToString() ?? "1";
            dados.nVlIR = dr["nVlISS"]?.ToString() ?? "";
            dados.nVlPis = dr["nVlPis"]?.ToString() ?? "";
            dados.nVlCofins = dr["nVlCofins"]?.ToString() ?? "";
            dados.nVlDeducoes = dr["nVlDeducoes"] == DBNull.Value ? 1.00m : Convert.ToDecimal(dr["nVlDeducoes"]);

            dados.nVlIR = dr["nVlIR"]?.ToString() ?? "";
            dados.nVlCsll = dr["nVlCsll"]?.ToString() ?? "";

            dados.sTributacaoRPS = dr["sTributacaoRPS"]?.ToString() ?? "";
            //dados.ValorInicialCobrado = dr["ValorInicialCobrado"]?.ToString() ?? "";
            dados.ValorInicialCobrado = dr["ValorInicialCobrado"] == DBNull.Value ? 1.00m : Convert.ToDecimal(dr["ValorInicialCobrado"]);
            dados.finNFSe = dr["finNFSe"]?.ToString() ?? "";
            dados.indFinal = dr["indFinal"]?.ToString() ?? "";
            dados.cIndOp = dr["cIndOp"]?.ToString() ?? "";
            dados.indDest = dr["indDest"]?.ToString() ?? "";
            dados.cClassTrib = dr["cClassTrib"]?.ToString() ?? "";

            return dados;
        }
        #endregion

        #endregion

        #region MONTANFESEFAZ.CS

        #region SELECT DA CLASSE MONTANFESEFAZ.CS
        public DataTable selectNFESefaz()
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = @"
SELECT iIdNotaFisc,
    N.iSerie AS Serie,
    ISNULL(NULLIF(N.inNF, 0), N.iIdNotaFisc) AS inNF,
    N.dEmi AS DataEmissao,
    RTRIM(ISNULL(N.snatOp, 'VENDA')) AS NatOp,
    N.sMod AS Modelo,
    dbo.fn_LimpaCNPJ_CPF(E.INS_CNPJ) AS EMIT_CNPJ,
    RTRIM(E.INS_NOME) AS EMIT_NOME,
    RTRIM(E.INS_ENDERECO) AS EMIT_ENDERECO,
    ltrim(RTRIM(REPLACE(E.INS_NUMERO, '.', ''))) AS EMIT_NUMERO,
    dbo.RemoverAcentos(RTRIM(E.INS_BAIRRO)) AS EMIT_BAIRRO,
    ISNULL(E.COD_MUNICIPIO, 3550308) AS EMIT_COD_MUNICIPIO,
    dbo.RemoverAcentos(RTRIM(E.INS_MUNICIPIO)) AS EMIT_MUNICIPIO,
    E.INS_UF AS EMIT_UF,
    E.INS_CEP AS EMIT_CEP,
    dbo.fn_LimpaTelefone(E.INS_TELEFONE) AS EMIT_TELEFONE,
    RTRIM(E.INS_IE) AS EMIT_IE,
    RTRIM(ISNULL(E.COD_REGIME_TRIBUTARIO, '3')) AS EMIT_CRT,
    RTRIM(N.sCPF_CNPJ) AS CPFDestinario,
    RTRIM(N.sNome_Razao) AS NomeDestinario,
    RTRIM(N.sEndereco) AS EnderecoDestinario,
    RTRIM(N.sNumero) AS NumeroDestinario,
    RTRIM(N.sBairro) AS BairroDestinario,
    RTRIM(N.sMunicipio) AS MunicipioDestinario,
    ISNULL(N.COD_MUNICIPIO_DEST, 3550308) AS cMunDest,
    N.sUF AS UFDestinario,
	replace(RTRIM(N.sCEP),'-','') as CEPDestinario,
    RTRIM(N.sInscr_Estadual) AS IE,
    ISNULL(N.nvFrete, 0) AS ValorFrete,
    ISNULL(N.nvSeg, 0) AS ValorSeguro,
    ISNULL(N.nvDesc, 0) AS ValorDesconto,
    'A:\SGM_files\NOTAS_FISCAIS\NFE\NFE_API' AS DIR_BASE
FROM NOTAFISC N
INNER JOIN ESCOLAS E ON E.ID_ESCOLA = N.iIdEmitente
WHERE iIdEmitente in (1,2) and iSituacaoNFe in (0,1,3)";

                    DataTable dados = new DataTable();
                    SqlDataAdapter adaptador = new SqlDataAdapter(query, stringConection);
                    adaptador.Fill(dados);
                    return dados;
                }
                catch (Exception ex)
                {
                    throw new Exception(" " + ex);
                }
            }
        }

        public DataTable selectProduto(int idNota)
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();

                    string query = @"
SELECT 
    RTRIM(N.iIdProduto)       AS Codigo, 
    RTRIM(N.Descricao_Produto) AS Descricao, 
    RTRIM(N.sNCM)             AS NCM, 
    RTRIM(N.unidade)          AS Unidade, 
    N.nqCom            AS Quantidade, 
    N.nvUnCom          AS ValorUnitario, 
    N.nvProd           AS ValorProd, 
    N.iCFOP           AS CFOP 
FROM NOTAPRSE N 
INNER JOIN NOTAFISC NOTA 
    ON NOTA.inNF = N.inNF 
   AND NOTA.iIdEmitente = N.iIdEmitente 
WHERE NOTA.iIdNotaFisc = @idNota;";

                    using (var cmd = new SqlCommand(query, conexao))
                    {
                        // Adiciona o parâmetro idNota
                        cmd.Parameters.Add("@idNota", SqlDbType.Int).Value = idNota;

                        // Usa o comando parametrizado no DataAdapter
                        using (var adaptador = new SqlDataAdapter(cmd))
                        {
                            var dados = new DataTable();
                            adaptador.Fill(dados);
                            return dados;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Evite encadear Exception genérico. Você pode logar o ex e relançar.
                    throw new Exception("Erro ao consultar produtos da nota.", ex);
                }
            }
        }
        #endregion                    

        #region UPDATE DA CLASSE MONTANFESefaz.CS
        public void updateNFESefaz(int idNota, string xmlRetorno, string chave)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlRetorno);
                XmlNode infProt = doc.GetElementsByTagName("infProt").Item(0);

                string cStatStr = "", xMotivo = "", nProt = "", dhAutOriginal = "", dataFormatada = "";

                if (infProt != null)
                {
                    cStatStr = infProt["cStat"]?.InnerText ?? "";
                    xMotivo = infProt["xMotivo"]?.InnerText ?? "";
                    nProt = infProt["nProt"]?.InnerText ?? "";
                    dhAutOriginal = infProt["dhRecbto"]?.InnerText ?? "";
                    if (DateTime.TryParse(dhAutOriginal, null, DateTimeStyles.RoundtripKind, out DateTime dtOut))
                        dataFormatada = dtOut.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    XmlNode retEnvi = doc.GetElementsByTagName("retEnviNFe").Item(0);
                    cStatStr = retEnvi?["cStat"]?.InnerText ?? "999";
                    xMotivo = retEnvi?["xMotivo"]?.InnerText ?? "Erro Sefaz";
                }

                int status = int.TryParse(cStatStr, out int s) ? s : 999;
                int situacaoFinal = (status == 100 || status == 150) ? 4 : (status == 110 || status == 301 || status == 302) ? 6 : (status == 225) ? 3 : 5;

                using (var conexao = AbrirConexao())
                {
                    conexao.Open();
                    string query = @"UPDATE NOTAFISC SET iSituacaoNFe = @sit, sUltimaOperacao = @msg, sChave = @cha, sProtocolAprNFe = @pro, sDtAutorizNFe = @dat WHERE iIdNotaFisc = @id";
                    using (var comando = conexao.CreateCommand())
                    {
                        comando.CommandText = query;
                        comando.Parameters.AddWithValue("@sit", situacaoFinal);
                        comando.Parameters.AddWithValue("@msg", (cStatStr + " - " + xMotivo));
                        comando.Parameters.AddWithValue("@cha", chave ?? (object)DBNull.Value);
                        comando.Parameters.AddWithValue("@pro", nProt ?? (object)DBNull.Value);
                        comando.Parameters.AddWithValue("@dat", !string.IsNullOrEmpty(dataFormatada) ? dataFormatada : (object)DBNull.Value);
                        comando.Parameters.AddWithValue("@id", idNota);
                        comando.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Erro Banco Update: " + ex.Message); }
        }
        #endregion

        #region POPULA CLASSE MONTANFESefaz.CS
        public PedidoNfe ConvertToDadosSEFAZ(DataRow dr)
        {
            PedidoNfe dados = new PedidoNfe();

            if (dados.Emitente == null) dados.Emitente = new Emitente();
            if (dados.Destinatario == null) dados.Destinatario = new Destinatario();
            if (dados.Produtos == null) dados.Produtos = new List<Produto>();

            dados.iIdNotaFisc = dr["iIdNotaFisc"] == DBNull.Value ? 0 : Convert.ToInt32(dr["iIdNotaFisc"]);
            dados.Serie = dr["Serie"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Serie"]);
            dados.InNF = dr["inNF"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["inNF"]);
            dados.DataEmissao = dr["DataEmissao"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(dr["DataEmissao"]);
            dados.NatOp = dr["NatOp"] == DBNull.Value ? string.Empty : dr["NatOp"]?.ToString() ?? string.Empty;
            dados.Modelo = dr["Modelo"] == DBNull.Value ? string.Empty : dr["Modelo"]?.ToString() ?? string.Empty;

            dados.Emitente.CNPJ = dr["EMIT_CNPJ"] == DBNull.Value ? string.Empty : dr["EMIT_CNPJ"]?.ToString() ?? string.Empty;
            dados.Emitente.RazaoSocial = dr["EMIT_NOME"] == DBNull.Value ? string.Empty : dr["EMIT_NOME"]?.ToString() ?? string.Empty;
            dados.Emitente.IE = dr["EMIT_IE"] == DBNull.Value ? string.Empty : dr["EMIT_IE"]?.ToString() ?? string.Empty;
            dados.Emitente.Logradouro = dr["EMIT_ENDERECO"] == DBNull.Value ? string.Empty : dr["EMIT_ENDERECO"]?.ToString() ?? string.Empty;
            dados.Emitente.Numero = dr["EMIT_NUMERO"] == DBNull.Value ? string.Empty : dr["EMIT_NUMERO"]?.ToString() ?? string.Empty;
            dados.Emitente.Bairro = dr["EMIT_BAIRRO"] == DBNull.Value ? string.Empty : dr["EMIT_BAIRRO"]?.ToString() ?? string.Empty;
            dados.Emitente.CodigoMunicipio = dr["EMIT_COD_MUNICIPIO"] == DBNull.Value ? string.Empty : dr["EMIT_COD_MUNICIPIO"]?.ToString() ?? string.Empty;
            dados.Emitente.Municipio = dr["EMIT_MUNICIPIO"] == DBNull.Value ? string.Empty : dr["EMIT_MUNICIPIO"]?.ToString() ?? string.Empty;
            dados.Emitente.UF = dr["EMIT_UF"] == DBNull.Value ? string.Empty : dr["EMIT_UF"]?.ToString() ?? string.Empty;
            dados.Emitente.CEP = dr["EMIT_CEP"] == DBNull.Value ? string.Empty : dr["EMIT_CEP"]?.ToString() ?? string.Empty;
            dados.Emitente.Telefone = dr["EMIT_TELEFONE"] == DBNull.Value ? string.Empty : dr["EMIT_TELEFONE"]?.ToString() ?? string.Empty;
            dados.Emitente.CRT = dr["EMIT_CRT"] == DBNull.Value ? string.Empty : dr["EMIT_CRT"]?.ToString() ?? string.Empty;

            // --- Destinatario ---
            dados.Destinatario.CPF = dr["CPFDestinario"] == DBNull.Value ? string.Empty : dr["CPFDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.Nome = dr["NomeDestinario"] == DBNull.Value ? string.Empty : dr["NomeDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.Logradouro = dr["EnderecoDestinario"] == DBNull.Value ? string.Empty : dr["EnderecoDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.Numero = dr["NumeroDestinario"] == DBNull.Value ? string.Empty : dr["NumeroDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.Bairro = dr["BairroDestinario"] == DBNull.Value ? string.Empty : dr["BairroDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.Municipio = dr["MunicipioDestinario"] == DBNull.Value ? string.Empty : dr["MunicipioDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.CodigoMunicipio = dr["cMunDest"] == DBNull.Value ? string.Empty : dr["cMunDest"]?.ToString() ?? string.Empty;
            dados.Destinatario.UF = dr["UFDestinario"] == DBNull.Value ? string.Empty : dr["UFDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.CEP = dr["CEPDestinario"] == DBNull.Value ? string.Empty : dr["CEPDestinario"]?.ToString() ?? string.Empty;
            dados.Destinatario.IE = dr["IE"] == DBNull.Value ? string.Empty : dr["IE"]?.ToString() ?? string.Empty;
            dados.ValorFrete = dr["ValorFrete"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ValorFrete"]);
            dados.ValorSeguro = dr["ValorSeguro"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ValorSeguro"]);
            dados.ValorDesconto = dr["ValorDesconto"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ValorDesconto"]);
            dados.DiretorioBase = dr["DIR_BASE"] == DBNull.Value ? string.Empty : dr["DIR_BASE"]?.ToString() ?? string.Empty;

            return dados;
        }

        public List<Produto> MapearProdutos(DataTable tabela)
        {
            var produtos = new List<Produto>();

            foreach (DataRow r in tabela.Rows)
            {


                var p = new Produto
                {
                    Codigo = SafeGetString(r, "Codigo"),
                    Descricao = SafeGetString(r, "Descricao"),
                    NCM = SafeGetString(r, "NCM"),
                    Unidade = SafeGetString(r, "Unidade"),
                    Quantidade = SafeGetDecimal(r, "Quantidade"),
                    ValorUnitario = SafeGetDecimal(r, "ValorUnitario"),
                    ValorProd = SafeGetDecimal(r, "ValorProd"),
                    CFOP = SafeGetInt(r, "CFOP")
                };

                produtos.Add(p);
            }

            return produtos;
        }

        private static int SafeGetInt(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value
                ? Convert.ToInt32(r[col])
                : 0;
        }

        private static decimal SafeGetDecimal(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value
                ? Convert.ToDecimal(r[col])
                : 0m;
        }

        private static string SafeGetString(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value
                ? r[col].ToString()
                : string.Empty;
        }
        #endregion

        #endregion

    }
}
