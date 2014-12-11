using BugSense;
using NLog;
using NLog.Config;
using NLog.Slack;
using NLog.Targets.Wrappers;

namespace SOVND.Client.Util
{
    public static class Logging
    {
        public static void SetupLogging()
        {
            SetupLogging("");
        }

        public static void SetupLogging(string username)
        {
            var config = new LoggingConfiguration();
            var slackTarget = new SlackTarget
            {
                Layout = "${message}",
                WebHookUrl = "https://hooks.slack.com/services/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",
                Channel = "#sovnd-client-logs",
                Username = username,
                Compact = true
            };

            if (config.FindTargetByName("slack") != null)
                config.RemoveTarget("slack");

            AsyncTargetWrapper asyncWrapper = new AsyncTargetWrapper(slackTarget);
            config.AddTarget("async", asyncWrapper);

            var slackTargetRules = new LoggingRule("*", LogLevel.Trace, asyncWrapper);
            config.LoggingRules.Add(slackTargetRules);

            LogManager.Configuration = config;

            BugSenseHandler.Instance.UserIdentifier = username;
        }
    }
}
