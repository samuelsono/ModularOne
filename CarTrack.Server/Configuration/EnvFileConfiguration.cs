namespace CarTrack.Server.Configuration;

public static class EnvFileConfiguration
{
    public static void LoadEnvFile(string path = ".env")
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
