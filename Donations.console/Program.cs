using System.Globalization;
using CsvHelper;
using Donations.Console.Mappers;
using Donations.Console.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Channels;

class Program
{
    static async Task Main()
    {
        using StreamReader reader = new("./Donations.console/input.csv");

        // Configuração do CsvHelper
        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",  // O delimitador está definido como ponto e vírgula
            HasHeaderRecord = true,  // O CSV tem cabeçalhos
            HeaderValidated = null,  // Ignora validação de cabeçalhos
            MissingFieldFound = null  // Ignora campos ausentes
        };

        using CsvReader csv = new(reader, config);

        // Registra a classe de mapeamento
        csv.Context.RegisterClassMap<InputMapper>();

        Channel<Input> channel = Channel.CreateUnbounded<Input>();

        // Chama a função ProcessRecordsAsync para processar os registros
        Task processTask = ProcessRecordsAsync(channel);

        // Lê os registros do CSV
        await foreach (Input record in csv.GetRecordsAsync<Input>())
        {
            await channel.Writer.WriteAsync(record);
        }

        channel.Writer.Complete();

        await processTask;
    }

    // Função para processar os registros do CSV
    static async Task ProcessRecordsAsync(Channel<Input> channel)
    {
        const int batchSize = 10;
        List<Input> currentBatch = new List<Input>();

        await foreach (var record in channel.Reader.ReadAllAsync())
        {
            currentBatch.Add(record);

            if (currentBatch.Count == batchSize)
            {
                await SendEmailsInParallelAsync(currentBatch);
                currentBatch.Clear();
            }
        }

        if (currentBatch.Count > 0)
        {
            await SendEmailsInParallelAsync(currentBatch);
        }
    }

    // Função para enviar e-mails em paralelo
    async static Task SendEmailsInParallelAsync(List<Input> batch)
    {
        await Parallel.ForEachAsync(batch, async (item, cts) =>
        {
            await Task.Delay(0, cts); // Simula um atraso controlado
            Console.WriteLine(item.Email);

        //     string remetente = "gabrielmedsilva27@gmail.com"; // Seu e-mail
        //     string senha = "kfrf pund ubkr wwji"; // Sua senha de app ou senha gerada (NÃO use a senha real da conta diretamente em produção)
        //     string smtpServidor = "smtp.gmail.com";
        //     int smtpPorta = 587;

        //     try
        //     {
        //         // Cria a mensagem de e-mail
        //         MailMessage mensagem = new MailMessage
        //         {
        //             From = new MailAddress(remetente),
        //             Subject = "Obrigado por participar!",
        //             Body = $"Seu nome é: {item.Name}",
        //             IsBodyHtml = false
        //         };
        //         mensagem.To.Add(item.Email); // Adiciona o e-mail do destinatário

        //         using (SmtpClient clienteSmtp = new SmtpClient(smtpServidor, smtpPorta))
        //         {
        //             clienteSmtp.Credentials = new NetworkCredential(remetente, senha);
        //             clienteSmtp.EnableSsl = true;
        //             clienteSmtp.Send(mensagem); // Envia o e-mail
        //         }

        //         Console.WriteLine($"✅ E-mail enviado para {item.Email}!");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"❌ Erro ao enviar e-mail para {item.Email}: {ex.Message}");
        //     }
        });
    }
}
