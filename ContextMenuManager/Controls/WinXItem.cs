﻿using BluePointLilac.Controls;
using BluePointLilac.Methods;
using ContextMenuManager.Controls.Interfaces;
using ContextMenuManager.Methods;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ContextMenuManager.Controls
{
    sealed class WinXItem : FoldSubItem, IChkVisibleItem, IBtnShowMenuItem, IBtnMoveUpDownItem, ITsiAdministratorItem,
        ITsiTextItem, ITsiWebSearchItem, ITsiFilePathItem, ITsiDeleteItem, ITsiShortcutCommandItem
    {
        public WinXItem(string filePath, FoldGroupItem group)
        {
            InitializeComponents();
            FoldGroupItem = group;
            FilePath = filePath;
            RefreshKeyPath();
            Indent();
        }

        private string filePath;
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                ShellLink = new ShellLink(value);
                Text = ItemText;
                Image = ItemImage;
            }
        }

        private string keyPath = null;
        private void RefreshKeyPath()
        {
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                keyPath = FilePath.Substring((ItemVisible ? WinXList.WinXPath : WinXList.BackupWinXPath).Length);
            }
        }

        private string BackupFilePath => $@"{(ItemVisible ? WinXList.BackupWinXPath : WinXList.WinXPath)}{keyPath}";
        private string DefaultFilePath => $@"{WinXList.DefaultWinXPath}{keyPath}";

        private string GroupPath => ((WinXGroupItem)FoldGroupItem).GroupPath;

        public string ItemText
        {
            get
            {
                string name = ShellLink.Description?.Trim();
                if(name.IsNullOrWhiteSpace()) name = DesktopIni.GetLocalizedFileNames(FilePath, true);
                if(name == string.Empty) name = Path.GetFileNameWithoutExtension(FilePath);
                return name;
            }
            set
            {
                ShellLink.Description = value;
                ShellLink.Save();
                
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    DesktopIni.SetLocalizedFileNames(FilePath, value);

                    if (ItemVisible)
                    {
                        DesktopIni.SetLocalizedFileNames(DefaultFilePath, value);
                    }
                }
                else
                {
                    DesktopIni.SetLocalizedFileNames(FilePath, value);
                }
                Text = ResourceString.GetDirectString(value);
                ExplorerRestarter.Show();
            }
        }

        // Win11需要改变两处快捷方式，Win10仅需要隐藏一处快捷方式
        public bool ItemVisible
        {
            get
            {
                return (WinOsVersion.Current >= WinOsVersion.Win11) ? 
                    FilePath.Substring(0, WinXList.WinXPath.Length).Equals(WinXList.WinXPath, StringComparison.OrdinalIgnoreCase) : 
                    (File.GetAttributes(FilePath) & FileAttributes.Hidden) != FileAttributes.Hidden;
            }
            set
            {
                void CreateGroupPath(string dirPath)
                {
                    if (!Directory.Exists(dirPath))
                    {
                        // 创建目录文件夹
                        Directory.CreateDirectory(dirPath);

                        // 初始化desktop.ini文件
                        string iniPath = $@"{dirPath}\desktop.ini";
                        File.WriteAllText(iniPath, string.Empty, Encoding.Unicode);
                        File.SetAttributes(iniPath, File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
                    }
                }

                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    // 处理用户WinX菜单目录
                    string name = DesktopIni.GetLocalizedFileNames(FilePath);
                    string dirPath = Path.GetDirectoryName(BackupFilePath);
                    CreateGroupPath(dirPath);
                    File.Move(FilePath, BackupFilePath);
                    // 处理用户WinX菜单目录下的desktop.ini文件（确保移动后名称在本地化下相同）
                    DesktopIni.DeleteLocalizedFileNames(FilePath);
                    if (name != string.Empty) DesktopIni.SetLocalizedFileNames(BackupFilePath, name);
                    // 处理默认WinX菜单目录
                    if (value)  // 从禁用变为启用
                    {
                        string defaultDirPath = Path.GetDirectoryName(DefaultFilePath);
                        CreateGroupPath(defaultDirPath);
                        File.Copy(BackupFilePath, DefaultFilePath, true);
                        if (name != string.Empty) DesktopIni.SetLocalizedFileNames(DefaultFilePath, name);
                        if (Directory.GetFiles(GroupPath, "*.lnk").Length == 1)
                        {
                            ((WinXGroupItem)FoldGroupItem).ignoreChange = true;
                            ((WinXGroupItem)FoldGroupItem).ChkChecked = true;
                        }
                    }
                    else  // 从启用变为禁用
                    {
                        File.Delete(DefaultFilePath);
                        DesktopIni.DeleteLocalizedFileNames(DefaultFilePath);
                        if (Directory.GetFiles(GroupPath, "*.lnk").Length == 0)
                        {
                            ((WinXGroupItem)FoldGroupItem).ignoreChange = true;
                            ((WinXGroupItem)FoldGroupItem).ChkChecked = false;
                        }
                    }
                    // 文件与备份文件目录交换
                    FilePath = BackupFilePath;
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(FilePath);
                    if (value)
                    {
                        attributes &= ~FileAttributes.Hidden;
                    }
                    else
                    {
                        attributes |= FileAttributes.Hidden;
                    }
                    File.SetAttributes(FilePath, attributes);
                }
                ExplorerRestarter.Show();
            }
        }

        public Icon ItemIcon
        {
            get
            {
                ShellLink.ICONLOCATION iconLocation = ShellLink.IconLocation;
                string iconPath = iconLocation.IconPath;
                int iconIndex = iconLocation.IconIndex;
                if(string.IsNullOrEmpty(iconPath)) iconPath = FilePath;
                Icon icon = ResourceIcon.GetIcon(iconPath, iconIndex);
                if(icon == null)
                {
                    string path = ItemFilePath;
                    if(File.Exists(path)) icon = ResourceIcon.GetExtensionIcon(path);
                    else if(Directory.Exists(path)) icon = ResourceIcon.GetFolderIcon(path);
                }
                return icon;
            }
        }

        public string ItemFilePath
        {
            get
            {
                string path = ShellLink.TargetPath;
                if(!File.Exists(path) && !Directory.Exists(path)) path = FilePath;
                return path;
            }
        }

        public ShellLink ShellLink { get; private set; }
        public string SearchText => $"{AppString.SideBar.WinX} {Text}";
        public string FileName => Path.GetFileName(FilePath);
        private Image ItemImage => ItemIcon?.ToBitmap() ?? AppImage.NotFound;

        public bool ChkChecked
        {
            get => ItemVisible;
            set => ChkVisible.Checked = value;
        }

        public VisibleCheckBox ChkVisible { get; set; }
        public MenuButton BtnShowMenu { get; set; }
        public ChangeTextMenuItem TsiChangeText { get; set; }
        public WebSearchMenuItem TsiSearch { get; set; }
        public FilePropertiesMenuItem TsiFileProperties { get; set; }
        public FileLocationMenuItem TsiFileLocation { get; set; }
        public ShortcutCommandMenuItem TsiChangeCommand { get; set; }
        public RunAsAdministratorItem TsiAdministrator { get; set; }
        public DeleteMeMenuItem TsiDeleteMe { get; set; }
        public MoveButton BtnMoveUp { get; set; }
        public MoveButton BtnMoveDown { get; set; }

        readonly RToolStripMenuItem TsiDetails = new RToolStripMenuItem(AppString.Menu.Details);
        readonly RToolStripMenuItem TsiChangeGroup = new RToolStripMenuItem(AppString.Menu.ChangeGroup);

        private void InitializeComponents()
        {
            BtnShowMenu = new MenuButton(this);
            ChkVisible = new VisibleCheckBox(this);
            BtnMoveDown = new MoveButton(this, false);
            BtnMoveUp = new MoveButton(this, true);
            TsiChangeText = new ChangeTextMenuItem(this);
            TsiChangeCommand = new ShortcutCommandMenuItem(this);
            TsiAdministrator = new RunAsAdministratorItem(this);
            TsiSearch = new WebSearchMenuItem(this);
            TsiFileLocation = new FileLocationMenuItem(this);
            TsiFileProperties = new FilePropertiesMenuItem(this);
            TsiDeleteMe = new DeleteMeMenuItem(this);

            ContextMenuStrip.Items.AddRange(new ToolStripItem[] { TsiChangeText, new RToolStripSeparator(),
                TsiChangeGroup, new RToolStripSeparator(), TsiAdministrator, new RToolStripSeparator(),
                TsiDetails, new RToolStripSeparator(), TsiDeleteMe });

            TsiDetails.DropDownItems.AddRange(new ToolStripItem[] { TsiSearch,
                new RToolStripSeparator(), TsiChangeCommand, TsiFileProperties, TsiFileLocation });

            TsiChangeGroup.Click += (sender, e) => ChangeGroup();
            BtnMoveDown.MouseDown += (sender, e) => MoveItem(false);
            BtnMoveUp.MouseDown += (sender, e) => MoveItem(true);
            TsiChangeCommand.Click += (sender, e) =>
            {
                if(TsiChangeCommand.ChangeCommand(ShellLink))
                {
                    Image = ItemImage;
                    WinXHasher.HashLnk(FilePath);
                    ExplorerRestarter.Show();
                }
            };
        }

        private void ChangeGroup()
        {
            void ChangeFileGroup(string selectText, bool isWinX, out string lnkPath)
            {
                string meFilePath = isWinX ? FilePath : DefaultFilePath;
                string meDirPath = $@"{(isWinX ? WinXList.WinXPath : WinXList.DefaultWinXPath)}\{selectText}";

                int count = Directory.GetFiles(meDirPath, "*.lnk").Length;
                string num = (count + 1).ToString().PadLeft(2, '0');    // TODO:修复本组内顺序的问题
                string partName = FileName;
                int index = partName.IndexOf(" - ");
                if (index > 0) partName = partName.Substring(index + 3);
                lnkPath = $@"{meDirPath}\{num} - {partName}";
                lnkPath = ObjectPath.GetNewPathWithIndex(lnkPath, ObjectPath.PathType.File);
                string text = DesktopIni.GetLocalizedFileNames(meFilePath);
                DesktopIni.DeleteLocalizedFileNames(meFilePath);
                if (text != string.Empty) DesktopIni.SetLocalizedFileNames(lnkPath, text);
                File.Move(meFilePath, lnkPath);
            }

            using (SelectDialog dlg = new SelectDialog())
            {
                dlg.Title = AppString.Dialog.SelectGroup;
                dlg.Items = WinXList.GetGroupNames();
                dlg.Selected = FoldGroupItem.Text;
                if(dlg.ShowDialog() != DialogResult.OK) return;
                if(dlg.Selected == FoldGroupItem.Text) return;

                ChangeFileGroup(dlg.Selected, true, out string lnkPath);
                if (WinOsVersion.Current >= WinOsVersion.Win11)
                {
                    ChangeFileGroup(dlg.Selected, false, out _);
                }
                FilePath = lnkPath;
                RefreshKeyPath();

                WinXList list = (WinXList)Parent;
                list.Controls.Remove(this);
                for(int i = 0; i < list.Controls.Count; i++)
                {
                    if(list.Controls[i] is WinXGroupItem groupItem && groupItem.Text == dlg.Selected)
                    {
                        list.Controls.Add(this);
                        list.SetItemIndex(this, i + 1);
                        Visible = !groupItem.IsFold;
                        ((WinXGroupItem)FoldGroupItem).RemoveWinXItem(this);
                        FoldGroupItem = groupItem;
                        groupItem.AddWinXItem(this);
                        break;
                    }
                }
                ExplorerRestarter.Show();
            }
        }

        private void MoveItem(bool isUp)
        {
            WinXList list = (WinXList)Parent;
            int index = list.Controls.GetChildIndex(this);
            if(index == list.Controls.Count - 1) return;
            index += isUp ? -1 : 1;
            Control ctr = list.Controls[index];
            if(ctr is WinXGroupItem) return;
            WinXItem item = (WinXItem)ctr;

            MoveFileItem(item, true, out string path1, out string path2);
            if (WinOsVersion.Current >= WinOsVersion.Win11)
            {
                MoveFileItem(item, false, out _, out _);
            }

            FilePath = path1;
            RefreshKeyPath();
            item.FilePath = path2;
            item.RefreshKeyPath();
            list.SetItemIndex(this, index);
            ExplorerRestarter.Show();
        }
        private void MoveFileItem(WinXItem item, bool isWinX, out string path1, out string path2)
        {
            bool itemVisible1 = ItemVisible;
            bool itemVisible2 = item.ItemVisible;

            if (!isWinX && !itemVisible1)
            {
                path1 = null;
            }
            else
            {
                // 获取旧路径
                string meFilePath1 = isWinX ? FilePath : DefaultFilePath;
                // 删除旧的本地化文件名
                string name1 = DesktopIni.GetLocalizedFileNames(meFilePath1);
                DesktopIni.DeleteLocalizedFileNames(meFilePath1);
                // 获取新路径
                string meDirPath1 = Path.GetDirectoryName(meFilePath1);
                string fileName1 = $@"{item.FileName.Substring(0, 2)}{FileName.Substring(2)}";
                path1 = $@"{meDirPath1}\{fileName1}";
                path1 = ObjectPath.GetNewPathWithIndex(path1, ObjectPath.PathType.File);
                // 移动文件至新路径
                File.Move(meFilePath1, path1);
                // 创建新的本地化文件名
                if (name1 != string.Empty) DesktopIni.SetLocalizedFileNames(path1, name1);
            }

            if (!isWinX && !itemVisible2)
            {
                path2 = null;
            }
            else
            {
                string meFilePath2 = isWinX ? item.FilePath : item.DefaultFilePath;
                string name2 = DesktopIni.GetLocalizedFileNames(meFilePath2);
                DesktopIni.DeleteLocalizedFileNames(meFilePath2);
                string fileName2 = $@"{FileName.Substring(0, 2)}{item.FileName.Substring(2)}";
                string meDirPath2 = Path.GetDirectoryName(meFilePath2);
                path2 = $@"{meDirPath2}\{fileName2}";
                path2 = ObjectPath.GetNewPathWithIndex(path2, ObjectPath.PathType.File);
                File.Move(meFilePath2, path2);
                if (name2 != string.Empty) DesktopIni.SetLocalizedFileNames(path2, name2);
            }
        }

        public void DeleteMe()
        {
            File.Delete(FilePath);
            DesktopIni.DeleteLocalizedFileNames(FilePath);
            if (ItemVisible)
            {
                File.Delete(DefaultFilePath);
                DesktopIni.DeleteLocalizedFileNames(DefaultFilePath);
            }
            ((WinXGroupItem)FoldGroupItem).RemoveWinXItem(this);
            ExplorerRestarter.Show();
            ShellLink.Dispose();
        }
    }
}