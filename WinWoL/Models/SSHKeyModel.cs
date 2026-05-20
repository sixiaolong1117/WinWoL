namespace WinWoL.Models
{
    public class SSHKeyModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string Fingerprint { get; set; }
        public string CreatedAt { get; set; }
    }
}
