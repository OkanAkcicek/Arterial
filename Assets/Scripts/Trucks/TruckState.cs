namespace FactoryCity.Trucks
{
    /// <summary>
    /// Kamyon durum makinesi adımları. Döngü:
    /// Idle → ToSource → Loading → ToDest → Unloading → Returning → Idle ...
    /// </summary>
    public enum TruckState
    {
        Idle,
        ToSource,
        Loading,
        ToDest,
        Unloading,
        Returning
    }
}
