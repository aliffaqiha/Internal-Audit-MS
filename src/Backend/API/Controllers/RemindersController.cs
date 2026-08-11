using IAMS.Api.Authorization;
using IAMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/admin/reminders")]
[Authorize]
public sealed class RemindersController : ControllerBase
{
    private readonly ICapReminderService _capReminders;

    public RemindersController(ICapReminderService capReminders) => _capReminders = capReminders;

    /// <summary>Runs the CAP due/overdue scan immediately (used for testing/on-demand).</summary>
    [HttpPost("run")]
    [Authorize(Policy = Policies.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RunCapReminders(CancellationToken ct = default)
    {
        await _capReminders.RunOnceAsync(ct);
        return NoContent();
    }
}