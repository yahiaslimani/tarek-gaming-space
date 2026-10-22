using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace TarekGamingSpace.Core
{
    /// <summary>
    /// English / Arabic strings. XAML binds with {Binding [Key], Source={x:Static core:Loc.I}}
    /// and every binding refreshes when the language changes.
    /// </summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc I { get; } = new Loc();

        string _lang = "en";

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action LanguageChanged;

        public string Lang
        {
            get => _lang;
            set
            {
                value = value == "ar" ? "ar" : "en";
                if (_lang == value) return;
                _lang = value;
                foreach (var name in new[] { "Item[]", nameof(Lang), nameof(IsArabic), nameof(Flow) })
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                LanguageChanged?.Invoke();
            }
        }

        public bool IsArabic => _lang == "ar";
        public FlowDirection Flow => IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public string this[string key]
        {
            get
            {
                if ((IsArabic ? Ar : En).TryGetValue(key, out var v)) return v;
                return En.TryGetValue(key, out v) ? v : key;
            }
        }

        public string Format(string key, params object[] args) => string.Format(this[key], args);

        /// <summary>"1h 12m" / "1 س 12 د"</summary>
        public string HoursMinutes(double seconds)
        {
            long m = (long)Math.Max(0, seconds) / 60;
            long h = m / 60;
            m %= 60;
            string hs = h > 0 ? $"{h}{this["HourShort"]} " : "";
            return $"{hs}{m}{this["MinShort"]}";
        }

        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["AppName"] = "Tarek Gaming Space",
            ["BrandSub"] = "GAMING SPACE",
            ["Poste"] = "Poste",
            ["OpenChip"] = "∞ Open",
            ["Open"] = "Open",
            ["Timer"] = "Timer",
            ["Free"] = "Free",
            ["Paused"] = "Paused",
            ["TimeUp"] = "Time's up",
            ["ClickToSilence"] = "click to silence",
            // {0} is a duration like "1h 12m". Arabic puts the word first so the text stays in order.
            ["LeftFmt"] = "{0} left",
            ["PlayedFmt"] = "{0} played",
            ["HourShort"] = "h",
            ["MinShort"] = "m",
            ["Min"] = "min",
            ["DZD"] = "DZD",

            ["Start"] = "Start",
            ["Pause"] = "Pause",
            ["Resume"] = "Resume",
            ["Stop"] = "Stop",
            ["Add15"] = "+15 min",
            ["MakeOpen"] = "Make open",
            ["SetTimer"] = "Set timer",

            ["DlgStart"] = "Start session",
            ["DlgSwitch"] = "Set a time limit",
            ["OpenDesc"] = "Plays until he stops. Counts up.",
            ["TimerDesc"] = "Fixed time. Counts down.",
            ["AddOnTop"] = "Add on top",
            ["AddOnTopDesc"] = "Minutes start after the time already played.",
            ["TotalFromStart"] = "Total from the start",
            ["TotalFromStartDesc"] = "Minutes include the time already played.",
            ["AlreadyPlayed"] = "Already played",
            ["Custom"] = "Custom",
            ["EndsIn"] = "Ends in",
            ["Total"] = "Total",
            ["TooShort"] = "He already played longer than that.",
            ["Cancel"] = "Cancel",

            ["Receipt"] = "Session ended",
            ["RMode"] = "Mode",
            ["RPlayed"] = "Time played",
            ["RRate"] = "Price per hour",
            ["RTotal"] = "To collect",
            ["Done"] = "Done · free the poste",

            ["ControlPanel"] = "Control panel",
            ["OpenControlPanel"] = "Open control panel",
            ["Postes"] = "Postes",
            ["AddPoste"] = "+ Add poste",
            ["MaxNote"] = "Up to 5 postes. A poste can be deleted only when it is free.",
            ["Delete"] = "Delete",
            ["Settings"] = "Settings",
            ["Rate"] = "Price per hour",
            ["Language"] = "Language",
            ["Startup"] = "Start with Windows",
            ["Warning"] = "Warning before end",
            ["WarningHint"] = "The poste turns orange. 0 = off",
            ["SoundOn"] = "Sound when time is up",
            ["SoundOnHint"] = "When off, the poste still blinks red.",
            ["Sound"] = "Alert sound",
            ["Chime"] = "Chime",
            ["Alarm"] = "Alarm",
            ["Arcade"] = "Arcade",
            ["Test"] = "Test",
            ["Opacity"] = "Strip opacity",
            ["OpacityHint"] = "You can also scroll the mouse wheel over the button.",
            ["MoreOptions"] = "More options in the control panel",
            ["FadeWhat"] = "What fades",
            ["FadeBackground"] = "Background",
            ["FadeAll"] = "Everything",
            ["SolidOnHover"] = "Solid under the mouse",
            ["SolidOnHoverHint"] = "Full opacity while the pointer is on the strip.",
            ["SolidOnAlert"] = "Alerts stay solid",
            ["SolidOnAlertHint"] = "Last minutes and time's up are never faded.",
            ["OnTop"] = "Always on top",
            ["Lock"] = "Lock strip position",
            ["HideAll"] = "Hide all postes",
            ["ShowAll"] = "Show all postes",
            ["Exit"] = "Exit",
            ["Minimize"] = "Minimize",
            ["Close"] = "Close",
            ["ToastTimeUp"] = "{0}: time's up",
            ["StillRunning"] = "Still running in the tray. Sessions keep counting.",
        };

        static readonly Dictionary<string, string> Ar = new Dictionary<string, string>
        {
            ["AppName"] = "فضاء طارق للألعاب",
            ["BrandSub"] = "فضاء الألعاب",
            ["Poste"] = "جهاز",
            ["OpenChip"] = "∞ مفتوح",
            ["Open"] = "مفتوح",
            ["Timer"] = "مؤقت",
            ["Free"] = "متاح",
            ["Paused"] = "متوقف",
            ["TimeUp"] = "انتهى الوقت",
            ["ClickToSilence"] = "انقر لإسكات التنبيه",
            ["LeftFmt"] = "متبقي {0}",
            ["PlayedFmt"] = "منذ {0}",
            ["HourShort"] = " س",
            ["MinShort"] = " د",
            ["Min"] = "د",
            ["DZD"] = "دج",

            ["Start"] = "ابدأ",
            ["Pause"] = "إيقاف مؤقت",
            ["Resume"] = "استئناف",
            ["Stop"] = "إنهاء",
            ["Add15"] = "+15 د",
            ["MakeOpen"] = "اجعله مفتوحًا",
            ["SetTimer"] = "تحديد مدة",

            ["DlgStart"] = "بدء جلسة",
            ["DlgSwitch"] = "تحديد مدة",
            ["OpenDesc"] = "يلعب حتى يتوقف. عدّ تصاعدي.",
            ["TimerDesc"] = "مدة محددة. عدّ تنازلي.",
            ["AddOnTop"] = "إضافة فوق الملعوب",
            ["AddOnTopDesc"] = "تبدأ الدقائق بعد الوقت الملعوب.",
            ["TotalFromStart"] = "المجموع من البداية",
            ["TotalFromStartDesc"] = "الدقائق تشمل الوقت الملعوب.",
            ["AlreadyPlayed"] = "المدة الملعوبة",
            ["Custom"] = "مدة أخرى",
            ["EndsIn"] = "ينتهي بعد",
            ["Total"] = "المجموع",
            ["TooShort"] = "لعب أكثر من هذه المدة.",
            ["Cancel"] = "إلغاء",

            ["Receipt"] = "انتهت الجلسة",
            ["RMode"] = "النوع",
            ["RPlayed"] = "مدة اللعب",
            ["RRate"] = "سعر الساعة",
            ["RTotal"] = "المبلغ المستحق",
            ["Done"] = "تم · تحرير الجهاز",

            ["ControlPanel"] = "لوحة التحكم",
            ["OpenControlPanel"] = "فتح لوحة التحكم",
            ["Postes"] = "الأجهزة",
            ["AddPoste"] = "+ إضافة جهاز",
            ["MaxNote"] = "5 أجهزة كحد أقصى. لا يمكن حذف جهاز إلا إذا كان متاحًا.",
            ["Delete"] = "حذف",
            ["Settings"] = "الإعدادات",
            ["Rate"] = "سعر الساعة",
            ["Language"] = "اللغة",
            ["Startup"] = "التشغيل مع Windows",
            ["Warning"] = "تنبيه قبل النهاية",
            ["WarningHint"] = "يتحول الجهاز إلى البرتقالي. 0 = بدون",
            ["SoundOn"] = "صوت عند انتهاء الوقت",
            ["SoundOnHint"] = "عند الإيقاف، يبقى الجهاز يومض بالأحمر.",
            ["Sound"] = "صوت التنبيه",
            ["Chime"] = "رنين",
            ["Alarm"] = "إنذار",
            ["Arcade"] = "أركيد",
            ["Test"] = "تجربة",
            ["Opacity"] = "شفافية الشريط",
            ["OpacityHint"] = "يمكنك أيضًا تدوير عجلة الفأرة فوق الزر.",
            ["MoreOptions"] = "خيارات أخرى في لوحة التحكم",
            ["FadeWhat"] = "ما يصبح شفافًا",
            ["FadeBackground"] = "الخلفية",
            ["FadeAll"] = "الكل",
            ["SolidOnHover"] = "واضح عند مرور الفأرة",
            ["SolidOnHoverHint"] = "يظهر الشريط بوضوح كامل عندما يكون المؤشر فوقه.",
            ["SolidOnAlert"] = "التنبيهات تبقى واضحة",
            ["SolidOnAlertHint"] = "الدقائق الأخيرة وانتهاء الوقت لا تصبح شفافة.",
            ["OnTop"] = "دائمًا في المقدمة",
            ["Lock"] = "تثبيت موضع الشريط",
            ["HideAll"] = "إخفاء كل الأجهزة",
            ["ShowAll"] = "إظهار كل الأجهزة",
            ["Exit"] = "خروج",
            ["Minimize"] = "تصغير",
            ["Close"] = "إغلاق",
            ["ToastTimeUp"] = "{0}: انتهى الوقت",
            ["StillRunning"] = "التطبيق ما زال يعمل في شريط المهام. الجلسات مستمرة.",
        };
    }
}
