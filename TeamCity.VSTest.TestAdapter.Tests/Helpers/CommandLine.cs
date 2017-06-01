﻿namespace TeamCity.VSTest.TestAdapter.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    public class CommandLine
    {
        private readonly Dictionary<string, string> _envitonmentVariables = new Dictionary<string, string>();

        public CommandLine(string executableFile, params string[] args)
        {
            if (executableFile == null) throw new ArgumentNullException(nameof(executableFile));
            if (args == null) throw new ArgumentNullException(nameof(args));
            ExecutableFile = executableFile;
            Args = args;
        }

        public string ExecutableFile { [NotNull] get; }

        public string[] Args { [NotNull] get; }

        public void AddEnvitonmentVariable([NotNull] string name, [CanBeNull] string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            _envitonmentVariables.Add(name, value);
        }

        public bool TryExecute(out CommandLineResult result)
        {
            var baseDir = Path.GetFullPath(Path.Combine(GetType().GetTypeInfo().Assembly.Location, "../../../../../"));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = baseDir,
                    FileName = ExecutableFile,
                    Arguments = string.Join(" ", Args.Select(i => $"\"{i}\"").ToArray()),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            foreach (var envVar in _envitonmentVariables)
            {
#if NET45
                if (envVar.Value == null)
                {
                    process.StartInfo.EnvironmentVariables.Remove(envVar.Key);
                }
                else
                {
                    process.StartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                }
#else
                if (envVar.Value == null)
                {
                    process.StartInfo.Environment.Remove(envVar.Key);
                }
                else
                {
                    process.StartInfo.Environment[envVar.Key] = envVar.Value;
                }
#endif
            }

            var stdOut = new StringBuilder();
            var stdError = new StringBuilder();
            process.OutputDataReceived += (sender, args) => { stdOut.AppendLine(args.Data); };
            process.ErrorDataReceived += (sender, args) => { stdError.AppendLine(args.Data); };

            Console.WriteLine($"Run: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            if (!process.Start())
            {
                result = default(CommandLineResult);
                return false;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            result = new CommandLineResult(this, process.ExitCode, stdOut.ToString(), stdError.ToString());
            return true;
        }

        public override string ToString()
        {
            return string.Join(" ", Enumerable.Repeat(ExecutableFile, 1).Concat(Args).Select(i => $"\"{i}\""));
        }
    }
}