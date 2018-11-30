using FR.Ganfra.Materialspinner;

namespace ForgottenConqueror
{
    public static class Extensions
    {
        public static void SetSelected(this MaterialSpinner spinner, int position)
        {
            spinner.SetSelection(position + 1);
        }
    }
}