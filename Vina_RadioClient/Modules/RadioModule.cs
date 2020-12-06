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
        #region VARIABLES

        private bool RadioWheelVisible { get; set; } = false;

        private int SelectedRadioIndex { get; set; } = 0;

        private List<RadioChannels> _radioChannelList = new List<RadioChannels>();

        #endregion
        #region BASE EVENTS

        protected override void OnModuleInitialized()
        {
            nuiModule = client.GetModule<NuiModule>();

            AddRadioChannel(RadioChannels.OFF);
            AddRadioChannel(RadioChannels.RADIO_01_CLASS_ROCK);
            AddRadioChannel(RadioChannels.RADIO_02_POP);
            AddRadioChannel(RadioChannels.RADIO_03_HIPHOP_NEW);
            AddRadioChannel(RadioChannels.RADIO_04_PUNK);
            AddRadioChannel(RadioChannels.RADIO_05_TALK_01);
            AddRadioChannel(RadioChannels.RADIO_06_COUNTRY);
            AddRadioChannel(RadioChannels.RADIO_07_DANCE_01);
            AddRadioChannel(RadioChannels.RADIO_08_MEXICAN);
            AddRadioChannel(RadioChannels.RADIO_09_HIPHOP_OLD);
            AddRadioChannel(RadioChannels.RADIO_11_TALK_02);
            AddRadioChannel(RadioChannels.RADIO_12_REGGAE);
            AddRadioChannel(RadioChannels.RADIO_13_JAZZ);
            AddRadioChannel(RadioChannels.RADIO_14_DANCE_02);
            AddRadioChannel(RadioChannels.RADIO_15_MOTOWN);
            AddRadioChannel(RadioChannels.RADIO_16_SILVERLAKE);
            AddRadioChannel(RadioChannels.RADIO_17_FUNK);
            AddRadioChannel(RadioChannels.RADIO_18_90S_ROCK);
            AddRadioChannel(RadioChannels.RADIO_20_THELAB);
            AddRadioChannel(RadioChannels.RADIO_21_DLC_XM17);
            AddRadioChannel(RadioChannels.RADIO_22_DLC_BATTLE_MIX1_RADIO);

            script.AddTick(DisableDefaultRadio);
            script.AddTick(RadioController);
        }

        #endregion
        #region MODULE EVENTS



        #endregion
        #region MODULE TICKS

        private async Task DisableDefaultRadio()
        {
            await Client.Delay(0);

            Game.DisableControlThisFrame(0, Control.VehicleRadioWheel);
            Game.DisableControlThisFrame(0, Control.VehicleNextRadio);
        }

        private async Task RadioController()
        {
            bool wasInVehicle = false;
            int lastTimeChanged = 0;

            while (true)
            {
                await Client.Delay(0);

                int now = Game.GameTime;
                bool canOpen = CanOpenRadioSwitcher();
                bool radioKeyDown = Game.IsControlPressed(0, Control.VehicleRadioWheel);

                // Auto select last channel when entering vehicle
                bool isInVehicle = (Game.PlayerPed.CurrentVehicle != null && Game.PlayerPed.SeatIndex == VehicleSeat.Driver);
                if (!wasInVehicle && isInVehicle)
                {
                    wasInVehicle = true;
                    SelectRadioChannelByIndex(SelectedRadioIndex);
                }
                else if (wasInVehicle && !isInVehicle)
                {
                    wasInVehicle = false;
                }

                // Radio wheel pressed
                if (!RadioWheelVisible && radioKeyDown && canOpen)
                {
                    RadioWheelVisible = true;
                    nuiModule.ShowRadioSwitcher();
                    Audio.PlaySoundFrontend("FocusIn", "HintCamSounds");
                    Screen.Effects.Start(ScreenEffect.SwitchHudIn);
                    API.DisplayRadar(false);
                }
                else if (RadioWheelVisible && (!radioKeyDown || !canOpen))
                {
                    RadioWheelVisible = false;
                    nuiModule.HideRadioSwitcher();
                    Audio.PlaySoundFrontend("FocusOut", "HintCamSounds");
                    Screen.Effects.Stop(ScreenEffect.SwitchHudIn);
                    Screen.Effects.Start(ScreenEffect.SwitchHudOut);
                    while (Screen.Effects.IsActive(ScreenEffect.SwitchHudOut))
                    {
                        await Client.Delay(0);
                    }
                    Screen.Effects.Stop(ScreenEffect.SwitchHudOut);
                    API.DisplayRadar(true);
                }

                // Radio is visible
                if (RadioWheelVisible)
                {
                    DisableControls();

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

                    if (Game.IsControlJustPressed(0, Control.PhoneScrollBackward)
                        || Game.IsDisabledControlPressed(0, Control.VehicleSelectNextWeapon))
                    {
                        if (now > lastTimeChanged + 225)
                        {
                            SelectPreviousRadioChannel();
                            lastTimeChanged = now;
                        }
                    }

                    if (Game.IsControlJustPressed(0, Control.PhoneScrollForward)
                        || Game.IsDisabledControlPressed(0, Control.VehicleCinCam))
                    {
                        if (now > lastTimeChanged + 225)
                        {
                            SelectNextRadioChannel();
                            lastTimeChanged = now;
                        }
                    }
                }
            }
        }

        #endregion
        #region MODULE METHODS

        private void AddRadioChannel(RadioChannels channel)
        {
            if (!_radioChannelList.Contains(channel))
            {
                _radioChannelList.Add(channel);
                nuiModule.AddRadioChannel(channel);
            }
        }

        private bool CanOpenRadioSwitcher()
        {
            return (!API.IsPauseMenuActive()
                && !API.IsPauseMenuRestarting()
                && API.IsScreenFadedIn()
                && Game.PlayerPed.IsAlive
                && Game.PlayerPed.CurrentVehicle != null
                && API.DoesPlayerVehHaveRadio()
                && Game.PlayerPed.SeatIndex == VehicleSeat.Driver
                && API.GetIsVehicleEngineRunning(Game.PlayerPed.CurrentVehicle.Handle));
        }

        private void SelectRadioChannelByIndex(int channelIndex)
        {
            SelectedRadioIndex = channelIndex;
            nuiModule.SelectRadioChannelIndex(SelectedRadioIndex);

            string radioName = Enum.GetName(typeof(RadioChannels), _radioChannelList[SelectedRadioIndex]);
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
                Audio.PlaySoundFrontend("Retune_High", "MP_RADIO_SFX");
            }
        }

        private void SelectNextRadioChannel()
        {
            if (SelectedRadioIndex < _radioChannelList.Count - 1)
            {
                SelectedRadioIndex++;
                SelectRadioChannelByIndex(SelectedRadioIndex);
                Audio.PlaySoundFrontend("Retune_High", "MP_RADIO_SFX");
            }
        }

        private void DisableControls()
        {
            Game.DisableControlThisFrame(0, Control.FrontendPause);
            Game.DisableControlThisFrame(0, Control.FrontendPauseAlternate);
            Game.DisableControlThisFrame(0, Control.Aim);
            Game.DisableControlThisFrame(0, Control.VehicleAim);
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
