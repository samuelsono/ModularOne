using System.Security.Claims;
using CarTrack.Api;
using CarTrack.Server.Users.Authorization;

namespace CarTrack.Modules.Expense;

public static class ExpenseEndpoints
{
    public static RouteGroupBuilder MapExpenseEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/categories", GetCategoriesAsync)
            .RequirePermission("expense.claims.read");

        group.MapGet("/settings", GetSettingsAsync)
            .RequirePermission("expense.settings.read");

        group.MapPut("/settings", UpdateSettingsAsync)
            .RequirePermission("expense.settings.write");

        group.MapGet("/admin/categories", GetAdminCategoriesAsync)
            .RequirePermission("expense.categories.read");

        group.MapPost("/admin/categories", CreateCategoryAsync)
            .RequirePermission("expense.categories.write");

        group.MapPut("/admin/categories/{id:guid}", UpdateCategoryAsync)
            .RequirePermission("expense.categories.write");

        group.MapPut("/claims/{id:guid}", UpdateClaimAsync)
            .RequirePermission("expense.claims.write");

        group.MapPost("/claims/{id:guid}/receipt", UploadReceiptAsync)
            .RequirePermission("expense.claims.write");

        group.MapGet("/claims/{id:guid}/receipt", DownloadReceiptAsync)
            .RequirePermission("expense.claims.read");

        group.MapPost("/claims/{id:guid}/submit", SubmitClaimAsync)
            .RequirePermission("expense.claims.write");

        group.MapPost("/claims", CreateClaimAsync)
            .RequirePermission("expense.claims.write");

        group.MapGet("/claims", GetMyClaimsAsync)
            .RequirePermission("expense.claims.read");

        group.MapGet("/claims/{id:guid}", GetClaimAsync)
            .RequirePermission("expense.claims.read");

        group.MapPost("/claims/{id:guid}/cancel", CancelClaimAsync)
            .RequirePermission("expense.claims.write");

        group.MapGet("/approvals/pending", GetPendingApprovalsAsync)
            .RequirePermission("expense.approvals.read");

        group.MapPost("/approvals/{id:guid}/decide", DecideAsync)
            .RequirePermission("expense.approvals.write");

        group.MapGet("/approvals/payment-pending", GetPendingPaymentsAsync)
            .RequirePermission("expense.approvals.read");

        group.MapPost("/approvals/{id:guid}/mark-paid", MarkPaidAsync)
            .RequirePermission("expense.approvals.write");

        group.MapGet("/reports/summary", GetReportSummaryAsync)
            .RequireAnyPermission("expense.reports.read", "expense.claims.read");

        group.MapGet("/reports/balances", GetMyBalancesAsync)
            .RequirePermission("expense.claims.read");

        group.MapGet("/reports/history", GetMyHistoryAsync)
            .RequireAnyPermission("expense.reports.read", "expense.claims.read");

        return group;
    }

    private static async Task<IResult> GetCategoriesAsync(
        IExpenseCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var items = await categoryService.GetActiveCategoriesAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetSettingsAsync(
        IExpenseSettingsService settingsService,
        CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSettingsAsync(
        UpdateExpenseSettingsRequest request,
        IExpenseSettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingsService.UpdateAsync(request, cancellationToken);
            return Results.Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["kilometerRate"] = [ex.Message],
            });
        }
    }

    private static async Task<IResult> GetAdminCategoriesAsync(
        IExpenseCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var items = await categoryService.GetAdminCategoriesAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateCategoryAsync(
        SaveExpenseCategoryRequest request,
        IExpenseCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await categoryService.CreateCategoryAsync(request, cancellationToken);
            return Results.Created($"/api/expense/admin/categories/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create expense category",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid id,
        SaveExpenseCategoryRequest request,
        IExpenseCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await categoryService.UpdateCategoryAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update expense category",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateClaimAsync(
        Guid id,
        UpdateExpenseClaimRequest request,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var updated = await expenseApprovalService.UpdateClaimAsync(id, userId, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to update expense claim",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UploadReceiptAsync(
        Guid id,
        IFormFile receipt,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var updated = await expenseApprovalService.UploadReceiptAsync(id, userId, receipt, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to upload receipt",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> DownloadReceiptAsync(
        Guid id,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var receipt = await expenseApprovalService.GetReceiptAsync(id, userId, cancellationToken);
        return receipt is null ? Results.NotFound() : Results.File(receipt.Value.Stream, receipt.Value.ContentType, receipt.Value.FileName);
    }

    private static async Task<IResult> SubmitClaimAsync(
        Guid id,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var updated = await expenseApprovalService.SubmitClaimAsync(id, userId, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to submit expense claim",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetPendingPaymentsAsync(
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var items = await expenseApprovalService.GetPendingPaymentsAsync(userId, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> MarkPaidAsync(
        Guid id,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var updated = await expenseApprovalService.MarkPaidAsync(id, userId, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to mark expense claim as paid",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> CreateClaimAsync(
        CreateExpenseClaimRequest request,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var created = await expenseApprovalService.CreateClaimAsync(userId, request, cancellationToken);
            return Results.Created($"/api/expense/claims/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to create expense claim",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetMyClaimsAsync(
        string? status,
        int? year,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var items = await expenseApprovalService.GetMyClaimsAsync(userId, status, year, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetClaimAsync(
        Guid id,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var claim = await expenseApprovalService.GetClaimAsync(id, userId, cancellationToken);
        return claim is null ? Results.NotFound() : Results.Ok(claim);
    }

    private static async Task<IResult> CancelClaimAsync(
        Guid id,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var updated = await expenseApprovalService.CancelClaimAsync(id, userId, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to cancel expense claim",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetPendingApprovalsAsync(
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var items = await expenseApprovalService.GetPendingApprovalsAsync(userId, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> DecideAsync(
        Guid id,
        ApprovalDecisionRequest request,
        ClaimsPrincipal principal,
        IExpenseApprovalService expenseApprovalService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var updated = await expenseApprovalService.DecideAsync(id, userId, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Unable to decide expense claim",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetReportSummaryAsync(
        int? year,
        ClaimsPrincipal principal,
        IExpenseReportService reportService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var summary = await reportService.GetSummaryAsync(userId, year, cancellationToken);
        return Results.Ok(summary);
    }

    private static async Task<IResult> GetMyBalancesAsync(
        int? year,
        ClaimsPrincipal principal,
        IExpenseReportService reportService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var balances = await reportService.GetMyBalancesAsync(userId, year, cancellationToken);
        return Results.Ok(balances);
    }

    private static async Task<IResult> GetMyHistoryAsync(
        string? status,
        int? year,
        ClaimsPrincipal principal,
        IExpenseReportService reportService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var history = await reportService.GetMyHistoryAsync(userId, status, year, cancellationToken);
        return Results.Ok(history);
    }

    private static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
