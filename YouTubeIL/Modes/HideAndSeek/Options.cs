using YouTubeIL.Options;

namespace YouTubeIL.Modes.HideAndSeek
{
    internal static class Options
    {
        private const string OptionPrefixId = "HideAndSeek_";
        private static readonly string[] RolesOptions = {"No one", "Crewmates", "Impostor", "All"};

        public static CustomToggleOption HideAndSeekEnabled;
        public static CustomStringOption AllowAdmin;
        public static CustomStringOption AllowCameras;
        public static CustomNumberOption TimeToHide;
        public static CustomToggleOption AnnounceTimeLeftToHide;
        public static CustomToggleOption BlackScreenForSeeker;

        public static void CreateCustomOptions()
        {
            HideAndSeekEnabled = CustomOption.AddToggle(OptionPrefixId + "enabled", "# Hide And Seek", false);
            
            AllowAdmin = CustomOption.AddString(OptionPrefixId + "allowAdmin", "Allow admin", RolesOptions,
                parentOption: HideAndSeekEnabled);
            
            AllowCameras = CustomOption.AddString(OptionPrefixId + "allowCameras", "Allow cameras", RolesOptions,
                parentOption: HideAndSeekEnabled);

            TimeToHide = CustomOption.AddNumber(OptionPrefixId + "timeToHide", "Time to hide", 15, 5, 60, 5, "s",
                HideAndSeekEnabled);

            AnnounceTimeLeftToHide = CustomOption.AddToggle(OptionPrefixId + "announceTimeLeftToHide",
                "Announce time left to hide", true, HideAndSeekEnabled);

            BlackScreenForSeeker = CustomOption.AddToggle(OptionPrefixId + "blackScreenForSeeker",
                "Black screen for seeker", true, HideAndSeekEnabled);
        }
    }
}