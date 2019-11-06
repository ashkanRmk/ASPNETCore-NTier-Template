namespace Liaro.ModelLayer
{
    public class LoginVM
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginByMobileVM
    {
        public string Code { get; set; }
        public string Mobile { get; set; }
    }


    public class LoginByMobileResultVM
    {
        public string Error { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}