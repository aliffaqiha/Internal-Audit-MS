using IAMS.Api.Authorization;
using IAMS.Application.Common;
using IAMS.Application.Common.Interfaces;
using IAMS.Application.CorrectiveActions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/caps")]
[Authorize]
public sealed class CapsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IObjectStorageService _storage;

    public CapsController(ISender sender, IObjectStorageService storage)
    {
        _sender = sender;
        _storage = storage;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CorrectiveActionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CorrectiveActionDto>>> Get(
        [FromQuery] GetCapsQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));

    [HttpGet("finding/{findingId:guid}")]
    [ProducesResponseType(typeof(CorrectiveActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CorrectiveActionDto?>> GetByFinding(Guid findingId, CancellationToken ct)
        => Ok(await _sender.Send(new GetCapByFindingQuery(findingId), ct));

    [HttpPost]
    [Authorize(Policy = Policies.CapEditor)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateCapCommand command, CancellationToken ct)
    {
        var id = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetByFinding), new { findingId = command.FindingId }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CapEditor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, UpdateCapCommand command, CancellationToken ct)
    {
        await _sender.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Policies.CapEditor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        await _sender.Send(new StartCapCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Policies.CapEditor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await _sender.Send(new SubmitCapCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/verify")]
    [Authorize(Policy = Policies.CapVerifier)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyCapCommand command, CancellationToken ct)
    {
        await _sender.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/attachment")]
    [Authorize(Policy = Policies.CapEditor)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File lampiran wajib dipilih." });

        await _sender.Send(new UploadCapAttachmentCommand(
            id,
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.Length,
            file.OpenReadStream()), ct);

        return NoContent();
    }

    [HttpGet("{id:guid}/attachment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken ct)
    {
        var dto = await _sender.Send(new GetCapAttachmentQuery(id), ct);
        var stream = await _storage.GetAsync(dto.StoredObjectName, ct);
        if (stream is null)
            return NotFound();

        return File(stream, dto.ContentType, dto.FileName);
    }
}