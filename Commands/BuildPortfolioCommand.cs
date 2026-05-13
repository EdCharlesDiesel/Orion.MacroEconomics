using MediatR;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Commands
{
    public record BuildPortfolioCommand(decimal Capital) : IRequest<List<PortfolioPosition>>;
}
