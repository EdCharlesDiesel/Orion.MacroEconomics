using MediatR;
using Orion.MacroEconomics.Commands;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Handlers
{


    public class GetNormalizedMacroDataHandler(IRepository<NormalizedIndicator> repo)
        : IRequestHandler<GetNormalizedMacroDataQuery, List<NormalizedIndicator>>
    {
        public async Task<List<NormalizedIndicator>> Handle(
            GetNormalizedMacroDataQuery request,
            CancellationToken ct)
        {
            var data = await repo.GetAllAsync();

            // Optional filtering
            if (!string.IsNullOrWhiteSpace(request.Country))
            {
                data = data.Where(x => x.Country == request.Country);
            }

            if (!string.IsNullOrWhiteSpace(request.Indicator))
            {
                data = data.Where(x => x.Indicator.Contains(request.Indicator));
            }

            // IMPORTANT: Only return latest per (Country + Indicator)
            var latest = data
                .GroupBy(x => new { x.Country, x.Indicator })
                .Select(g => g.OrderByDescending(x => x.Date).First())
                .ToList();

            return latest;
        }
    }
}
