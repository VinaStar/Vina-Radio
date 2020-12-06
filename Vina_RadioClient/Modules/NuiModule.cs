using VinaFrameworkClient.Core;
using Vina_RadioClient.Shared;

namespace Vina_RadioClient.Modules
{
    public class NuiModule : Module
    {
        public NuiModule(Client client) : base(client)
        {

        }

        #region BASE EVENTS



        #endregion
        #region MODULE EVENTS



        #endregion
        #region MODULE METHODS

        public void ShowRadioSwitcher()
        {
            Client.SendNuiActionData("ShowRadioSwitcher");
        }

        public void HideRadioSwitcher()
        {
            Client.SendNuiActionData("HideRadioSwitcher");
        }

        public void AddRadioChannel(RadioChannels channel)
        {
            Client.SendNuiActionData("AddRadioChannel", channel);
        }

        public void SelectRadioChannelIndex(int index)
        {
            Client.SendNuiActionData("SelectRadioChannelIndex", index);
        }

        #endregion
    }
}
