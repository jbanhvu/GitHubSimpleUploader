using System.Diagnostics;
using System.Text;

namespace GitHubSimpleUploader;

public sealed class GitService
{
    public async Task<GitCommandResult> RunGitCommandAsync(
        string workingDirectory,
        string arguments,
        Action<string>? outputReceived = null,
        CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GCM_INTERACTIVE"] = "Auto";

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            output.AppendLine(e.Data);
            outputReceived?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            error.AppendLine(e.Data);
            outputReceived?.Invoke(e.Data);
        };

        try
        {
            if (!process.Start())
            {
                return new GitCommandResult
                {
                    ExitCode = -1,
                    StandardError = "KhÃ´ng thá»ƒ khá»Ÿi Ä‘á»™ng Git."
                };
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            process.WaitForExit();

            return new GitCommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = output.ToString(),
                StandardError = error.ToString()
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new GitCommandResult
            {
                ExitCode = -1,
                StandardError = "KhÃ´ng tÃ¬m tháº¥y Git. Vui lÃ²ng cÃ i Git for Windows vÃ  má»Ÿ láº¡i á»©ng dá»¥ng."
            };
        }
        catch (OperationCanceledException)
        {
            return new GitCommandResult
            {
                ExitCode = -1,
                StandardError = "Lá»‡nh Git Ä‘Ã£ bá»‹ há»§y."
            };
        }
        catch (Exception ex)
        {
            return new GitCommandResult
            {
                ExitCode = -1,
                StandardError = $"CÃ³ lá»—i khi cháº¡y Git: {ex.Message}"
            };
        }
    }

    public static string QuoteArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var quoted = new StringBuilder();
        quoted.Append('"');

        var backslashCount = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                quoted.Append('\\', backslashCount * 2 + 1);
                quoted.Append('"');
                backslashCount = 0;
                continue;
            }

            quoted.Append('\\', backslashCount);
            quoted.Append(character);
            backslashCount = 0;
        }

        quoted.Append('\\', backslashCount * 2);
        quoted.Append('"');

        return quoted.ToString();
    }
}

