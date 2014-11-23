using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOVND.Lib
{
    public interface IMQTTSettings
    {
        string Broker { get; }
        int Port { get; }

        string Username { get; }
        string Password { get; }
    }
}
