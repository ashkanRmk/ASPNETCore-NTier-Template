using System.Threading.Tasks;
using Liaro.ModelLayer;

namespace Liaro.ServiceLayer.Contracts
{
    public interface IKavenegarService
    {
        Task<SmsResultVM> SendLoginCode(string loginCode, string mobile, string fullName);

    }
}