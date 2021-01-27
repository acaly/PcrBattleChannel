using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<PcrIdentityUser> _userManager;
        private readonly SignInManager<PcrIdentityUser> _signInManager;

        public IndexModel(
            UserManager<PcrIdentityUser> userManager,
            SignInManager<PcrIdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "游戏昵称")]
            public string GameID { get; set; }

            [Display(Name = "QQ号")]
            public ulong? QQID { get; set; }

            [Display(Name = "禁用Yobot同步")]
            public bool DisableYobotSync { get; set; }
        }

        private async Task LoadAsync(PcrIdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            Username = userName;

            Input = new InputModel
            {
                GameID = user.GameID,
                QQID = user.QQID == 0 ? null : user.QQID,
                DisableYobotSync = user.DisableYobotSync,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (Input.GameID != user.GameID ||
                Input.QQID != user.QQID ||
                Input.DisableYobotSync != user.DisableYobotSync)
            {
                user.GameID = Input.GameID;
                user.QQID = Input.QQID ?? 0L;
                user.DisableYobotSync = Input.DisableYobotSync;

                var setResult = await _userManager.UpdateAsync(user);
                if (!setResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to update profile.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
