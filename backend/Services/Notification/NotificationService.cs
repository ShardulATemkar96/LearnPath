using LearnPath.API.Data;
using LearnPath.API.DTOs.Notification;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationResponseDto>> GetMyNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task MarkReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
            ?? throw new KeyNotFoundException("Notification not found.");

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread) n.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task CreateAsync(
        string userId, string title, string message, string type)
    {
        await _context.Notifications.AddAsync(new Entities.Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
        });
        await _context.SaveChangesAsync();
    }
}