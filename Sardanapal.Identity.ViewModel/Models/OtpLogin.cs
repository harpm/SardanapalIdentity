using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sardanapal.Identity.ViewModel.Models;

public class OtpRequestVM
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public long? PhoneNumber { get; set; }
}

public class OtpLoginVM
{
    public string OtpCode { get; set; }
}
