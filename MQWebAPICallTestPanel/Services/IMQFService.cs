using CommonLibrary;

namespace MQWebAPICallTestPanel.Services
{
    public interface IMQFService
    {
        Task<string> Remote_testapi333(int input);
        Task<string> Remote_testpostapi444(CommonStructure content);
    }
}
