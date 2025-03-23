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
            Delimiter = ";",  
            HasHeaderRecord = true,  
            HeaderValidated = null,  
            MissingFieldFound = null
        };

        using CsvReader csv = new(reader, config);

       
        csv.Context.RegisterClassMap<InputMapper>();

        Channel<Input> channel = Channel.CreateUnbounded<Input>();

        
        Task processTask = ProcessRecordsAsync(channel);

    
        await foreach (Input record in csv.GetRecordsAsync<Input>())
        {
            await channel.Writer.WriteAsync(record);
        }

        channel.Writer.Complete();

        await processTask;
    }


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

    
 async static Task SendEmailsInParallelAsync(List<Input> batch)
{
    await Parallel.ForEachAsync(batch, async (item, cts) =>
    {
        await Task.Delay(0, cts); 
        // Console.WriteLine(item.Email);

        string remetente = "# Seu Email aqui #"; 
        string senha = "# Sua senha aqui #"; 
        string smtpServidor = "smtp.gmail.com";
        int smtpPorta = 587;
        string imagemPath = "./Donations.console/imagem_doacao.jpeg"; // Caminho da imagem

        try
        {
            // Criar corpo do e-mail
            string corpoEmail = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color: #333; text-align: center;'>
                    <h2 style='color: #27ae60;'>Muito obrigado por sua participação, {item.Name}!</h2>
                    <p style='font-size: 16px;'>Sua colaboração é essencial para o sucesso da nossa iniciativa. 
                    Juntos, podemos alcançar grandes objetivos!</p>
                    <p style='font-size: 14px;'>Enviamos em anexo uma lembrança especial para você.</p>
                    <p><strong>Equipe da Eco</strong></p>
                </body>
                </html>";

            MailMessage mensagem = new MailMessage
            {
                From = new MailAddress(remetente),
                Subject = "Obrigado por participar!",
                Body = corpoEmail,
                IsBodyHtml = true
            };
            mensagem.To.Add(item.Email); 

            // Anexar a imagem ao e-mail, se ela existir
            if (File.Exists(imagemPath))
            {
                Attachment imagemAnexo = new Attachment(imagemPath);
                mensagem.Attachments.Add(imagemAnexo);
            }

            using (SmtpClient clienteSmtp = new SmtpClient(smtpServidor, smtpPorta))
            {
                clienteSmtp.Credentials = new NetworkCredential(remetente, senha);
                clienteSmtp.EnableSsl = true;
                clienteSmtp.Send(mensagem); 
            }

            Console.WriteLine($"✅ E-mail enviado para {item.Email}!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao enviar e-mail para {item.Email}: {ex.Message}");
        }
    });
}

}
