import AppTitle from '@platform/ui/AppTitle';

export default function TemplatePage() {
  return (
    <div className="flex flex-col gap-3 p-4">
      <AppTitle title="Template module" />
      <p className="text-sm text-neutral-foreground-3">
        Replace this page with your feature UI. Keep imports limited to `@platform/*` and this module.
      </p>
    </div>
  );
}
