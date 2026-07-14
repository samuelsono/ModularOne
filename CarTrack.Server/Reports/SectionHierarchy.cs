using CarTrack.Server.Data;

namespace CarTrack.Server.Reports;

public static class SectionHierarchy
{
    public const int MaxDepth = 3;

    public static int GetDepth(DashboardSection section, IReadOnlyDictionary<Guid, DashboardSection> sectionsById)
    {
        var depth = 1;

        var currentParentId = section.ParentSectionId;
        while (currentParentId.HasValue)
        {
            if (!sectionsById.TryGetValue(currentParentId.Value, out var parent))
            {
                break;
            }

            depth++;
            currentParentId = parent.ParentSectionId;

            if (depth > MaxDepth)
            {
                break;
            }
        }

        return depth;
    }

    public static bool IsDescendant(
        Guid potentialAncestorId,
        Guid sectionId,
        IReadOnlyDictionary<Guid, DashboardSection> sectionsById)
    {
        if (!sectionsById.TryGetValue(sectionId, out var current))
        {
            return false;
        }

        var parentId = current.ParentSectionId;
        while (parentId.HasValue)
        {
            if (parentId.Value == potentialAncestorId)
            {
                return true;
            }

            if (!sectionsById.TryGetValue(parentId.Value, out var parent))
            {
                break;
            }

            parentId = parent.ParentSectionId;
        }

        return false;
    }

    public static IReadOnlyDictionary<Guid, DashboardSection> IndexSections(IEnumerable<DashboardSection> sections) =>
        sections.ToDictionary(section => section.Id);

    public static IReadOnlyList<DashboardSection> GetRootSections(IEnumerable<DashboardSection> sections) =>
        sections
            .Where(section => section.ParentSectionId is null)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Title)
            .ToList();

    public static IReadOnlyList<DashboardSection> GetChildSections(
        IEnumerable<DashboardSection> sections,
        Guid parentSectionId) =>
        sections
            .Where(section => section.ParentSectionId == parentSectionId)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Title)
            .ToList();
}
