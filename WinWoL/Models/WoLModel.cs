namespace WinWoL.Models
{
    public class WoLModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MacAddress { get; set; }
        public string IPAddress { get; set; }
        public string WoLAddress { get; set; }
        public string WoLPort { get; set; }
        public string RDPPort { get; set; }
        public string SSHCommand { get; set; }
        public string SSHPort { get; set; }
        public string SSHUser { get; set; }
        public string SSHKeyId { get; set; }
        public string SSHKeyPath { get; set; }
        public string WoLIsOpen { get; set; }
        public string RDPIsOpen { get; set; }
        public string SSHIsOpen { get; set; }
        public string BroadcastIsOpen { get; set; }
        public string SSHKeyIsOpen { get; set; }
    }
}
