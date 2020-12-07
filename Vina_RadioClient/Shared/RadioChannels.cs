namespace Vina_RadioClient.Shared
{
    public enum RadioChannels
    {
        OFF = 0,
        RADIO_01_CLASS_ROCK,
        RADIO_02_POP,
        RADIO_03_HIPHOP_NEW,
        RADIO_04_PUNK,
        RADIO_05_TALK_01,
        RADIO_06_COUNTRY,
        RADIO_07_DANCE_01,
        RADIO_08_MEXICAN,
        RADIO_09_HIPHOP_OLD,
        RADIO_11_TALK_02,
        RADIO_12_REGGAE,
        RADIO_13_JAZZ,
        RADIO_14_DANCE_02,
        RADIO_15_MOTOWN,
        RADIO_16_SILVERLAKE,
        RADIO_17_FUNK,
        RADIO_18_90S_ROCK,
        RADIO_20_THELAB,
        RADIO_21_DLC_XM17,
        RADIO_22_DLC_BATTLE_MIX1_RADIO,
    }

    public class NuiRadioChannel
    {
        public RadioChannels Channel { get; set; }
        public string Label { get; set; }

        public NuiRadioChannel(RadioChannels channel, string label)
        {
            Channel = channel;
            Label = label;
        }
    }
}
