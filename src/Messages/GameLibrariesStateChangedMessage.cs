using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DLSS_Swapper.Messages;

internal class GameLibrariesStateChangedMessage : ValueChangedMessage<bool>
{
    public GameLibrariesStateChangedMessage() : base(true)
    {
    }
}
