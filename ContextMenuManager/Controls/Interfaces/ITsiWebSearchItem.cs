﻿using BluePointLilac.Methods;
using ContextMenuManager.Methods;
using System.Windows.Forms;

namespace ContextMenuManager.Controls.Interfaces
{
    interface ITsiWebSearchItem
    {
        string SearchText { get; }
        WebSearchMenuItem TsiSearch { get; set; }
    }

    sealed class WebSearchMenuItem : ToolStripMenuItem
    {
        public WebSearchMenuItem(ITsiWebSearchItem item) : base(AppString.Menu.WebSearch)
        {
            Click += (sender, e) =>
            {
                string url = AppConfig.EngineUrl.Replace("%s", item.SearchText);
                ExternalProgram.OpenWebUrl(url);
            };
        }
    }
}