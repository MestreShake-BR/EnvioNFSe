using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NFSe.Class
{
    internal class Banco
    {


        static string stringConection =
            "";
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

        public DataTable selectRPS()
        {

            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = "";
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


        public DataTable selectRPSSP()
        {

            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = "";
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

        public void updateRPS(string chaveAcesso, string sNrNFSe, string numeroRPS, string emitente)
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = "";
                    using (var comando = conexao.CreateCommand())
                    {
                        comando.CommandText = query;

                        // Parâmetros
                        comando.Parameters.AddWithValue("@ChaveAcesso", chaveAcesso);
                        comando.Parameters.AddWithValue("@NumeroNFSE", sNrNFSe);
                        comando.Parameters.AddWithValue("@NumeroRPS", numeroRPS);
                        comando.Parameters.AddWithValue("@emitente", emitente);

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

        public void updateRPSSP(string chaveAcesso, string NumeroNFe, string mensagem, string codigoVerificacao, string situacao, string nNumero, string IdEmitente)
        {
            using (var conexao = AbrirConexao())
            {
                try
                {
                    conexao.Open();
                    string query = "";
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

            // Perguntar para o Sr Roberto - use valores temporários
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
    }
}
