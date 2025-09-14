using BluePointLilac.Methods;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MyStatusBar : Panel
    {
        public static readonly string DefaultText = $"Ver: {Application.ProductVersion}    {Application.CompanyName}";

        // �����ɫ��������
        private Color topColor = Color.Empty;
        private Color middleColor = Color.Empty;
        private Color bottomColor = Color.Empty;

        // ��ɫģʽ��־
        private bool isDarkMode = false;

        public MyStatusBar()
        {
            Text = DefaultText;
            Height = 30.DpiZoom();
            Dock = DockStyle.Bottom;
            Font = SystemFonts.StatusFont;

            // ���ϵͳ���Ⲣ������ɫ
            CheckSystemTheme();

            // ����ϵͳ��������¼�
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            this.Disposed += (s, e) => Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        // ϵͳ��������¼�����
        private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
            {
                CheckSystemTheme();
                Refresh();
            }
        }

        // ���ϵͳ����
        private void CheckSystemTheme()
        {
            // ����Ƿ���ɫģʽ (Windows 10/11)
            isDarkMode = IsDarkThemeEnabled();

            if (isDarkMode)
            {
                // ��ɫģʽ��ɫ���� - ʹ����ɫ����
                BackColor = Color.FromArgb(40, 40, 40); // �м�ɫ����Ϊ����ɫ
                ForeColor = Color.LightGray;

                // ��ɫģʽ������ɫ - ��ɫ����
                topColor = Color.FromArgb(128, 128, 128);
                middleColor = Color.FromArgb(56, 56, 56);
                bottomColor = Color.FromArgb(128, 128, 128);
            }
            else
            {
                // ǳɫģʽ��ɫ����
                BackColor = MyMainForm.ButtonMain;
                ForeColor = MyMainForm.FormFore;

                // ǳɫģʽ������ɫ - ��ɫ����
                topColor = Color.FromArgb(255, 255, 255);
                middleColor = Color.FromArgb(230, 230, 230);
                bottomColor = Color.FromArgb(255, 255, 255);
            }
        }

        // ���ϵͳ�Ƿ�ʹ����ɫ����
        private bool IsDarkThemeEnabled()
        {
            try
            {
                // ͨ��ע�����Windows��������
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null && value is int)
                        {
                            return (int)value == 0;
                        }
                    }
                }
            }
            catch
            {
                // ����޷���⣬ʹ��Ĭ��ֵ
            }

            // Ĭ��ʹ��ǳɫģʽ
            return false;
        }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get => base.Text; set => base.Text = value; }

        // �����ɫ��������
        [Browsable(true), Category("Appearance"), Description("���䶥����ɫ")]
        public Color TopColor
        {
            get => topColor;
            set { topColor = value; Refresh(); }
        }

        [Browsable(true), Category("Appearance"), Description("�����м���ɫ")]
        public Color MiddleColor
        {
            get => middleColor;
            set { middleColor = value; Refresh(); }
        }

        [Browsable(true), Category("Appearance"), Description("����ײ���ɫ")]
        public Color BottomColor
        {
            get => bottomColor;
            set { bottomColor = value; Refresh(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // ������ɫ���䱳��
            using (LinearGradientBrush brush = new LinearGradientBrush(
                ClientRectangle,
                Color.Empty,
                Color.Empty,
                LinearGradientMode.Vertical))
            {
                // ������ɫ����
                ColorBlend colorBlend = new ColorBlend(3);
                colorBlend.Colors = new Color[] { TopColor, MiddleColor, BottomColor };
                colorBlend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = colorBlend;

                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            // �����ı�������ԭ���߼���
            string txt = Text;
            int left = Height / 3;
            for (int i = Text.Length - 1; i >= 0; i--)
            {
                Size size = TextRenderer.MeasureText(txt, Font);
                if (size.Width < ClientSize.Width - 2 * left)
                {
                    using (Brush brush = new SolidBrush(ForeColor))
                    {
                        int top = (Height - size.Height) / 2;
                        e.Graphics.DrawString(txt, Font, brush, left, top);
                        break;
                    }
                }
                txt = Text.Substring(0, i) + "...";
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e); Refresh();
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e); Refresh();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e); Refresh();
        }
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e); Refresh();
        }
        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e); Refresh();
        }
    }
}