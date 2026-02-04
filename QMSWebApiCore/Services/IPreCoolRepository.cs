using QMSWebApiCore.Models;

namespace QMSWebApiCore.Services
{
    public interface IPreCoolRepository
    {
        Task<bool> StampPreCoolAsync(M_Precool CLS_PRECOOL);
    }
}
