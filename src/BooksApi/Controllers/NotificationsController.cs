using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUserService _user;

    public NotificationsController(INotificationService notifications, ICurrentUserService user)
    {
        _notifications = notifications;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> List() =>
        Ok(await _notifications.ListAsync(_user.RequireUserId()));

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount() =>
        Ok(new { count = await _notifications.GetUnreadCountAsync(_user.RequireUserId()) });

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        await _notifications.MarkReadAsync(_user.RequireUserId(), id);
        return NoContent();
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(_user.RequireUserId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _notifications.DeleteAsync(_user.RequireUserId(), id);
        return NoContent();
    }
}
