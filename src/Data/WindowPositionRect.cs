using System;
using System.Text.Json.Serialization;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace DLSS_Swapper.Data;

public class WindowPositionRect
{
    [JsonPropertyName("x")]
    public int X { get; set; } = -1;

    [JsonPropertyName("y")]
    public int Y { get; set; } = -1;

    [JsonPropertyName("width")]
    public int Width { get; set; } = -1;

    [JsonPropertyName("height")]
    public int Height { get; set; } = -1;

    public OverlappedPresenterState State { get; set; } = OverlappedPresenterState.Restored;

    public WindowPositionRect()
    {

    }

    public WindowPositionRect(WindowPositionRect other)
    {
        ArgumentNullException.ThrowIfNull(other);

        X = other.X;
        Y = other.Y;
        Width = other.Width;
        Height = other.Height;
        State = other.State;

        // -32000 is some magic number were windows go to die.
        if (X == -32000)
        {
            X = 0;
        }

        if (Y == -32000)
        {
            Y = 0;
        }

    }

    public WindowPositionRect(int x, int y, int width, int height)
    {
        // -32000 is some magic number were windows go to die.
        // This is to help apps that are already broken to show the main window again.
        if (x == -32000)
        {
            x = 0;
        }

        if (y == -32000)
        {
            y = 0;
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public RectInt32 GetRectInt32()
    {
        // -32000 is some magic number were windows go to die.
        // This is to help apps that are already broken to show the main window again.
        if (X == -32000)
        {
            X = 0;
        }

        if (Y == -32000)
        {
            Y = 0;
        }

        return new RectInt32(X, Y, Width, Height);
    }

    public void UpdatePosition(PointInt32 position)
    {
        // -32000 is some magic number were windows go to die.
        if (position.X == -32000 || position.Y == -32000)
        {
            return;
        }

        X = position.X;
        Y = position.Y;
    }

    public void UpdateFromAppWindow(AppWindow appWindow)
    {
        // -32000 is some magic number were windows go to die.
        if (appWindow.Position.X == -32000 || appWindow.Position.Y == -32000)
        {
            return;
        }

        Width = appWindow.Size.Width;
        Height = appWindow.Size.Height;
        X = appWindow.Position.X;
        Y = appWindow.Position.Y;
    }

}
