using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Reporting.Application;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Results;
using Tameru.Web.Common.Results;

namespace Tameru.Reporting.Api;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/assistant").WithTags("AI Assistant").RequireAuthorization();

        group.MapPost("/chat", async (ChatRequest request, AssistantService service, CancellationToken ct) =>
        {
            var result = await service.ChatAsync(request, ct);
            return Result<ChatResponse>.Success(result).ToHttp();
        });

        group.MapPost("/test-connection", async (TestConnectionRequest request, AssistantService service, CancellationToken ct) =>
        {
            var result = await service.TestConnectionAsync(request, ct);
            return Result<TestConnectionResponse>.Success(result).ToHttp();
        });

        group.MapDelete("/conversation/{id}", (string id, AssistantService service) =>
        {
            service.ClearConversation(id);
            return Result<bool>.Success(true).ToHttp();
        });

        return app;
    }
}
