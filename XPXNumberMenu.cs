using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace XPXLevels;

public sealed class XPXNumberMenu : BaseMenu
{
    private readonly BasePlugin _plugin;

    public int ItemsPerPage { get; set; } = 4;
    public List<string> BodyLines { get; } = [];
    public int BodyLinesPerPage { get; set; } = 3;
    public bool PaginateBodyWithMenu { get; set; }
    public string? SlotSevenLabel { get; set; }
    public Action<CCSPlayerController>? SlotSevenAction { get; set; }
    public int MaxOptionTextLength { get; set; } = 24;
    public string TitleColor { get; set; } = "gold";
    public string BodyColor { get; set; } = "silver";
    public string EnabledColor { get; set; } = "white";
    public string DisabledColor { get; set; } = "gray";
    public string PrevPageColor { get; set; } = "deepskyblue";
    public string NextPageColor { get; set; } = "deepskyblue";
    public string CloseColor { get; set; } = "tomato";

    public XPXNumberMenu(string title, BasePlugin plugin) : base(title)
    {
        _plugin = plugin;
    }

    public override void Open(CCSPlayerController player)
    {
        if (MenuManager.GetActiveMenu(player) is XPXNumberMenuInstance XPXMenu)
        {
            XPXMenu.Close();
        }
        else
        {
            MenuManager.CloseActiveMenu(player);
        }

        MenuManager.GetActiveMenus()[player.Handle] = new XPXNumberMenuInstance(_plugin, player, this);
        MenuManager.GetActiveMenus()[player.Handle].Display();
    }
}

public sealed class XPXNumberMenuInstance : BaseMenuInstance
{
    private readonly BasePlugin _plugin;

    public override int NumPerPage => Menu is XPXNumberMenu xpxMenu ? xpxMenu.ItemsPerPage : 6;

    protected override int MenuItemsPerPage => NumPerPage;

    public bool IsBodyPagedMenu => Menu is XPXNumberMenu { PaginateBodyWithMenu: true };
    public bool CanGoPrevPage => HasPrevButton;
    public bool CanGoNextPage => HasNextButton;
    public int BodyPageIndex => MenuItemsPerPage <= 0 ? 0 : CurrentOffset / MenuItemsPerPage;

    public XPXNumberMenuInstance(BasePlugin plugin, CCSPlayerController player, IMenu menu) : base(player, menu)
    {
        _plugin = plugin;
        RemoveOnTickListener();
        plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnTick>(Display);
    }

    public override void Display()
    {
        if (MenuManager.GetActiveMenu(Player) != this)
        {
            RemoveOnTickListener();
            return;
        }

        if (Menu is XPXNumberMenu menu)
        {
            var builder = new StringBuilder();
            builder.Append($"<b><font color='{menu.TitleColor}'>{menu.Title}</font></b>");
            builder.AppendLine("<br>");

            if (menu.BodyLines.Count > 0)
            {
                var bodyLines = menu.PaginateBodyWithMenu
                    ? menu.BodyLines
                        .Skip((CurrentOffset / MenuItemsPerPage) * menu.BodyLinesPerPage)
                        .Take(menu.BodyLinesPerPage)
                    : menu.BodyLines;

                foreach (var bodyLine in bodyLines)
                {
                    builder.Append($"<font color='{menu.BodyColor}'>{bodyLine}</font>");
                    builder.AppendLine("<br>");
                }
            }

            var keyOffset = 1;
            for (var index = CurrentOffset; index < Math.Min(CurrentOffset + MenuItemsPerPage, menu.MenuOptions.Count); index++)
            {
                var option = menu.MenuOptions[index];
                if (string.IsNullOrWhiteSpace(option.Text))
                {
                    continue;
                }

                var color = option.Disabled ? menu.DisabledColor : menu.EnabledColor;
                builder.Append($"<font color='{color}'>{keyOffset++}.</font> {TrimOptionLabel(option.Text, menu.MaxOptionTextLength)}");
                builder.AppendLine("<br>");
            }

            if (HasPrevButton)
            {
                builder.Append($"<font color='{menu.PrevPageColor}'>7.</font> Prev");
                builder.AppendLine("<br>");
            }
            else if (menu.SlotSevenAction is not null && !string.IsNullOrWhiteSpace(menu.SlotSevenLabel))
            {
                builder.Append($"<font color='{menu.PrevPageColor}'>7.</font> {menu.SlotSevenLabel}");
                builder.AppendLine("<br>");
            }

            if (HasNextButton)
            {
                builder.Append($"<font color='{menu.NextPageColor}'>8.</font> Next");
                builder.AppendLine("<br>");
            }

            if (menu.ExitButton)
            {
                builder.Append($"<font color='{menu.CloseColor}'>9.</font> Close");
                builder.AppendLine("<br>");
            }

            Player.PrintToCenterHtml(builder.ToString());
        }
    }

    public override void Close()
    {
        base.Close();
        RemoveOnTickListener();
        Player.PrintToCenterHtml(" ");
    }

    public void GoToBodyPage(int pageIndex)
    {
        if (!IsBodyPagedMenu)
        {
            return;
        }

        CurrentOffset = Math.Max(0, pageIndex * MenuItemsPerPage);
        Display();
    }

    private void RemoveOnTickListener()
    {
        CounterStrikeSharp.API.Core.Listeners.OnTick handler = Display;
        _plugin.RemoveListener("OnTick", handler);
    }

    private static string TrimOptionLabel(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLength <= 0 || text.Length <= maxLength)
        {
            return text;
        }

        return text[..Math.Max(1, maxLength - 3)].TrimEnd() + "...";
    }
}
