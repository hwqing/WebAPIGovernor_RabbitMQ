
using MQWebAPICallTestPanel.Controllers;

namespace MQWebAPICallTestPanel
{
    public class TestWorker : BackgroundService
    {
        private Test1Controller _controller;
        public TestWorker(Test1Controller controller)
        {
            _controller = controller;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int index = 0;

            while (true)
            {
                index += 1;
                Thread.Sleep(1000);
                if (index % 3 == 0)
                {
                    _controller.TestApi1(index);
                }
            }
        }
    }
}
