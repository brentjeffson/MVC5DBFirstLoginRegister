using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MVC5DBFirstLoginRegister.Models;

namespace MVC5DBFirstLoginRegister.ViewModels
{
    public class RegistrationViewModel : BaseLayoutViewModel
    {
        public User User { get; set; }
    }
}