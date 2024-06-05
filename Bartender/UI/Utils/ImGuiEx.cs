using Dalamud.Interface.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bartender.UI.Utils;

public static class ImGuiEx
{
    public static void SetItemTooltip(string s, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (ImGui.IsItemHovered(flags))
            ImGui.SetTooltip(s);
    }

    private static bool SliderEnabled = false;
    private static bool SliderVertical = false;
    private static float SliderInterval = 0;
    private static int LastHitInterval = 0;
    private static Action<bool, bool, bool> SliderAction;
    public static void SetupSlider(bool vertical, float interval, Action<bool, bool, bool> action)
    {
        SliderEnabled = true;
        SliderVertical = vertical;
        SliderInterval = interval;
        LastHitInterval = 0;
        SliderAction = action;
    }

    public static void DoSlider()
    {
        if (!SliderEnabled) return;

        var popupOpen = !ImGui.IsPopupOpen("_SLIDER") && ImGui.IsPopupOpen(null, ImGuiPopupFlags.AnyPopup);
        if (!popupOpen)
        {
            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(new Vector2(-100));
            ImGui.OpenPopup("_SLIDER", ImGuiPopupFlags.NoOpenOverItems);
            if (!ImGui.BeginPopup("_SLIDER")) return;
        }

        var drag = SliderVertical ? ImGui.GetMouseDragDelta().Y : ImGui.GetMouseDragDelta().X;
        var dragInterval = (int)(drag / SliderInterval);
        var hit = false;
        var increment = false;
        if (dragInterval > LastHitInterval)
        {
            hit = true;
            increment = true;
        }
        else if (dragInterval < LastHitInterval)
        {
            hit = true;
        }

        var closing = !ImGui.IsMouseDown(ImGuiMouseButton.Left);

        if (LastHitInterval != dragInterval)
        {
            while (LastHitInterval != dragInterval)
            {
                LastHitInterval += increment ? 1 : -1;
                SliderAction(hit, increment, closing && LastHitInterval == dragInterval);
            }
        }
        else
            SliderAction(false, false, closing);

        if (closing)
            SliderEnabled = false;

        if (!popupOpen)
            ImGui.EndPopup();
    }
}
