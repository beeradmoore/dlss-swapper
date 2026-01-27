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
    }

    public WindowPositionRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public RectInt32 GetRectInt32()
    {
        return new RectInt32(X, Y, Width, Height);
    }

    public void UpdatePosition(PointInt32 position)
    {
        X = position.X;
        Y = position.Y;
    }

    public void UpdateFromAppWindow(AppWindow appWindow)
    {
        Width = appWindow.Size.Width;
        Height = appWindow.Size.Height;
        X = appWindow.Position.X;
        Y = appWindow.Position.Y;
    }

}
