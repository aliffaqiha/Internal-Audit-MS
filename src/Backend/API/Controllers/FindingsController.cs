using IAMS.Api.Authorization;
using IAMS.Application.Common;
using IAMS.Application.Common.Interfaces;
using IAMS.Application.Findings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/findings")]
[Authorize]
public sealed class FindingsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IObjectStorageService _storage;

    public FindingsController(ISender sender, IObjectStorageService storage)
    {
        _sender = sender;
        _storage = storage;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FindingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FindingDto>>> Get(
        [FromQuery] GetFindingsQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FindingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FindingDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new GetFindingByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.FindingManager)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateFindingCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.FindingManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateFindingCommand command, CancellationToken ct)
    {
        await _sender.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.FindingManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteFindingCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/evidence")]
    [Authorize(Policy = Policies.FindingManager)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> UploadEvidence(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File bukti wajib dipilih." });

        var evidenceId = await _sender.Send(new AddEvidenceCommand(
            id,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length,
            file.OpenReadStream()), ct);

        return CreatedAtAction(nameof(GetById), new { id }, evidenceId);
    }

    [HttpGet("{id:guid}/evidence/{evidenceId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadEvidence(Guid id, Guid evidenceId, CancellationToken ct)
    {
        var dto = await _sender.Send(new GetEvidenceDownloadQuery(id, evidenceId), ct);
        var stream = await _storage.GetAsync(dto.StoredObjectName, ct);
        if (stream is null)
            return NotFound();

        return File(stream, dto.ContentType, dto.OriginalFileName);
    }
}