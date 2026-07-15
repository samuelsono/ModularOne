using CarTrack.Server.CarTrack;
using Microsoft.EntityFrameworkCore;

namespace CarTrack.Modules.Reporting;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/metadata", GetMetadata)
            .RequireAuthorization();

        group.MapGet("/", GetReportsAsync)
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetReportByIdAsync)
            .RequireAuthorization();

        group.MapPost("/", CreateReportAsync)
            .RequireAuthorization();

        group.MapPut("/{id:guid}", UpdateReportAsync)
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", DeleteReportAsync)
            .RequireAuthorization();

        group.MapPost("/preview", PreviewReportAsync)
            .RequireAuthorization();

        group.MapPost("/{id:guid}/execute", ExecuteReportAsync)
            .RequireAuthorization();

        return group;
    }

    private static IResult GetMetadata(IReportService reportService) =>
        Results.Ok(reportService.GetMetadata());

    private static async Task<IResult> GetReportsAsync(
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var reports = await reportService.GetAllAsync(cancellationToken);
        return Results.Ok(reports);
    }

    private static async Task<IResult> GetReportByIdAsync(
        Guid id,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var report = await reportService.GetByIdAsync(id, cancellationToken);
        return report is null ? Results.NotFound() : Results.Ok(report);
    }

    private static async Task<IResult> CreateReportAsync(
        SaveReportRequest request,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(reportService.Validate(request));
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var report = await reportService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/reports/{report.Id}", report);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sectionId"] = [ex.Message],
            });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Section not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    private static async Task<IResult> UpdateReportAsync(
        Guid id,
        SaveReportRequest request,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(reportService.Validate(request));
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var report = await reportService.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(report);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Report not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (DbUpdateException ex)
        {
            return Results.Problem(
                title: "Report update failed",
                detail: ex.InnerException?.Message ?? ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> DeleteReportAsync(
        Guid id,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        await reportService.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PreviewReportAsync(
        SaveReportRequest request,
        IReportService reportService,
        IReportQueryService reportQueryService,
        CancellationToken cancellationToken)
    {
        var validationError = ToValidationResult(reportService.Validate(request));
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var result = await reportQueryService.ExecutePreviewAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Report preview failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (CarTrackApiException ex)
        {
            return Results.Problem(
                title: "CarTrack API request failed",
                detail: ex.Message,
                statusCode: ex.StatusCode is >= 400 and < 600 ? ex.StatusCode : StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> ExecuteReportAsync(
        Guid id,
        IReportQueryService reportQueryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await reportQueryService.ExecuteAsync(id, cancellationToken);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(
                title: "Report not found",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Report execution failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (CarTrackApiException ex)
        {
            return Results.Problem(
                title: "CarTrack API request failed",
                detail: ex.Message,
                statusCode: ex.StatusCode is >= 400 and < 600 ? ex.StatusCode : StatusCodes.Status502BadGateway);
        }
    }

    private static IResult? ToValidationResult(IReadOnlyDictionary<string, string[]>? errors) =>
        errors is null ? null : Results.ValidationProblem(errors);
}
