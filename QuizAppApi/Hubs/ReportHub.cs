using Microsoft.AspNetCore.SignalR;

namespace QuizAppApi.Hubs
{
    public class ReportHub : Hub
    {
        public async Task SendReport(string groupId, string reportPath)
        {
            await Clients.Group(groupId).SendAsync("ReceiveReport", reportPath);
        }

        public async Task SendAnalysisReport(string groupId, string reportPath)
        {
            await Clients.Group(groupId).SendAsync("ReceiveAnalysisReport", reportPath);
        }
    }
}
