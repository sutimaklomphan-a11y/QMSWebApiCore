using QMSWebApiCore.Models;

namespace QMSWebApiCore.Services
{
    public interface IGateInRepository
    {
        // --- Gate In Process Service ---
        Task<List<M_GateIn>> GetAllGateInStatusAsync(M_GateIn ClsGateIn);
        Task<List<M_GateIn>> GetAllGateInAsync(string DCCode);
        Task<List<M_GateIn>> GatGateInByBarcodeOnceAsync(string Barcode, string DCCode);
        Task<string> CheckDuplicateBarcode(string barcode);
        Task<M_GateIn?> GetGateInByDetailAsync(int Gate_id,string DCCode);
        Task<M_GateIn> CreateGateInAsync(M_GateIn ClsGateIn);
        Task<bool> UpdateGateInAsync(M_GateIn ClsGateIn);        
        Task<bool> DeleteGateInAsync(M_GateIn ClsGateIn);
        Task<M_GateIn> SearchEDPInByBarcode(string DCcode, string barcode, string ActionDate);

    }
}