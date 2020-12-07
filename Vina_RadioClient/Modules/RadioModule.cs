using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

using VinaFrameworkClient.Core;
using Vina_RadioClient.Shared;

namespace Vina_RadioClient.Modules
{
    public class RadioModule : Module
    {
        public RadioModule(Client client) : base(client)
        {

        }

        #region MODULES

        NuiModule nuiModule;

        #endregion
        #region ACCESSORS

        public int SelectedRadioIndex { get; private set; } = 0;
        public bool RadioWheelVisible { get; private set; } = false;

        #endregion
        #region VARIABLES

        string radioName = "";
        int frontEndSound = -1;
        int lastTimeChanged = 0;
        int now = Game.GameTime;
        bool isInVehicle = false;
        bool wasInVehicle = false;
        bool wasRadarVisible = false;
        Control radioKey = Control.VehicleRadioWheel;
        List<RadioChannels> _radioChannelList = new List<RadioChannels>();

        #endregion
        #region BASE EVENTS

        protected override void OnModuleInitialized()
        {
            nuiModule = client.GetModule<NuiModule>();

            AddRadioChannel(RadioChannels.OFF, "Off");
            AddRadioChannel(RadioChannels.RADIO_01_CLASS_ROCK, "Los Santos Rock Radio");
            AddRadioChannel(RadioChannels.RADIO_02_POP, "Non-Stop-Pop FM");
            AddRadioChannel(RadioChannels.RADIO_03_HIPHOP_NEW, "Radio Los Santos");
            AddRadioChannel(RadioChannels.RADIO_04_PUNK, "Channel X");
            AddRadioChannel(RadioChannels.RADIO_05_TALK_01, "WCTR");
            AddRadioChannel(RadioChannels.RADIO_06_COUNTRY, "Rebel Radio");
            AddRadioChannel(RadioChannels.RADIO_07_DANCE_01, "Soulwax FM");
            AddRadioChannel(RadioChannels.RADIO_08_MEXICAN, "East Los FM");
            AddRadioChannel(RadioChannels.RADIO_09_HIPHOP_OLD, "West Coast Classics");
            AddRadioChannel(RadioChannels.RADIO_11_TALK_02, "Blaine County Radio");
            AddRadioChannel(RadioChannels.RADIO_12_REGGAE, "Blue Ark");
            AddRadioChannel(RadioChannels.RADIO_13_JAZZ, "WorldWide FM");
            AddRadioChannel(RadioChannels.RADIO_14_DANCE_02, "FlyLo FM");
            AddRadioChannel(RadioChannels.RADIO_15_MOTOWN, "The Lowdown 91.1");
            AddRadioChannel(RadioChannels.RADIO_16_SILVERLAKE, "Radio Mirror Park");
            AddRadioChannel(RadioChannels.RADIO_17_FUNK, "Space 103.2");
            AddRadioChannel(RadioChannels.RADIO_18_90S_ROCK, "Vinewood Boulevard Radio");
            AddRadioChannel(RadioChannels.RADIO_20_THELAB, "The Lab");
            AddRadioChannel(RadioChannels.RADIO_21_DLC_XM17, "blonded Los Santos 97.8 FM");
            AddRadioChannel(RadioChannels.RADIO_22_DLC_BATTLE_MIX1_RADIO, "Los Santos Underground Radio");

            script.AddTick(DisableDefaultRadio);
            script.AddTick(RadioProcess);
            script.AddTick(RadioControls);
        }

        #endregion
        #region MODULE EVENTS



        #endregion
        #region MODULE TICKS

        private async Task DisableDefaultRadio()
        {
            if (isInVehicle)
            {
                Game.DisableControlThisFrame(0, Control.VehicleRadioWheel);
                Game.DisableControlThisFrame(0, Control.VehicleNextRadio);
            }

            if (RadioWheelVisible)
            {
                DisableControls();
            }
        }

