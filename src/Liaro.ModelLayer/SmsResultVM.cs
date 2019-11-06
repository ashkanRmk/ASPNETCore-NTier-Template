namespace Liaro.ModelLayer
{
    public class Return
    {
        public int status { get; set; }
        public string message { get; set; }
    }

    public class Entries
    {
        public int messageid { get; set; }
        public string message { get; set; }
        public int status { get; set; }
        public string statustext { get; set; }
        public string sender { get; set; }
        public string receptor { get; set; }
        public int date { get; set; }
        public int cost { get; set; }
    }

    public class SmsResultVM
    {
        public Return @return { get; set; }
        public Entries entries { get; set; }
    }
}