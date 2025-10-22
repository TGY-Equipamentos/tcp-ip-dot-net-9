// ============================================================================
// Programa de Comunicação TCP/IP - Versão Simplificada para Treinamento
// Desenvolvido por © 2025 TGY Cyber utilizando .NET 9
// ============================================================================

using System.Net.Sockets;
using System.Text;

namespace TCP_IP_NET;

internal static class TcpIpNet
{
    // ========================================
    // CONFIGURAÇÕES DE CONEXÃO
    // ========================================

    // Endereço IP do servidor que queremos conectar
    private static string _enderecoIp = "192.168.15.130";

    // Porta TCP onde o servidor está escutando
    private static int _porta = 1100;

    // Comando que será enviado ao servidor (em hexadecimal)
    private static byte _comando = 0x05;

    // Delimitadores da mensagem:
    // STX (Start of Text) = 0x02 - marca o início da mensagem
    // ETX (End of Text) = 0x03 - marca o fim da mensagem
    private static byte _inicioMensagem = 0x02;
    private static byte _fimMensagem = 0x03;

    // ========================================
    // MÉTODO PRINCIPAL
    // ========================================
    public static async Task Main()
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"Servidor: {_enderecoIp}:{_porta}");
        Console.WriteLine($"Comando: 0x{_comando:X2}; Delimitadores: STX=0x{_inicioMensagem:X2}, ETX=0x{_fimMensagem:X2}");
        Console.WriteLine();

        try
        {
            await ExecutarComunicacaoAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }
    }

    // ========================================
    // MÉTODO DE COMUNICAÇÃO PRINCIPAL
    // ========================================
    private static async Task ExecutarComunicacaoAsync()
    {
        // ETAPA 1: CRIAR E CONECTA O CLIENTE TCP
        Console.WriteLine("Conectando ao servidor...");
        using var cliente = new TcpClient();

        cliente.ConnectAsync(_enderecoIp, _porta).Wait();

        // Verifica se conectou com sucesso
        if (!cliente.Connected)
            throw new Exception("Não foi possível conectar ao servidor");

        Console.WriteLine("Conectado com sucesso!");
        Console.WriteLine();

        // ETAPA 2: OBTER O STREAM DE REDE PARA ENVIAR/RECEBER DADOS
        using var stream = cliente.GetStream();

        // ETAPA 3: ENVIAR COMANDO PARA O SERVIDOR
        await EnviarComandoAsync(stream, _comando);
        Console.WriteLine();

        // ETAPA 4: RECEBER E PROCESSAR A RESPOSTA
        string resposta = await ReceberRespostaAsync(stream);
        Console.WriteLine();

        // ETAPA 5: FORMATAR E EXIBIR O RESULTADO
        var formatado = TentarFormatarDados(resposta);
        Console.WriteLine(formatado);
    }

    // ========================================
    // MÉTODO PARA ENVIAR COMANDO
    // ========================================
    private static async Task EnviarComandoAsync(NetworkStream stream, byte comando)
    {
        // Cria um array com o byte do comando
        byte[] dados = [comando];

        // Envia o byte através do stream
        await stream.WriteAsync(dados);
        await stream.FlushAsync();

        // Exibe log da transmissão
        Console.WriteLine("Comando enviado");
    }

    // ========================================
    // MÉTODO PARA RECEBER RESPOSTA
    // ========================================
    private static async Task<string> ReceberRespostaAsync(NetworkStream stream)
    {
        // Buffer para armazenar os dados recebidos (4KB)
        byte[] buffer = new byte[4096];

        // StringBuilder para concatenar os dados recebidos
        StringBuilder mensagemCompleta = new StringBuilder();

        // Índice onde foi encontrado o byte de início (STX)
        int posicaoInicio = -1;

        // Loop para ler dados até encontrar a mensagem completa (STX...ETX)
        while (true)
        {
            // Lê dados do stream
            int bytesLidos = await stream.ReadAsync(buffer, 0, buffer.Length);

            // Se não recebeu nada, encerra o loop
            if (bytesLidos <= 0)
                break;

            // Converte os bytes recebidos para string (ASCII)
            string textoRecebido = Encoding.ASCII.GetString(buffer, 0, bytesLidos);

            // Exibe log da recepção
            Console.WriteLine($"   [RX {DateTime.Now:HH:mm:ss.fff}] Recebido: {textoRecebido.TrimEnd()}");

            // Adiciona ao texto completo
            mensagemCompleta.Append(textoRecebido);

            // Procura pelos delimitadores STX e ETX
            string textoAtual = mensagemCompleta.ToString();

            // Encontra a posição do byte de início (STX)
            if (posicaoInicio < 0)
                posicaoInicio = textoAtual.IndexOf((char)_inicioMensagem);

            // Se encontrou o STX, procura pelo ETX
            if (posicaoInicio >= 0)
            {
                int posicaoFim = textoAtual.IndexOf((char)_fimMensagem, posicaoInicio + 1);

                // Se encontrou ambos os delimitadores, extrai a mensagem completa
                if (posicaoFim > posicaoInicio)
                {
                    // Retorna a substring do STX até o ETX (inclusive)
                    return textoAtual.Substring(posicaoInicio, posicaoFim - posicaoInicio + 1);
                }
            }
        }

        // Retorna tudo que foi recebido (caso não encontre os delimitadores)
        return mensagemCompleta.ToString();
    }

    // ========================================
    // MÉTODO PARA FORMATAR DADOS RECEBIDOS
    // ========================================
    // Este método interpreta a resposta do servidor e formata em texto legível
    // Formato esperado: "peso precoKg total" (valores inteiros sem separadores decimais)
    // Exemplo: "1500 2500 3750" = 1.500kg × R$ 25,00/kg = R$ 37,50
    private static string TentarFormatarDados(string resposta)
    {
        // Remove os delimitadores STX e ETX para extrair apenas o conteúdo
        int posInicio = resposta.IndexOf((char)_inicioMensagem);
        int posFim = posInicio >= 0 ? resposta.IndexOf((char)_fimMensagem, posInicio + 1) : -1;

        string conteudo;
        if (posInicio >= 0 && posFim > posInicio)
        {
            // Extrai o texto entre STX e ETX
            conteudo = resposta.Substring(posInicio + 1, posFim - posInicio - 1);
        }
        else
        {
            conteudo = resposta;
        }


        // Divide a string em partes separadas por espaço ou tab
        string[] partes = conteudo.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);


        // Tenta converter os valores de string para inteiro
        int.TryParse(partes[0], out int pesoRaw);
        int.TryParse(partes[1], out int precoKgRaw);
        int.TryParse(partes[2], out int totalRaw);

        // Converte os valores inteiros para decimais (divisão por potências de 10)
        // Peso: dividido por 1000 (exemplo: 1500 → 1.500 kg)
        // Preço: dividido por 100 (exemplo: 2500 → R$ 25,00)
        // Total: dividido por 100 (exemplo: 3750 → R$ 37,50)
        decimal peso = pesoRaw / 1000m;
        decimal precoKg = precoKgRaw / 100m;
        decimal total = totalRaw / 100m;


        string pesoFormatado = peso.ToString("F3");
        string precoFormatado = precoKg.ToString("F2");
        string totalFormatado = total.ToString("F2");

        // Monta a string formatada final
        return $"Peso: {pesoFormatado} kg | Preço/Kg: R${precoFormatado} | Total: R${totalFormatado}";

    }
}