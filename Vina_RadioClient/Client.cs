using VinaFrameworkClient.Core;
using Vina_RadioClient.Modules;

namespace Vina_RadioClient
{
    public class Client : BaseClient
    {
        public Client()
        {
            AddModule(typeof(NuiModule));
            AddModule(typeof(RadioModule));
        }
    }
}
