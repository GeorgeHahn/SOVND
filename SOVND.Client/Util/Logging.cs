using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Anotar.NLog;
using BugSense;
using BugSense.Core.Model;
using NLog;
using NLog.Config;
using NLog.Slack;
using NLog.Targets;
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

        public static IEnumerable<string> Tail(int lines)
        {
            if (memoryTarget == null)
                return null;
            if(memoryTarget.Logs.Count > lines)
                return memoryTarget.Logs.Skip(Math.Max(0, memoryTarget.Logs.Count() - lines)).Take(lines);
            return memoryTarget.Logs;
        }

        private static MemoryTarget memoryTarget;

        public static void SetupLogging(string username)
        {
            Username = username;
            var config = LogManager.Configuration;
            if(config == null)
                config = new LoggingConfiguration();

            var loggername = "asyncslack";
            if (config.FindTargetByName(loggername) != null)
                config.RemoveTarget(loggername);

            var slackTarget = new SlackTarget
            {
                Layout = "${message} ${exception:format=tostring}",
                WebHookUrl = "https://hooks.slack.com/services/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",
                Channel = "#sovnd-client-logs",
                Username = username,
                Compact = true
            };

            AsyncTargetWrapper asyncSlack = new AsyncTargetWrapper(slackTarget);
            config.AddTarget(loggername, asyncSlack);
            var slackTargetRules = new LoggingRule("*", LogLevel.Error, asyncSlack);
            config.LoggingRules.Add(slackTargetRules);


            loggername = "asyncmem";
            if (memoryTarget == null)
            {
                memoryTarget = new MemoryTarget { Layout = "${message} ${exception:format=tostring}" };
                config.AddTarget(loggername, memoryTarget);

                var memTargetRules = new LoggingRule("*", LogLevel.Trace, memoryTarget);
                config.LoggingRules.Add(memTargetRules);

                if (config.FindTargetByName(loggername) != null)
                    config.RemoveTarget(loggername);
            }

            LogManager.Configuration = config;
            BugSenseHandler.Instance.UserIdentifier = username;

            var ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogTo.Error("SOVND Ver {0} running as {1}", ver, username);
        }

        public static void Event(string eventName)
        {
            BugSenseHandler.Instance.SendEventAsync(eventName);
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
                    new CrashExtraData("tail-15", string.Join(Environment.NewLine, Logging.Tail(15))),
                };
                BugSenseHandler.Instance.LogException(exception, extraData);
            });
        }
    }
}
