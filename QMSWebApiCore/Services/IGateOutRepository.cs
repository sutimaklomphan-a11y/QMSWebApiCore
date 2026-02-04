using QMSWebApiCore.Models;

namespace QMSWebApiCore.Services
{
    public interface IGateOutRepository
    {
        //---- Gate Out ----//
        Task<List<M_GateOut>> GetAllGateOutStatusAsync(M_GateOut ClsGateOut);
        Task<List<M_GateOut>> GetAllGateOutAsync(string DCCode);
        Task<List<M_GateOut>> GatGateOutByBarcodeOnceAsync(string Barcode, string DCCode);
        Task<bool> StampGateOutAsync(M_GateOut ClsGateOut);
        Task<bool> StampGateOutDirectAsync(M_GateOut ClsGateOut);
        Task<bool> UpdateGateOutAsync(M_GateOut ClsGateOut);
        Task<bool> DeleteGateOutAsync(M_GateOut ClsGateOut);


        //---- EDP Process --//
        //Task<M_GateIn> SearchEDPInByBarcode(string DCcode, string barcode, string ActionDate);
    }
}
