using System;
using System.Windows;
using TarekGamingSpace.Core;
using Forms = System.Windows.Forms;

namespace TarekGamingSpace.Services
{
    /// <summary>Tray icon with the quick menu, plus Windows notifications (balloons show as toasts on Windows 10/11).</summary>
    public sealed class TrayService : IDisposable
    {
        readonly Forms.NotifyIcon _icon;
        readonly Forms.ToolStripMenuItem _toggle, _panel, _exit;

        public event Action ToggleStripRequested;
        public event Action PanelRequested;
        public event Action ExitRequested;

        public TrayService()
        {
            var menu = new Forms.ContextMenuStrip();
            _toggle = new Forms.ToolStripMenuItem("", null, (s, e) => ToggleStripRequested?.Invoke());
            _panel = new Forms.ToolStripMenuItem("", null, (s, e) => PanelRequested?.Invoke());
            _exit = new Forms.ToolStripMenuItem("", null, (s, e) => ExitRequested?.Invoke());
            menu.Items.Add(_toggle);
            menu.Items.Add(_panel);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(_exit);

            _icon = new Forms.NotifyIcon
            {
                Icon = LoadIcon(),
                ContextMenuStrip = menu,
                Visible = true,
            };
            _icon.DoubleClick += (s, e) => PanelRequested?.Invoke();
        }

        public void UpdateTexts(bool stripVisible)
        {
            var L = Loc.I;
            _icon.Text = L["AppName"];
            _toggle.Text = stripVisible ? L["HideAll"] : L["ShowAll"];
            _panel.Text = L["OpenControlPanel"];
            _exit.Text = L["Exit"];
            _icon.ContextMenuStrip.RightToLeft = L.IsArabic ? Forms.RightToLeft.Yes : Forms.RightToLeft.No;
        }

        public void Notify(string title, string text, bool warning = true) =>
            _icon.ShowBalloonTip(8000, title, text, warning ? Forms.ToolTipIcon.Warning : Forms.ToolTipIcon.Info);

        static System.Drawing.Icon LoadIcon()
        {
            try
            {
                var info = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
                if (info != null)
                    using (var s = info.Stream)
                        return new System.Drawing.Icon(s, Forms.SystemInformation.SmallIconSize);
            }
            catch (Exception ex) { Log.Error("Could not load tray icon", ex); }
            return System.Drawing.SystemIcons.Application;
        }

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
