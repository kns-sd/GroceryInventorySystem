using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GroceryInventorySystem.Models;

namespace GroceryInventorySystem.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public SettingsController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ManageUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all fields correctly!";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Sign out and sign in again with new password
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password updated successfully!";
            }
            else
            {
                TempData["Error"] = "Current password is incorrect!";
            }

            return RedirectToAction("Index");
        }
    }
}