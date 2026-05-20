using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinWoL.Models
{
    public class SSHModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string SSHCommand { get; set; }
        public string SSHPort { get; set; }
        public string SSHUser { get; set; }
        public string SSHKeyId { get; set; }
        public string SSHKeyPath { get; set; }
        public string SSHKeyIsOpen { get; set; }
    }
}
