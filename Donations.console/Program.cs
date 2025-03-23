using System.Globalization;
using CsvHelper;
using Donations.Console.Mappers;
using Donations.Console.Models;
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

        Task processTask = ProcessRecordsAsync(channel);

        // Lê os registros do CSV
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
            Console.WriteLine(item.Name); // Aqui você pode adicionar lógica para enviar email
        });
    }
}
