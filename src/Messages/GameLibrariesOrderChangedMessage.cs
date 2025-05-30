using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DLSS_Swapper.Messages;


internal class GameLibrariesOrderChangedMessage : ValueChangedMessage<bool>
{
    public GameLibrariesOrderChangedMessage() : base(true)
    {
    }
}
