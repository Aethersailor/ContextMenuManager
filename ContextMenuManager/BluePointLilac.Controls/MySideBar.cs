﻿using BluePointLilac.Methods;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluePointLilac.Controls
{
    public sealed class MySideBar : Panel
    {
        public MySideBar()
        {
            Dock = DockStyle.Left;
            ItemHeight = 30.DpiZoom();
            Font = SystemFonts.MenuFont;
            Font = new Font(Font.FontFamily, Font.Size + 1F);
            ForeColor = MyMainForm.foreMain;
            BackColor = MyMainForm.buttonSecond;
            BackgroundImageLayout = ImageLayout.None;
            Controls.AddRange(new Control[] { LblSeparator, PnlSelected, PnlHovered });
            PnlHovered.Paint += PaintItem;
            PnlSelected.Paint += PaintItem;
            SelectedIndex = -1;
        }

        private string[] itemNames;
        public string[] ItemNames
        {
            get => itemNames;
            set
            {
                itemNames = value;
                if(value != null && !IsFixedWidth)
                {
                    int maxWidth = 0;
                    Array.ForEach(value, str => maxWidth = Math.Max(maxWidth, GetItemWidth(str)));
                    Width = maxWidth + 2 * HorizontalSpace;
                }
                PnlHovered.Width = PnlSelected.Width = Width;
                PaintItems();
                SelectedIndex = -1;
            }
        }

        private int itemHeight;
        public int ItemHeight
        {
            get => itemHeight;
            set => PnlHovered.Height = PnlSelected.Height = itemHeight = value;
        }//项上下边缘距离
        public int TopSpace { get; set; } = 2.DpiZoom();//第一项顶部与上边缘的距离
        public int HorizontalSpace { get; set; } = 20.DpiZoom();//项文字与项左右边缘距离
        private float VerticalSpace => (itemHeight - TextHeight) * 0.5F;//项文字与项上下边缘距离
        private int TextHeight => TextRenderer.MeasureText(" ", Font).Height;//项文字高度
        public bool IsFixedWidth { get; set; } = true;//是否固定宽度

        public Color SeparatorColor
        {
            get => LblSeparator.BackColor;
            set => LblSeparator.BackColor = value;
        }//分隔线颜色
        public Color SelectedBackColor
        {
            get => PnlSelected.BackColor;
            set => PnlSelected.BackColor = value;
        }
        public Color HoveredBackColor
        {
            get => PnlHovered.BackColor;
            set => PnlHovered.BackColor = value;
        }
        public Color SelectedForeColor
        {
            get => PnlSelected.ForeColor;
            set => PnlSelected.ForeColor = value;
        }
        public Color HoveredForeColor
        {
            get => PnlHovered.ForeColor;
            set => PnlHovered.ForeColor = value;
        }

        readonly Panel PnlSelected = new Panel
        {
            BackColor = MyMainForm.MainColor,//侧边栏项目选中颜色
            ForeColor = Color.White,
            Enabled = false
        };
        readonly Panel PnlHovered = new Panel
        {
            BackColor = MyMainForm.buttonSecond,
            ForeColor = Color.White,
            Enabled = false
        };
        readonly Label LblSeparator = new Label
        {
            BackColor = Color.FromArgb(220, 220, 220),
            Dock = DockStyle.Right,
            Width = 1,
        };

        /// <summary>获取项目宽度</summary>
        public int GetItemWidth(string str)
        {
            return TextRenderer.MeasureText(str, Font).Width + 2 * HorizontalSpace;
        }

        /// <summary>绘制所有项目作为底图</summary>
        private void PaintItems()
        {
            BackgroundImage = new Bitmap(Width, ItemHeight * ItemNames.Length);
            using(Graphics g = Graphics.FromImage(BackgroundImage))
            {
                g.Clear(BackColor);
                if(itemNames == null) return;
                for(int i = 0; i < itemNames.Length; i++)
                {
                    if(itemNames[i] != null)
                    {
                        g.DrawString(itemNames[i], Font, new SolidBrush(ForeColor),
                            new PointF(HorizontalSpace, TopSpace + i * ItemHeight + VerticalSpace));
                    }
                    else
                    {
                        g.DrawLine(new Pen(SeparatorColor),
                            new PointF(HorizontalSpace, TopSpace + (i + 0.5F) * ItemHeight),
                            new PointF(Width - HorizontalSpace, TopSpace + (i + 0.5F) * ItemHeight)
                        );
                    }
                }
            }
        }

        /// <summary>刷新选中的项目</summary>
        private void RefreshItem(Panel panel, int index)
        {
            panel.CreateGraphics().Clear(panel.BackColor);
            panel.Top = index < 0 ? -ItemHeight : (TopSpace + index * ItemHeight);
            panel.Text = index < 0 ? null : ItemNames[index];
            panel.Refresh();
        }

        /// <summary>绘制选中的项目</summary>
        private void PaintItem(object sender, PaintEventArgs e)
        {
            Control ctr = (Control)sender;
            e.Graphics.DrawString(ctr.Text, Font,
                new SolidBrush(ctr.ForeColor),
                new PointF(HorizontalSpace, VerticalSpace));
        }

        /// <summary>显示选中的项目</summary>
        private void ShowItem(Panel panel, MouseEventArgs e)
        {
            if(itemNames == null) return;
            int index = (e.Y - TopSpace) / ItemHeight;
            if(index >= itemNames.Length || index < 0 || string.IsNullOrEmpty(itemNames[index]) || index == SelectedIndex)
            {
                Cursor = Cursors.Default;
                HoveredIndex = SelectedIndex;
            }
            else
            {
                Cursor = Cursors.Hand;
                if(panel == PnlSelected) SelectedIndex = index;
                else HoveredIndex = index;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ShowItem(PnlHovered, e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if(e.Button == MouseButtons.Left) ShowItem(PnlSelected, e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoveredIndex = SelectedIndex;
        }

        public event EventHandler SelectIndexChanged;

        public event EventHandler HoverIndexChanged;

        private int selectIndex;
        public int SelectedIndex
        {
            get => selectIndex;
            set
            {
                if(selectIndex == value) return;
                HoveredIndex = value;
                RefreshItem(PnlSelected, value);
                selectIndex = value;
                SelectIndexChanged?.Invoke(this, null);
            }
        }

        private int hoverIndex;
        public int HoveredIndex
        {
            get => hoverIndex;
            set
            {
                if(hoverIndex == value) return;
                RefreshItem(PnlHovered, value);
                hoverIndex = value;
                HoverIndexChanged?.Invoke(this, null);
            }
        }
    }
}