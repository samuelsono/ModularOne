# Settings persistence

`SettingsDbContext` owns `CarTrackSettings` and `PlatformSettings`
(history: `__EFMigrationsHistory_Settings`).

Existing DBs are baselined via probe table `CarTrackSettings`.
Host `RemoveSettingsFromHost` has empty Up/Down (no DropTable).
