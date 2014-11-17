using Charlotte;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            (new Server()).Run();
        }
    }

    public class Server : MqttModule
    {
        public Action<string> Log = _ => Console.WriteLine(_);

        private Dictionary<string, int> votes = new Dictionary<string, int>();
        private Dictionary<string, bool> uservotes = new Dictionary<string, bool>();

        public Server()
            : base("127.0.0.1", 1883, "server", "serverpass")
        {
            Log("Starting up");

            On["/user/{username}/{channel}/songs/{songid}"] = _ =>
            {
                if (_.Message == "vote")
                {
                    Log("\{_.username} voted for song \{_.songid}");

                    if (!uservotes.ContainsKey(_.username + _.songid) || !uservotes[_.username + _.songid])
                    {
                        Log("Vote was valid");

                        if (!votes.ContainsKey(_.songid))
                            votes[_.songid] = 0;
                        votes[_.songid]++;
                        uservotes[_.username + _.songid] = true;

                        // TODO: Publish vote to channel topic
                    }
                    else
                    {
                        Log("Vote was invalid");
                        return;
                    }
                }
                else if (_.Message == "unvote")
                {
                    Log(":\{_.username} unvoted for song :\{_.songid}");

                    // TODO if songid is valid
                    if (uservotes[_.username + _.songid])
                    {
                        votes[_.songid]--;
                        uservotes[_.username + _.songid] = false;
                    }
                    else
                        return;
                }
                else if (_.Message == "report")
                {
                    Log("\{_.username} reported song \{_.songid} on \{_.channel}");

                    // TODO Record report
                }
                else if (_.Message == "remove")
                {
                    Log("\{_.username} removed song \{_.songid} on \{_.channel}");

                    // TODO Verify priveleges
                    // TODO Remove song
                }
                else if (_.Message == "block")
                {
                    Log("\{_.username} blocked song \{_.songid} on \{_.channel}");

                    // TODO Verify priveleges
                    // TODO Remove song
                    // TODO Record block
                }

                else
                {
                    Log("Invalid command: \{_.Topic}: \{_.Message}");
                    return;
                }

                Publish("/\{_.channel}/playlist/\{_.songid}/votes", votes[_.songid].ToString());
                // TODO votetime
                // TODO voters
            };

            On["/user/{username}/{channel}/chat"] = _ =>
            {
                Log("\{_.username} said \{_.Message} on \{_.channel}");

                // TODO [LOW] Log chats
                // TODO [LOW] Pass chats from this topic to a channel chat topic
            };
;
            Log("Running");
        }
    }
}
