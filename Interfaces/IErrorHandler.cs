using System;
using System.Threading.Tasks;

namespace TaigaBotCS.Interfaces
{
    public interface IErrorHandler
    {
        public Task HandleErrorAsync(Enum error);
    }
}
