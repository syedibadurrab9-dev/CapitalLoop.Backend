namespace Application.Common.DTOs;

public class BinanceSyncResult
{
    public decimal SpotBalancePkr { get; set; }
    public decimal EarnBalancePkr { get; set; }
    public decimal FuturesBalancePkr { get; set; }
    public decimal TotalValuePkr => SpotBalancePkr + EarnBalancePkr + FuturesBalancePkr;
}