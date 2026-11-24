using DevTrack.Api.Auth.Authorize.Tasks;
using DevTrack.Shared.Security;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly IBatchService _batchService;

    public BatchesController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet]
    [Permission(Permissions.BatchesRead)]
    public async Task<IActionResult> GetBatches([FromQuery] PaginationRequest request)
    {
        var result = await _batchService.GetBatchesAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Permission(Permissions.BatchesRead)]
    public async Task<IActionResult> GetBatchById(int id)
    {
        var result = await _batchService.GetBatchByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission(Permissions.BatchesWrite)]
    public async Task<IActionResult> CreateBatch([FromBody] BatchRequest request)
    {
        var result = await _batchService.CreateBatchAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}/developers")]
    [Permission(Permissions.BatchesRead)]
    public async Task<IActionResult> GetBatchDevelopers(int id)
    {
        var result = await _batchService.GetBatchDevelopersAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/assignments")]
    [Permission(Permissions.BatchesWrite)]
    public async Task<IActionResult> UpdateAssignments(int id, [FromBody] List<int> selectedDeveloperIds)
    {
        var result = await _batchService.UpdateBatchAssignmentsAsync(id, selectedDeveloperIds);
        return Ok(result);
    }
}
