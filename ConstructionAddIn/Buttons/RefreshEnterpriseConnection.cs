using ArcGIS.Desktop.Framework.Contracts;
using ConstructionAddIn.Services.EnterpriseConnection;

namespace ConstructionAddIn.Buttons
{
    /// <summary>
    /// Reconnects to the enterprise database without any hard-coded server address.
    ///
    /// The server / database are read from the project's layers, or from the admin's
    /// shared settings file if that server cannot be reached. After reconnecting, every
    /// layer in the Contents pane is checked, and editing is enabled again only if the
    /// Construction layers point to the enterprise database.
    ///
    /// See <see cref="EnterpriseConnectionService.ReconnectAsync"/>.
    /// </summary>
    internal class RefreshEnterpriseConnection : Button
    {
        protected override async void OnClick()
        {
            await EnterpriseConnectionService.ReconnectAsync();
        }
    }
}
