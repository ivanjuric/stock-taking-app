using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Services;
using StockTakingApp.Mapping;

namespace StockTakingApp.Controllers;

[Authorize]
public sealed class NotificationsController(
    INotificationService notificationService, 
    INotificationHub notificationHub) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Notifications";

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await notificationService.GetUserNotificationsAsync(userId, 50);

        var model = new NotificationListViewModel
        {
            Notifications = notifications
                .Select(n => n.ToViewModel(n.CreatedAt.ToTimeAgo()))
                .ToList(),
            UnreadCount = notifications.Count(n => !n.IsRead)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var count = await notificationService.GetUnreadCountAsync(userId);
        
        if (count == 0)
            return Content("");

        return Content(count.ToString());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await notificationService.MarkAsReadAsync(id, userId);

        if (Request.Headers.ContainsKey("HX-Request"))
            return Ok();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await notificationService.MarkAllAsReadAsync(userId);

        if (Request.Headers.ContainsKey("HX-Request"))
            return Ok();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var channel = Channel.CreateUnbounded<Notification>();

        notificationHub.Subscribe(userId, channel);

        try
        {
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var html = $"""
                    <div class="toast" id="toast-{notification.Id}">
                        <div class="toast-header">
                            <strong class="toast-title">{notification.Title}</strong>
                            <button class="toast-close" onclick="this.parentElement.parentElement.remove()">&times;</button>
                        </div>
                        <div class="toast-body">
                            {notification.Message}
                            {(notification.Link is not null ? $"""<a href="{notification.Link}" class="btn btn-sm btn-primary mt-sm">View</a>""" : "")}
                        </div>
                    </div>
                    """;

                // SSE format: event name + data
                await Response.WriteAsync($"event: message\n", cancellationToken);
                await Response.WriteAsync($"data: {html.Replace("\n", "").Replace("\r", "")}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                // Also trigger notification count update
                await Response.WriteAsync($"event: notification-update\n", cancellationToken);
                await Response.WriteAsync($"data: update\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            notificationHub.Unsubscribe(userId, channel);
        }
    }
}
