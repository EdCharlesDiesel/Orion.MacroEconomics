using MediatR;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Application
{
    public record GetNormalizedMacroDataQuery(string? Country = null, string? Indicator = null) : IRequest<List<NormalizedIndicator>>;
}
