using Microsoft.AspNetCore.Mvc;
using RedisInventoryDemo.Application.Contracts.Inventory;
using RedisInventoryDemo.Application.Services;
using RedisInventoryDemo.Domain.Entities;

namespace RedisInventoryDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItem>> GetById(int id)
    {
        var item = await _inventoryService.GetByIdAsync(id);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost("{id:int}/reserve")]
    public async Task<ActionResult<InventoryItem>> Reserve(int id, ReserveInventoryRequest request)
    {
        var item = await _inventoryService.ReserveAsync(
            id,
            request);

        if (item is null)
            return Conflict();

        return Ok(item);
    }
}