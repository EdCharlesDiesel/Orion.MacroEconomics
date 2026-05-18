using MediatR;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Commands
{
    public record GetNormalizedMacroDataQuery(string? Country = null, string? Indicator = null) : IRequest<List<NormalizedIndicator>>;
}
