namespace YouTubeIL.Options
{
    internal interface IToggleOption
    {
        public void Toggle();
    }
    
    internal sealed class CustomToggleOption : CustomOption, IToggleOption
    {
        public CustomToggleOption(string id, string name, bool value, CustomOption parent = null) : base(id, name,
            CustomOptionType.Toggle, value, parent)
        {
            ValueStringFormat = (_, optionValue) => (bool) optionValue ? "On" : "Off";
        }

        protected override bool GameOptionCreated(OptionBehaviour o)
        {
            base.GameOptionCreated(o);
            switch (o)
            {
                case ToggleOption toggle:
                    toggle.TitleText.Text = GetFormattedName();

                    toggle.CheckMark.enabled = toggle.oldValue = GetValue();

                    return true;
                // Display options in menu for non-host
                case StringOption str:
                    str.TitleText.Text = GetFormattedName();

                    str.Value = str.oldValue = 0;

                    str.ValueText.Text = GetFormattedValue();

                    return true;
                default:
                    return false;
            }
        }

        private bool GetValue() => GetValue<bool>();

        public void Toggle() => SetValue(!GetValue());
    }

    internal abstract partial class CustomOption
    {
        public static CustomToggleOption AddToggle(string id, string name, bool value, CustomOption parent = null) =>
            new(id, name, value, parent);
    }
}