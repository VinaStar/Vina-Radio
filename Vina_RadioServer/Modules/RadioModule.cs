using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vina_RadioClient.Shared;
using VinaFrameworkClient.Shared;
using VinaFrameworkServer.Core;

namespace Vina_RadioServer.Modules
{
    public class RadioModule : Module
    {
        public RadioModule(Server server) : base(server)
        {
            script.AddEvent("ToggleLoudRadio", new Action<int, bool>(OnToggleLoudRadio));
            script.AddEvent("PlayerRadioChannelChanged", new Action<Player, int>(OnPlayerRadioChannelChanged));
        }

        #region VARIABLES

        bool debug = false;
        Dictionary<Player, RadioChannels> playerRadioChannel = new Dictionary<Player, RadioChannels>();
        Dictionary<int, bool> boostedVehicleRadio = new Dictionary<int, bool>();

        #endregion
        #region BASE EVENTS

        protected override void OnModuleInitialized()
        {
            debug = API.GetConvarInt("vina_radio_debug", 0) == 1;

            if (debug)
            {
                script.Log($"Debug mode enabled!");
            }
        }

        protected override void OnPlayerClientInitialized(Player player)
        {
            // Set all players boosted radio status when the player is fully in game
            foreach (KeyValuePair<int, bool> pair in boostedVehicleRadio)
            {
                Server.TriggerClientEvent(player, "ToggleLoudRadio", pair.Key, pair.Value);
            }

            if (debug)
            {
                script.Log($"Sending all vehicles radio loud to {player.Name} [{boostedVehicleRadio.Count} vehicles].");
            }
        }

        protected override void OnPlayerDropped(Player player, string reason)
        {
            if (playerRadioChannel.ContainsKey(player))
            {
                playerRadioChannel.Remove(player);

                if (debug)
                {
                    script.Log($"Player {player.Name} left game, removed player radio [{playerRadioChannel.Count} players]!");
                }
            }
        }

        protected override void OnEntityRemoved(int entityHandle)
        {
            int networkId = API.NetworkGetNetworkIdFromEntity(entityHandle);
            if (boostedVehicleRadio.ContainsKey(networkId))
            {
                boostedVehicleRadio.Remove(networkId);
                Server.TriggerClientEvent("RemoveLoudRadio", networkId);

                if (debug)
                {
                    script.Log($"Removing vehicle {networkId} radio loud [{boostedVehicleRadio.Count} vehicles].");
                }
            }
        }

        #endregion
        #region MODULE EVENTS

        private void OnPlayerRadioChannelChanged([FromSource] Player player, int channel)
        {
            if (playerRadioChannel.ContainsKey(player))
            {
                playerRadioChannel[player] = (RadioChannels) channel;
            }
            else
            {
                playerRadioChannel.Add(player, (RadioChannels) channel);
            }

            if (debug)
            {
                script.Log($"Setting player {player.Name} radio channel to {channel} [{playerRadioChannel.Count} players].");
            }
        }

        private void OnToggleLoudRadio(int vehicleHandle, bool boosted)
        {
            if (boostedVehicleRadio.ContainsKey(vehicleHandle))
            {
                boostedVehicleRadio[vehicleHandle] = boosted;
            }
            else
            {
                boostedVehicleRadio.Add(vehicleHandle, boosted);
            }

            // Tell everyone about the change
            Server.TriggerClientEvent("ToggleLoudRadio", vehicleHandle, boosted);

            if (debug)
            {
                script.Log($"Setting vehicle {vehicleHandle} radio loud {boosted} [{boostedVehicleRadio.Count} vehicles].");
            }
        }

        #endregion
    }
}
