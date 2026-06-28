
using LearnPath.API.DTOs.Notification; namespace LearnPath.API.Interfaces.Services; public interface INotificationService { Task<List<NotificationResponseDto>> GetMyNotificationsAsync(string userId); Task MarkReadAsync(int notificationId, string userId); Task MarkAllReadAsync(string userId); Task CreateAsync(string userId, string title, string message, string type); }

