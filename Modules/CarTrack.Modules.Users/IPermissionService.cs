namespace CarTrack.Modules.Users;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetModulesFromPermissions(IEnumerable<string> permissions);
}
