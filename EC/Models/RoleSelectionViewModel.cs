using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace EC.Models
{
    public class RoleSelectionViewModel
    {
        public SelectList Roles { get; set; }
        public int SelectedRole { get; set; }

    }
}
