using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqRes.Client.DTOs
{
    public class ReqResOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 10;
        public string ApiKey { get; set; } = string.Empty;
    }

}
