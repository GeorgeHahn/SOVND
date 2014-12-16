using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Anotar.NLog;
using BugSense;
using BugSense.Core.Model;
using NLog;
using NLog.Config;
using NLog.Slack;
using NLog.Targets.Wrappers;

namespace SOVND.Client.Util
{
    public static class Logging
    {
        public static string Username { get; private set; }

        public static void SetupLogging()
        {
            SetupLogging("");
        }

        public static void SetupLogging(string username)
        {
            Username = username;
            var config = LogManager.Configuration;
            if(config == null)
                config = new LoggingConfiguration();

            var slackTarget = new SlackTarget
            {
                Layout = "${message}",
                WebHookUrl = "https://hooks.slack.com/services/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",
                Channel = "#sovnd-client-logs",
                Username = username,
                Compact = true
            };

            var loggername = "asyncslack";
            if (config.FindTargetByName(loggername) != null)
                config.RemoveTarget(loggername);

            AsyncTargetWrapper asyncWrapper = new AsyncTargetWrapper(slackTarget);
            config.AddTarget(loggername, asyncWrapper);

            var slackTargetRules = new LoggingRule("*", LogLevel.Error, asyncWrapper);
            config.LoggingRules.Add(slackTargetRules);

            LogManager.Configuration = config;

            BugSenseHandler.Instance.UserIdentifier = username;

            var ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogTo.Error("SOVND Ver {0} running as {1}", ver, username);
        }
    }

    public static class AsyncErrorHandler
    {
        public static void HandleException(Exception exception)
        {
            Task.Run(() =>
            {
                var extraData = new LimitedCrashExtraDataList
                {
                    new CrashExtraData("username", Logging.Username),
                    new CrashExtraData("tail-15", "goes here")
                };

                BugSenseLogResult logResult = BugSenseHandler.Instance.LogException(exception, extraData);
            });
        }
    }
}
