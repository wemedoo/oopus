namespace sReportsV2.BusinessLayer.Interfaces
{
    public interface IExistBLL<T>
    {
        bool ExistEntity(T dataIn);
    }
}
