using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DLSS_Swapper.Data.Messages;
public class SelectedLogicDriveStatesChangedMessage :ValueChangedMessage<IEnumerable<LogicalDriveState>>
{
    public SelectedLogicDriveStatesChangedMessage(IEnumerable<LogicalDriveState> value) : base(value)
    {
    }
}
