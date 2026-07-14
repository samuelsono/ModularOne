using CarTrack.Server.Users.Authorization;

namespace CarTrack.Server.Users;

public static class UserEndpoints
{
  private const string ReadPermission = "platform.settings.users.read";
  private const string WritePermission = "platform.settings.users.write";

  public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
  {
    group.MapGet("/", GetUsersAsync)
      .RequirePermission(ReadPermission);

    group.MapGet("/managers", GetManagersAsync)
      .RequirePermission(ReadPermission);

    group.MapGet("/audit-log", GetAuditLogAsync)
      .RequirePermission(ReadPermission);

    group.MapPost("/from-driver/{driverId:guid}", CreateUserFromDriverAsync)
      .RequirePermission(WritePermission);

    group.MapGet("/{id}/reports", GetDirectReportsAsync)
      .RequirePermission(ReadPermission);

    group.MapGet("/{id}/org", GetUserOrgAsync)
      .RequirePermission(ReadPermission);

    group.MapGet("/{id}", GetUserByIdAsync)
      .RequirePermission(ReadPermission);

    group.MapPost("/", CreateUserAsync)
      .RequirePermission(WritePermission);

    group.MapPut("/{id}", UpdateUserAsync)
      .RequirePermission(WritePermission);

    group.MapPut("/{id}/roles", SetUserRolesAsync)
      .RequirePermission(WritePermission);

    group.MapPatch("/{id}/active", SetUserActiveAsync)
      .RequirePermission(WritePermission);

    group.MapPost("/{id}/invite", SendInviteAsync)
      .RequirePermission(WritePermission);

    return group;
  }

  public static RouteGroupBuilder MapRoleEndpoints(this RouteGroupBuilder group)
  {
    group.MapGet("/", GetRolesAsync)
      .RequirePermission(ReadPermission);

    return group;
  }

  public static RouteGroupBuilder MapPermissionEndpoints(this RouteGroupBuilder group)
  {
    group.MapGet("/", GetPermissionsAsync)
      .RequirePermission(ReadPermission);

    return group;
  }

  private static async Task<IResult> GetUsersAsync(
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var users = await userService.GetUsersAsync(cancellationToken);
    return Results.Ok(users);
  }

  private static async Task<IResult> GetManagersAsync(
    string? excludeUserId,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var managers = await userService.GetManagerOptionsAsync(excludeUserId, cancellationToken);
    return Results.Ok(managers);
  }

  private static async Task<IResult> GetDirectReportsAsync(
    string id,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var reports = await userService.GetDirectReportsAsync(id, cancellationToken);
    return Results.Ok(reports);
  }

  private static async Task<IResult> GetUserOrgAsync(
    string id,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var org = await userService.GetOrgAsync(id, cancellationToken);
    return org is null ? Results.NotFound() : Results.Ok(org);
  }

  private static async Task<IResult> CreateUserFromDriverAsync(
    Guid driverId,
    CreateUserFromDriverRequest request,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    try
    {
      var user = await userService.CreateFromDriverAsync(driverId, request, cancellationToken);
      return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (InvalidOperationException ex)
    {
      return Results.Problem(
        title: "Unable to create user from driver",
        detail: ex.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private static async Task<IResult> GetUserByIdAsync(
    string id,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var user = await userService.GetByIdAsync(id, cancellationToken);
    return user is null ? Results.NotFound() : Results.Ok(user);
  }

  private static async Task<IResult> CreateUserAsync(
    CreateUserRequest request,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var validationError = ValidateCreateRequest(request);
    if (validationError is not null)
    {
      return validationError;
    }

    try
    {
      var user = await userService.CreateAsync(request, cancellationToken);
      return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (InvalidOperationException ex)
    {
      return Results.Problem(
        title: "Unable to create user",
        detail: ex.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private static async Task<IResult> UpdateUserAsync(
    string id,
    UpdateUserRequest request,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Email))
    {
      return Results.ValidationProblem(new Dictionary<string, string[]>
      {
        ["email"] = ["Email is required."],
      });
    }

    try
    {
      var user = await userService.UpdateAsync(id, request, cancellationToken);
      return user is null ? Results.NotFound() : Results.Ok(user);
    }
    catch (InvalidOperationException ex)
    {
      return Results.Problem(
        title: "Unable to update user",
        detail: ex.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private static async Task<IResult> SetUserRolesAsync(
    string id,
    SetUserRolesRequest request,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    try
    {
      var user = await userService.SetRolesAsync(id, request, cancellationToken);
      return user is null ? Results.NotFound() : Results.Ok(user);
    }
    catch (InvalidOperationException ex)
    {
      return Results.Problem(
        title: "Unable to update roles",
        detail: ex.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private static async Task<IResult> SetUserActiveAsync(
    string id,
    SetUserActiveRequest request,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var user = await userService.SetActiveAsync(id, request, cancellationToken);
    return user is null ? Results.NotFound() : Results.Ok(user);
  }

  private static async Task<IResult> SendInviteAsync(
    string id,
    IUserService userService,
    CancellationToken cancellationToken)
  {
    try
    {
      var user = await userService.SendInviteAsync(id, cancellationToken);
      return user is null ? Results.NotFound() : Results.Ok(user);
    }
    catch (InvalidOperationException ex)
    {
      return Results.Problem(
        title: "Unable to send invite",
        detail: ex.Message,
        statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private static async Task<IResult> GetAuditLogAsync(
    int? limit,
    ISecurityAuditService auditService,
    CancellationToken cancellationToken)
  {
    var response = await auditService.GetRecentAsync(limit ?? 100, cancellationToken);
    return Results.Ok(response);
  }

  private static async Task<IResult> GetRolesAsync(
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var roles = await userService.GetRolesAsync(cancellationToken);
    return Results.Ok(roles);
  }

  private static async Task<IResult> GetPermissionsAsync(
    IUserService userService,
    CancellationToken cancellationToken)
  {
    var permissions = await userService.GetPermissionsAsync(cancellationToken);
    return Results.Ok(permissions);
  }

  private static IResult? ValidateCreateRequest(CreateUserRequest request)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Username))
    {
      errors["username"] = ["Username is required."];
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
      errors["email"] = ["Email is required."];
    }

    if (!request.SendInvite && string.IsNullOrWhiteSpace(request.Password))
    {
      errors["password"] = ["Password is required unless sending an invite."];
    }

  if (request.Roles.Count == 0)
    {
      errors["roles"] = ["At least one role is required."];
    }

    return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
  }
}
