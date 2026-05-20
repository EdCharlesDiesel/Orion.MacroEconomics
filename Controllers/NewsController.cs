using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ForexEventsController(IForexEventRepository eventRepository, INewsEngine newsEngine, ILogger<ForexEventsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ForexEvent>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var events = await eventRepository.GetAllAsync(page, pageSize);
        return Ok(events);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<ForexEvent>>> GetUpcoming([FromQuery] int days = 7)
    {
        var events = await eventRepository.GetUpcomingEventsAsync(days);
        return Ok(events);
    }

    [HttpGet("high-impact")]
    public async Task<ActionResult<IEnumerable<ForexEvent>>> GetHighImpactEvents([FromQuery] int days = 3)
    {
        var events = await newsEngine.GetHighImpactEventsAsync(days);
        return Ok(events);
    }

    [HttpGet("by-currency/{currency}")]
    public async Task<ActionResult<IEnumerable<ForexEvent>>> GetByCurrency(
        string currency,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var events = await eventRepository.GetByCurrencyAsync(currency.ToUpper(), from, to);
        return Ok(events);
    }

    [HttpGet("strength")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetCurrencyStrength()
    {
        var strength = await newsEngine.CalculateCurrencyStrengthAsync();
        return Ok(strength);
    }

    [HttpGet("next-major")]
    public async Task<ActionResult<ForexEvent?>> GetNextMajorEvent([FromQuery] string? currency = null)
    {
        var nextEvent = await newsEngine.GetNextMajorEventAsync(currency?.ToUpper());
        if (nextEvent == null)
            return NotFound("No upcoming major events found");
        return Ok(nextEvent);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshData()
    {
        logger.LogInformation("Manual refresh triggered");
        var newEvents = await newsEngine.FetchAndProcessEventsAsync();
        return Ok(new { Message = $"Refreshed {newEvents.Count()} new events", Events = newEvents });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ForexEvent>> GetById(Guid id)
    {
        var forexEvent = await eventRepository.GetByIdAsync(id);
        if (forexEvent == null)
            return NotFound();
        return Ok(forexEvent);
    }

    [HttpPost]
    public async Task<ActionResult<ForexEvent>> Create(CreateForexEventDto createDto)
    {
        var forexEvent = new ForexEvent
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid().ToString(),
            Currency = createDto.Currency,
            Title = createDto.Title,
            Description = createDto.Description,
            EventDate = createDto.EventDate,
            Actual = createDto.Actual,
            Forecast = createDto.Forecast,
            Previous = createDto.Previous,
            Impact = createDto.Impact,
            RetrievedAt = DateTime.UtcNow
        };

        await eventRepository.AddAsync(forexEvent);
        return CreatedAtAction(nameof(GetById), new { id = forexEvent.Id }, forexEvent);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await eventRepository.DeleteAsync(id);
        return NoContent();
    }
}

