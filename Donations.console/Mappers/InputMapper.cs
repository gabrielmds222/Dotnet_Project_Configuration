using CsvHelper.Configuration;
using Donations.Console.Models;

namespace Donations.Console.Mappers;

public class InputMapper : ClassMap<Input>
{
    public InputMapper()
    {
        Map(m => m.Name).Name("name");
        Map(m => m.Email).Name("email");        
    }
}