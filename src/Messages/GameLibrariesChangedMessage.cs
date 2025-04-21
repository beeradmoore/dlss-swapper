using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DLSS_Swapper.Messages;

internal class GameLibrariesChangedMessage : ValueChangedMessage<uint>
{
    public GameLibrariesChangedMessage(uint gameLibraries) : base(gameLibraries)
    {
    }
}
