using Microsoft.AspNetCore.Mvc;

namespace MagicVilla_Web.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult IndexBooking()
        {
            return View();
        }

        public IActionResult Sorry()
        {
            return View();
        }
    }

}