        private async Task RadioControls()
        {
            // Radio is visible
            if (RadioWheelVisible)
            {
                // Keyboard
                if (API.IsInputDisabled(0))
                {
                    Screen.DisplayHelpTextThisFrame($"Use ~INPUT_CELLPHONE_SCROLL_BACKWARD~ or ~INPUT_CELLPHONE_SCROLL_FORWARD~ to change channel.");
                }
                // Gamepad
                else
                {
                    Screen.DisplayHelpTextThisFrame($"Press ~INPUT_VEH_SELECT_NEXT_WEAPON~ or ~INPUT_VEH_CIN_CAM~ to change channel.");
                }

                if (now < lastTimeChanged + 225 || API.IsRadioRetuning() || API.IsRadioFadedOut()) return;

                // Check controls
                if (Game.IsControlJustPressed(0, Control.PhoneScrollBackward)
                    || Game.IsDisabledControlPressed(0, Control.VehicleSelectNextWeapon)
                    || Game.IsDisabledControlPressed(0, Control.Talk))
                {
                    SelectPreviousRadioChannel();
                    lastTimeChanged = now;
                }
                else if (Game.IsControlJustPressed(0, Control.PhoneScrollForward)
                    || Game.IsDisabledControlPressed(0, Control.VehicleCinCam))
                {
                    SelectNextRadioChannel();
                    lastTimeChanged = now;
                }
            }
        }

        private async Task RadioProcess()
        {
            await Client.Delay(4);

            now = Game.GameTime;

            // Auto select last channel when entering vehicle
            isInVehicle = (Game.PlayerPed.CurrentVehicle != null);
            if (!wasInVehicle && isInVehicle && Game.PlayerPed.IsSittingInVehicle() && Game.PlayerPed.SeatIndex == VehicleSeat.Driver && Game.PlayerPed.CurrentVehicle.IsEngineRunning)
            {
                wasInVehicle = true;
                SelectRadioChannelByIndex(SelectedRadioIndex);
            }
            else if (wasInVehicle && !isInVehicle)
            {
                wasInVehicle = false;
            }

            // Radio wheel pressed
            if (!RadioWheelVisible && Game.IsControlPressed(0, radioKey) && CanOpenRadioSwitcher())
            {
                RadioWheelVisible = true;

                // Show Nui
                nuiModule.ShowRadioSwitcher();

                PlayAudioIn();

                Screen.Effects.Start(ScreenEffect.SwitchHudIn);

                // Hide radar radar
                if (!API.IsRadarHidden() && API.IsRadarPreferenceSwitchedOn())
                {
                    wasRadarVisible = true;
                    API.DisplayRadar(false);
                }
            }
            else if (RadioWheelVisible && !Game.IsControlPressed(0, radioKey))
            {
                RadioWheelVisible = false;

                // Hide Nui
                nuiModule.HideRadioSwitcher();

                PlayAudioOut();

                // Show radar again if it was on before opening it
                if (wasRadarVisible)
                {
                    wasRadarVisible = false;
                    API.DisplayRadar(true);
                }

                Screen.Effects.Stop(ScreenEffect.SwitchHudIn);
                Screen.Effects.Start(ScreenEffect.SwitchHudOut);
                while (Screen.Effects.IsActive(ScreenEffect.SwitchHudOut))
                {
                    await Client.Delay(0);
                }
                Screen.Effects.Stop(ScreenEffect.SwitchHudOut);
            }
        }

        #endregion
        #region MODULE METHODS

        private void StopCurrentAudio()
        {
            if (frontEndSound != -1)
            {
                if (!Audio.HasSoundFinished(frontEndSound))
                    Audio.StopSound(frontEndSound);

                frontEndSound = -1;
            }
        }

        private void PlayAudioIn()
        {
            StopCurrentAudio();
            frontEndSound = Audio.PlaySoundFrontend("FocusIn", "HintCamSounds");
        }

