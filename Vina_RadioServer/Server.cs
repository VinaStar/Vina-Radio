using VinaFrameworkServer.Core;
using Vina_RadioServer.Modules;

namespace Vina_RadioServer
{
    public class Server : BaseServer
    {
        public Server()
        {
            AddModule(typeof(RadioModule));
        }
    }
}
