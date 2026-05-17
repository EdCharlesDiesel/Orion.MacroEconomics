using MediatR;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Commands
{
    public record GenerateFxSignalsCommand : IRequest<List<FxSignal>>;
}