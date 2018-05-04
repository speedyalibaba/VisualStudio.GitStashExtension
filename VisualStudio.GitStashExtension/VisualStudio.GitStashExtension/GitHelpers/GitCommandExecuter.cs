﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using VisualStudio.GitStashExtension.Models;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System.Threading.Tasks;
using Log = VisualStudio.GitStashExtension.Logger.Logger;

namespace VisualStudio.GitStashExtension.GitHelpers
{
    /// <summary>
    /// Represents service for executing git commands on current repositoty.
    /// </summary>
    public class GitCommandExecuter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGitExt _gitService;

        public GitCommandExecuter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _gitService = (IGitExt)_serviceProvider.GetService(typeof(IGitExt));
        }

        /// <summary>
        /// Gets all stashes for current repositoty.
        /// </summary>
        /// <param name="stashes">List of stashes for current repositoty.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TryGetAllStashes(out IList<Stash> stashes, out string errorMessage)
        {
            var commandResult = Execute(GitCommandConstants.StashList);
            if (commandResult.IsError)
            {
                errorMessage = commandResult.ErrorMessage;
                stashes = null;
                return false;
            }

            stashes = GitResultParser.ParseStashListResult(commandResult.OutputMessage);
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Applies stash to current repository state by stash id.
        /// </summary>
        /// <param name="stashId">Stash Id.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TryApplyStash(int stashId, out string errorMessage)
        {
            var applyCommand = string.Format(GitCommandConstants.StashApplyFormatted, stashId);
            var commandResult = Execute(applyCommand);

            errorMessage = commandResult.ErrorMessage;
            return !commandResult.IsError;
        }

        /// <summary>
        /// Creates stash on current branch.
        /// </summary>
        /// <param name="message">Save message for stash.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TryCreateStash(string message, out string errorMessage)
        {
            var createCommand = string.IsNullOrEmpty(message) ? 
                GitCommandConstants.Stash : 
                string.Format(GitCommandConstants.StashSaveFormatted, message);

            var commandResult = Execute(createCommand);

            errorMessage = commandResult.ErrorMessage;
            return !commandResult.IsError;
        }

        /// <summary>
        /// Creates stash on current branch(only staged files).
        /// </summary>
        /// <param name="message">Save message for stash.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TryCreateStashStaged(string message, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Stash everything temporarily.  Keep staged files, discard everything else after stashing.
            var commandResult = ExecuteWithCmd(GitCommandConstants.StashAllKeepStaged);
            if (commandResult.IsError)
            {
                errorMessage = commandResult.ErrorMessage;
                return false;
            }

            // Stash everything that remains (only the staged files should remain)  This is the stash we want to keep, so give it a name.
            if (!TryCreateStash(message, out errorMessage))
                return false;

            // Apply the original stash to get us back to where we started.
            if (!TryApplyStash(1, out errorMessage))
                return false;

            //Create a temporary patch to reverse the originally staged changes and apply it
            commandResult = ExecuteWithCmd(GitCommandConstants.CreatePatchAndApply, true);
            if (commandResult.IsError)
            {
                errorMessage = commandResult.ErrorMessage;
                return false;
            }

            //Delete the temporary stash
            return TryDeleteStash(1, out errorMessage);
        }

        /// <summary>
        /// Delete stash by id.
        /// </summary>
        /// <param name="id">Stash id.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TryDeleteStash(int id, out string errorMessage)
        {
            var deleteCommand = string.Format(GitCommandConstants.StashDeleteFormatted, id);

            var commandResult = Execute(deleteCommand);

            errorMessage = commandResult.ErrorMessage;
            return !commandResult.IsError;
        }

        /// <summary>
        /// Gets stash info by id.
        /// </summary>
        /// <param name="id">Stash id.</param>
        /// <param name="stash">Stash model.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TryGetStashInfo(int id, out Stash stash, out string errorMessage)
        {
            var infoCommand = string.Format(GitCommandConstants.StashInfoFormatted, id);

            var commandResult = Execute(infoCommand);

            if (commandResult.IsError)
            {
                errorMessage = commandResult.ErrorMessage;
                stash = null;
                return false;
            }

            errorMessage = string.Empty;
            stash = GitResultParser.ParseStashInfoResult(commandResult.OutputMessage);
            return true;
        }

        /// <summary>
        /// Saves temp file after stash version of specific file.
        /// </summary>
        /// <param name="id">Stash id.</param>
        /// <param name="filePath">Path to the specific file.</param>
        /// <param name="pathToSave">Path for saving temp file.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TrySaveFileAfterStashVersion(int id, string filePath, string pathToSave, out string errorMessage)
        {
            var afterFileCreateCommand = string.Format(GitCommandConstants.AfterStashFileVersionSaveTempFormatted, id, filePath, pathToSave);

            var commandResult = ExecuteWithCmd(afterFileCreateCommand);

            if (commandResult.IsError)
            {
                errorMessage = commandResult.ErrorMessage;
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Saves temp file before stash version of specific file.
        /// </summary>
        /// <param name="id">Stash id.</param>
        /// <param name="filePath">Path to the specific file.</param>
        /// <param name="pathToSave">Path for saving temp file.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>Bool value that indicates whether command execution was succeeded.</returns>
        public bool TrySaveFileBeforeStashVersion(int id, string filePath, string pathToSave, out string errorMessage)
        {
            var beforeFileCreateCommand = string.Format(GitCommandConstants.BeforeStashFileVersionSaveTempFormatted, id, filePath, pathToSave);

            var commandResult = ExecuteWithCmd(beforeFileCreateCommand);

            if (commandResult.IsError)
            {
                errorMessage = commandResult.ErrorMessage;
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private GitCommandResult Execute(string gitCommand)
        {
            try
            {
                var activeRepository = _gitService.ActiveRepositories.FirstOrDefault();
                if (activeRepository == null)
                    return new GitCommandResult { ErrorMessage = Constants.UnknownRepositoryErrorMessage };

                var gitExePath = GitPathHelper.GetGitPath();

                var gitStartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = (gitExePath ?? "git.exe"),
                    Arguments = gitCommand,
                    WorkingDirectory = activeRepository.RepositoryPath
                };

                using (var gitProcess = Process.Start(gitStartInfo))
                {
                    var errorMessage = Task.Run(() => gitProcess.StandardError.ReadToEndAsync());
                    var outputMessage = Task.Run(() => gitProcess.StandardOutput.ReadToEndAsync());

                    gitProcess.WaitForExit();

                    return new GitCommandResult
                    {
                        OutputMessage = outputMessage.Result,
                        ErrorMessage = errorMessage.Result
                    };
                }
            }
            catch (Win32Exception e)
            {
                Log.LogException(e);
                if (e.TargetSite.Name != "StartWithCreateProcess")
                    throw;

                return new GitCommandResult { ErrorMessage = Constants.UnableFindGitMessage };
            }
            catch (Exception e)
            {
                Log.LogException(e);
                return new GitCommandResult { ErrorMessage = Constants.UnexpectedErrorMessage + Environment.NewLine + $"Find error info in {Log.GetLogFilePath()}" };
            }
        }

        /// <summary>
        /// This method is a part of new functionality. To save backward compatibility previous implementation was left.
        /// </summary>
        /// <param name="gitCommand"></param>
        /// <returns></returns>
        private GitCommandResult ExecuteWithCmd(string gitCommand, bool isCmdCommand = false)
        {
            try
            {
                var activeRepository = _gitService.ActiveRepositories.FirstOrDefault();
                if (activeRepository == null)
                    return new GitCommandResult {ErrorMessage = Constants.UnknownRepositoryErrorMessage };

                var gitExePath = GitPathHelper.GetGitPath();
                var cmdCommand = isCmdCommand ? "/C \"" + gitCommand + "\"" : "/C \"\"" + (gitExePath ?? "git.exe") + "\" " + gitCommand + "\"";

                var gitStartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = "cmd.exe",
                    Arguments = cmdCommand,
                    WorkingDirectory = activeRepository.RepositoryPath
                };

                using (var gitProcess = Process.Start(gitStartInfo))
                {
                    var errorMessage = Task.Run(() => gitProcess.StandardError.ReadToEndAsync()); 
                    var outputMessage = Task.Run(() => gitProcess.StandardOutput.ReadToEndAsync());

                    gitProcess.WaitForExit();

                    return new GitCommandResult
                    {
                        OutputMessage = outputMessage.Result,
                        ErrorMessage = errorMessage.Result
                    };
                }
            }
            catch(Exception e)
            {
                Log.LogException(e);
                return new GitCommandResult { ErrorMessage = Constants.UnexpectedErrorMessage + Environment.NewLine + $"Find error info in {Log.GetLogFilePath()}" };
            }
        }

    }
}
