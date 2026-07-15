export function filterSettingsSections(
  categories: Array<{
    id: string;
    label: string;
    sections: Array<{ id: string; label: string }>;
  }>,
  query: string,
) {
  const normalized = query.trim().toLowerCase();
  if (!normalized) return categories;
  return categories
    .map((category) => ({
      ...category,
      sections: category.sections.filter(
        (section) =>
          section.label.toLowerCase().includes(normalized)
          || category.label.toLowerCase().includes(normalized),
      ),
    }))
    .filter((category) =>
      category.label.toLowerCase().includes(normalized) || category.sections.length > 0);
}
