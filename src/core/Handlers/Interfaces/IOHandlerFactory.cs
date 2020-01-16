namespace RoyLab.ThunderSocket.Core.Handlers.Interfaces
{
    public interface IOHandlerFactory<out T> where T : IOHandler
    {
        T CreateNewHandler();
    }
}