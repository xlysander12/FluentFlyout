// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyout.Classes.Settings;
using System.Runtime.InteropServices;
using static FluentFlyout.Classes.NativeMethods;

namespace FluentFlyoutWPF.Classes;

internal static class FullscreenDetector
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Checks if a DirectX exclusive fullscreen application or game is currently running.
    /// </summary>
    /// <returns>
    /// true if a fullscreen DirectX application is running;
    /// false if no fullscreen application is detected or if the check fails
    /// </returns>
    private static bool IsDirectXApplicationRunning()
    {
        try
        {
            QUERY_USER_NOTIFICATION_STATE state;
            int result = SHQueryUserNotificationState(out state);

            Logger.Debug(state);
            
            if (result != 0) // 0 means SUCCESS
            {
                throw new Exception($"SHQueryUserNotificationState failed with error code: {result}");
            }

            return state == QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error detecting fullscreen state");
            return false;
        }
    }

    /// <summary>
    /// Checks if a borderless fullscreen application or game is currently running.
    /// </summary>
    /// <returns>
    /// true if a borderless fullscreen application is running;
    /// false if no borderless fullscreen application is detected or if the check fails
    /// </returns>
    private static bool IsBorderlessFullscreenApplicationRunning()
    {
        // Get the current foreground window
        IntPtr hwnd = GetForegroundWindow();
        
        // If there's none, disregard this check
        if (hwnd == IntPtr.Zero) return false;
        
        if (IsIconic(hwnd)) return false; // If the window is minimized, disregard this check

        // Get window corners
        if (!GetWindowRect(hwnd, out RECT windowRect))
        {
            Logger.Error($"Failed to get window rectangle for hwnd: {hwnd}");
            return false;
        }
        
        // Get the monitor the window is in
        IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

        // Get the information of this monitor
        MONITORINFOEX monitorInfo = new() { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            Logger.Error($"Failed to get monitor info for monitor: {monitor}");
            return false;
        }
        
        // Check if the foreground window's borders are in the same positions as the borders of the monitor (A.K.A, is in borderless fullscreen)
        return windowRect.Left == monitorInfo.rcMonitor.Left &&
               windowRect.Top == monitorInfo.rcMonitor.Top &&
               windowRect.Right == monitorInfo.rcMonitor.Right &&
               windowRect.Bottom == monitorInfo.rcMonitor.Bottom;
    }

    /// <summary>
    /// Checks if an exclusive or borderless fullscreen application or game is currently running.
    /// </summary>
    /// <returns>
    /// true if an exclusive or borderless fullscreen application is running;
    /// false if no exclusive or borderless fullscreen application is detected, DisableIfFullscreen setting is false, or if any of the previous checks fail
    /// </returns>
    public static bool IsFullscreenApplicationRunning()
    {
        bool directX = IsDirectXApplicationRunning();
        bool borderless = IsBorderlessFullscreenApplicationRunning();
        
        Logger.Debug($"DirectX Fullscreen: {directX}, Borderless Fullscreen: {borderless}, DisableIfFullscreen Setting: {SettingsManager.Current.DisableIfFullscreen}");
        
        return SettingsManager.Current.DisableIfFullscreen && (directX || borderless);
    }
}