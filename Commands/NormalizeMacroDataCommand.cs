using MediatR;

namespace Orion.MacroEconomics.Commands
{
    public record NormalizeMacroDataCommand(bool ForceRefresh = false) : IRequest<int>;
}