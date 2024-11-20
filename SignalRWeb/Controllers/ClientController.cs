using Microsoft.AspNetCore.Mvc;
using SignalRWeb.SignalRHub;

namespace SignalRWeb.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult ClientStatus()
        {
            // Get the list of connected clients from the SignalR hub
            var connectedClients = ClientHub.GetConnectedClients();
            return View(connectedClients);
        }
    }
}
