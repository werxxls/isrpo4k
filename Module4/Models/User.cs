namespace BootProject.Models
{
    public class User
    {
        public string Login { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string Patronymic { get; set; }
        public string RoleName { get; set; }

        public string FullName => $"{Lastname} {Firstname} {Patronymic}".Trim();
    }
}