using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.SignalR;

namespace Demo;

public class ChatHub : Hub
{
    private static List<object> list = [];
    public void SendText(string name, string text) 
    {
        list.Add(new { name,text});
        while (list.Count > 50) list.RemoveAt(0);
        Clients.All.SendAsync("ReceiveText", name, text);

    }
    public override Task OnConnectedAsync()
    {
        Clients.Caller.SendAsync("Initialize",list);
        return base.OnConnectedAsync();
    }
}