        private void PlayAudioOut()
        {
            StopCurrentAudio();
            frontEndSound = Audio.PlaySoundFrontend("FocusOut", "HintCamSounds");
        }

        private void PlayRetuneAudio()
        {
            StopCurrentAudio();
            frontEndSound = Audio.PlaySoundFrontend("Retune_High", "MP_RADIO_SFX");
        }

        private void AddRadioChannel(RadioChannels channel, string label)
        {
            if (!_radioChannelList.Contains(channel))
            {
                _radioChannelList.Add(channel);
                nuiModule.AddRadioChannel(channel, label);
            }
        }

        private bool CanOpenRadioSwitcher()
        {
            return (_radioChannelList.Count > 0
                && !API.IsPauseMenuActive()
                && !API.IsPauseMenuRestarting()
                && API.IsScreenFadedIn()
                && Game.PlayerPed.IsAlive
                && Game.PlayerPed.CurrentVehicle != null
                && API.DoesPlayerVehHaveRadio()
                && API.IsPlayerVehRadioEnable()
                && Game.PlayerPed.SeatIndex == VehicleSeat.Driver
                && API.GetIsVehicleEngineRunning(Game.PlayerPed.CurrentVehicle.Handle));
        }

        private void SelectRadioChannelByIndex(int channelIndex)
        {
            SelectedRadioIndex = channelIndex;
            nuiModule.SelectRadioChannelIndex(SelectedRadioIndex);

            radioName = Enum.GetName(typeof(RadioChannels), _radioChannelList[SelectedRadioIndex]);
            Client.TriggerServerEvent("Radio.SelectRadioName", radioName);

            if (Game.PlayerPed.CurrentVehicle != null)
            {
                API.SetVehRadioStation(Game.PlayerPed.CurrentVehicle.Handle, radioName);
            }
        }

        private void SelectPreviousRadioChannel()
        {
            if (SelectedRadioIndex > 0)
            {
                SelectedRadioIndex--;
                SelectRadioChannelByIndex(SelectedRadioIndex);
                PlayRetuneAudio();
            }
        }

        private void SelectNextRadioChannel()
        {
            if (SelectedRadioIndex < _radioChannelList.Count - 1)
            {
                SelectedRadioIndex++;
                SelectRadioChannelByIndex(SelectedRadioIndex);
                PlayRetuneAudio();
            }
        }

        private void DisableControls()
        {
            Game.DisableControlThisFrame(0, Control.FrontendPause);
            Game.DisableControlThisFrame(0, Control.FrontendPauseAlternate);
            Game.DisableControlThisFrame(0, Control.Talk);
            Game.DisableControlThisFrame(0, Control.VehicleAim);
            Game.DisableControlThisFrame(0, Control.VehicleExit);
            Game.DisableControlThisFrame(0, Control.VehicleHorn);
            Game.DisableControlThisFrame(0, Control.VehicleAttack);
            Game.DisableControlThisFrame(0, Control.VehicleAttack2);
            Game.DisableControlThisFrame(0, Control.VehicleFlyAttack);
            Game.DisableControlThisFrame(0, Control.VehicleFlyAttack2);
            Game.DisableControlThisFrame(0, Control.VehicleFlyAttackCamera);
            Game.DisableControlThisFrame(0, Control.NextWeapon);
            Game.DisableControlThisFrame(0, Control.SelectNextWeapon);
            Game.DisableControlThisFrame(0, Control.VehicleCinCam);
            Game.DisableControlThisFrame(0, Control.VehicleFlySelectNextWeapon);
            Game.DisableControlThisFrame(0, Control.VehicleFlySelectPrevWeapon);
            Game.DisableControlThisFrame(0, Control.VehicleSelectNextWeapon);
            Game.DisableControlThisFrame(0, Control.VehicleSelectPrevWeapon);
        }

        #endregion
    }
}
