using System.Text;
using FluentAssertions;
using LearnPath.API.Common;
using LearnPath.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace LearnPath.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private static async Task<HttpContext> InvokeAsync(Exception exception)
    {
        var middleware = new ExceptionMiddleware(
            _ => throw exception,
            NullLogger<ExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return context;
    }

    private static async Task<ApiResponse<object>> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ApiResponse<object>>(json)!;
    }

    [Fact]
    public async Task BusinessRuleException_Returns400WithMessage()
    {
        var context = await InvokeAsync(new ArgumentException(
            "Cannot delete learning path \"DemoJava\" because 1 certificate(s) have been issued for it."));

        context.Response.StatusCode.Should().Be(400);
        var body = await ReadBodyAsync(context);
        body.Success.Should().BeFalse();
        body.Message.Should().Be(
            "Cannot delete learning path \"DemoJava\" because 1 certificate(s) have been issued for it.");
    }

    [Fact]
    public async Task ClassroomDependencyException_Returns400WithMessage()
    {
        var context = await InvokeAsync(new ArgumentException(
            "Cannot delete learning path \"Test Path\" because it has 1 classroom(s). Delete or reassign the classroom(s) first."));

        context.Response.StatusCode.Should().Be(400);
        var body = await ReadBodyAsync(context);
        body.Message.Should().Be(
            "Cannot delete learning path \"Test Path\" because it has 1 classroom(s). Delete or reassign the classroom(s) first.");
    }

    [Fact]
    public async Task NotFoundException_Returns404WithMessage()
    {
        var context = await InvokeAsync(new KeyNotFoundException("Learning path not found."));

        context.Response.StatusCode.Should().Be(404);
        var body = await ReadBodyAsync(context);
        body.Message.Should().Be("Learning path not found.");
    }

    [Fact]
    public async Task Unauthorized_Returns403WithMessage()
    {
        var context = await InvokeAsync(new UnauthorizedAccessException("You do not own this learning path."));

        context.Response.StatusCode.Should().Be(403);
        var body = await ReadBodyAsync(context);
        body.Message.Should().Be("You do not own this learning path.");
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithGenericMessage()
    {
        var context = await InvokeAsync(new InvalidOperationException("secret internals: connection string"));

        context.Response.StatusCode.Should().Be(500);
        var body = await ReadBodyAsync(context);
        body.Message.Should().Be("An unexpected error occurred. Please try again.");
    }
}
