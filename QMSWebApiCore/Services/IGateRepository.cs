using QMSWebApiCore.Models;

namespace QMSWebApiCore.Services
{
    public interface IGateRepository
    {
        //Gate In Process Service
        Task<GateIn?> GetGateInByBarcodeAsync(string Barcode);
        Task<GateIn?> GetGateInByDetailAsync(int Gate_id,string DCCode);
        Task<IEnumerable<GateIn>> GetAllGateInAsync();
        Task<GateIn> CreateGateInAsync(GateIn ClsGateIn);
        Task<bool> UpdateGateInAsync(string Barcode, GateIn ClsGateIn);        
        Task<bool> DeleteGateInAsync(string Barcode);
    }
}