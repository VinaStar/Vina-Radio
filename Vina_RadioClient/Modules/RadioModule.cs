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
        public bool RadioBoosted 
        { 
            get
            {
                if (Game.PlayerPed.CurrentVehicle != null && boostedVehicleRadio.ContainsKey(Game.PlayerPed.CurrentVehicle.NetworkId))
                {
                    return boostedVehicleRadio[Game.PlayerPed.CurrentVehicle.NetworkId];
                }
                return false;
            } 
            private set
            {
                if (Game.PlayerPed.CurrentVehicle != null && Game.PlayerPed.CurrentVehicle.Exists())
                {
                    int vehicleNetId = Game.PlayerPed.CurrentVehicle.NetworkId;
                    if (boostedVehicleRadio.ContainsKey(vehicleNetId))
                    {
                        boostedVehicleRadio[vehicleNetId] = value;
                    }
                    else
                    {
                        boostedVehicleRadio.Add(vehicleNetId, value);
                    }

                    Client.TriggerServerEvent("ToggleLoudRadio", Game.PlayerPed.CurrentVehicle.NetworkId, value);
                }
            }
        }

        #endregion
        #region VARIABLES

        string radioName = "";
        int frontEndSound = -1;
        int lastTimeChanged = 0;
        int now = Game.GameTime;
        bool isInVehicle = false;
        bool wasInVehicle = false;
        bool loudRadioEnabled = false;
        Dictionary<int, bool> boostedVehicleRadio = new Dictionary<int, bool>();

        Control radioKey = Control.VehicleRadioWheel;
        List<RadioChannels> _radioChannelList = new List<RadioChannels>();

        #endregion
        #region BASE EVENTS

        protected override void OnModuleInitialized()
        {
            nuiModule = client.GetModule<NuiModule>();

            script.AddEvent("ToggleLoudRadio", new Action<int, bool>(OnToggleLoudRadio));
            script.AddEvent("RemoveLoudRadio", new Action<int>(OnRemoveLoudRadio));

            Audio.SetAudioFlag(AudioFlag.DisableFlightMusic, true);
            Audio.SetAudioFlag(AudioFlag.WantedMusicDisabled, true);

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

        private void OnToggleLoudRadio(int vehicleNetId, bool boosted)
        {
            int vehicleHandle = API.NetworkGetEntityFromNetworkId(vehicleNetId);

            if (API.DoesEntityExist(vehicleHandle))
            {
                if (boostedVehicleRadio.ContainsKey(vehicleNetId))
                {
                    boostedVehicleRadio[vehicleNetId] = boosted;
                }
                else
                {
                    boostedVehicleRadio.Add(vehicleNetId, boosted);
                }

                API.SetVehicleRadioLoud(vehicleHandle, boosted);

                script.Log($"Set vehicle loud radio {vehicleNetId} to {boosted} [Vehicles: {boostedVehicleRadio.Count}]");
            }
        }

        private void OnRemoveLoudRadio(int vehicleNetId)
        {
            if (boostedVehicleRadio.ContainsKey(vehicleNetId))
            {
                boostedVehicleRadio.Remove(vehicleNetId);

                script.Log($"Removed vehicle loud radio {vehicleNetId} [Vehicles: {boostedVehicleRadio.Count}]");
            }
        }

        #endregion
        #region MODULE TICKS

        private async Task DisableDefaultRadio()
        {
            await Client.Delay(0);

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
                else if (Game.IsControlJustPressed(0, Control.VehicleExit)
                    || Game.IsDisabledControlJustPressed(0, Control.VehicleExit))
                {
                    loudRadioEnabled = !loudRadioEnabled;
                    RadioBoosted = loudRadioEnabled;
                }
            }
        }

        private async Task RadioProcess()
        {
            await Client.Delay(100);

            now = Game.GameTime;

            // Auto select last channel when entering vehicle
            isInVehicle = (Game.PlayerPed.CurrentVehicle != null);
            if (!wasInVehicle 
                && isInVehicle 
                && Game.PlayerPed.IsSittingInVehicle(Game.PlayerPed.CurrentVehicle) 
                && Game.PlayerPed.SeatIndex == VehicleSeat.Driver 
                && Game.PlayerPed.CurrentVehicle.IsEngineRunning
                && API.IsPlayerVehicleRadioEnabled())
            {
                await Client.Delay(1000);
                wasInVehicle = true;
                RadioBoosted = loudRadioEnabled; // update radio boosted
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

                script.AddTick(DrawInstruction);

                // Show Nui
                nuiModule.ShowRadioSwitcher();

                PlayAudioIn();

                Screen.Effects.Start(ScreenEffect.SwitchHudIn);
            }
            else if (RadioWheelVisible && !Game.IsControlPressed(0, radioKey))
            {
                RadioWheelVisible = false;

                script.RemoveTick(DrawInstruction);

                // Hide Nui
                nuiModule.HideRadioSwitcher();

                PlayAudioOut();

                Screen.Effects.Stop(ScreenEffect.SwitchHudIn);
                Screen.Effects.Start(ScreenEffect.SwitchHudOut);
                while (Screen.Effects.IsActive(ScreenEffect.SwitchHudOut))
                {
                    await Client.Delay(0);
                }
                Screen.Effects.Stop(ScreenEffect.SwitchHudOut);
            }
        }

        private async Task DrawInstruction()
        {
            API.HideHudAndRadarThisFrame();

            int scaleform = API.RequestScaleformMovie("instructional_buttons");
            while (!API.HasScaleformMovieLoaded(scaleform))
            {
                await Client.Delay(0);
            }

            API.PushScaleformMovieFunction(scaleform, "CLEAR_ALL");
            API.PopScaleformMovieFunctionVoid();

            API.PushScaleformMovieFunction(scaleform, "SET_CLEAR_SPACE");
            API.PushScaleformMovieFunctionParameterInt(200);
            API.PopScaleformMovieFunctionVoid();

            API.PushScaleformMovieFunction(scaleform, "SET_DATA_SLOT");
            API.PushScaleformMovieFunctionParameterInt(0);
            API.ScaleformMovieMethodAddParamPlayerNameString(API.GetControlInstructionalButton(2, (int)Control.VehicleExit, 1));
            API.BeginTextCommandScaleformString("STRING");
            API.AddTextComponentScaleform((RadioBoosted) ? "Loud Radio Enabled" : "Loud Radio Disabled");
            API.EndTextCommandScaleformString();
            API.PopScaleformMovieFunctionVoid();

            API.PushScaleformMovieFunction(scaleform, "DRAW_INSTRUCTIONAL_BUTTONS");
            API.PopScaleformMovieFunctionVoid();

            API.PushScaleformMovieFunction(scaleform, "SET_BACKGROUND_COLOUR");
            API.PushScaleformMovieFunctionParameterInt(0);
            API.PushScaleformMovieFunctionParameterInt(0);
            API.PushScaleformMovieFunctionParameterInt(0);
            API.PushScaleformMovieFunctionParameterInt(80);
            API.PopScaleformMovieFunctionVoid();

            API.DrawScaleformMovieFullscreen(scaleform, 255, 255, 255, 255, 0);
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
                && (Game.PlayerPed.SeatIndex == VehicleSeat.Driver/* || Game.PlayerPed.SeatIndex == VehicleSeat.Passenger*/)
                && API.GetIsVehicleEngineRunning(Game.PlayerPed.CurrentVehicle.Handle));
        }

        private void SelectRadioChannelByIndex(int channelIndex)
        {
            SelectedRadioIndex = channelIndex;
            nuiModule.SelectRadioChannelIndex(SelectedRadioIndex);

            radioName = Enum.GetName(typeof(RadioChannels), _radioChannelList[SelectedRadioIndex]);
            Client.TriggerServerEvent("PlayerRadioChannelChanged", SelectedRadioIndex);

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
