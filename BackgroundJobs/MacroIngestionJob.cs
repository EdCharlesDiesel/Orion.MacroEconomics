using MediatR;
using Orion.MacroEconomics.Commands;
using Quartz;

namespace Orion.MacroEconomics.BackgroundJobs
{
    public class MacroIngestionJob(IMediator mediator) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var countries = new[]
            {
                "United States",
                "Euro Area",
                "Japan",
                "United Kingdom",
                "South Africa"
            };

            foreach (var country in countries)
            {
                var count = await mediator.Send(new IngestMacroDataCommand(country));

                Console.WriteLine($"[{country}] Ingested: {count} records");
            }
        }
    }
}