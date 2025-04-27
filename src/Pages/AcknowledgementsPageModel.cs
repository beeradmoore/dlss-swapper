using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DLSS_Swapper.Pages;

public partial class AcknowledgementsPageModel : ObservableObject
{
    readonly WeakReference<AcknowledgementsPage> _weakPage;

    public AcknowledgementsPageModel(AcknowledgementsPage page)
    {
        _weakPage = new WeakReference<AcknowledgementsPage>(page);
    }
}
