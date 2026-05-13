using MediatR;

namespace Orion.MacroEconomics.Commands
{
    public record IngestMacroDataCommand(string Country) : IRequest<int>;
}