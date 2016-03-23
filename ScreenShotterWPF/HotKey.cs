using System.Configuration;
using System.Windows.Input;

namespace ScreenShotterWPF
{
    public class HotKey
    {
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public int vkKey { get; set; }
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public bool ctrl { get; set; }
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public bool alt { get; set; }
        [UserScopedSetting()]
        [SettingsSerializeAs(SettingsSerializeAs.Xml)]
        public bool shift { get; set; }

        public HotKey() { }
        public HotKey(bool ctrl, bool alt, bool shift, int vkKey)
        {
            this.vkKey = vkKey;
            this.ctrl = ctrl;
            this.alt = alt;
            this.shift = shift;
        }

        public override string ToString()
        {
            string hk = "";
            if (ctrl)
            {
                hk += "Ctrl + ";
            }
            if (alt)
            {
                hk += "Alt + ";
            }
            if (shift)
            {
                hk += "Shift + ";
            }
            hk += KeyInterop.KeyFromVirtualKey(vkKey).ToString();
            return hk;
        }
    }
}
