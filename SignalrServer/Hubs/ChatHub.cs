using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalrServer.Models;
using SignalrServer.Signalr;

namespace SignalrServer.Hubs
{
    public class ChatHub : Hub<IClient>
    {
        private static ConcurrentDictionary<string, User> ChatClients = new ConcurrentDictionary<string, User>();
        private static ConcurrentDictionary<string, User> ScreenshareClients = new ConcurrentDictionary<string, User>();
        private static string RoomId;
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            Trace.WriteLine($"Join Room : {Context.ConnectionId},{groupName}");
        }
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            Trace.WriteLine($"Leave Room : {Context.ConnectionId},{groupName}");
        }
        public void SendAsync(string name, string message) => Clients.All.BroadcastMessage(name, message);

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var leave = LeaveGroup(RoomId);
            if (leave.IsCompleted)
            {
                ConnectedUser.Ids.Remove(Context.ConnectionId);
                var userName = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId).Key;
                if (userName != null)
                {
                    ChatClients.TryRemove(userName, out User client);
                    //await Clients.Others.ParticipantDisconnection(userName);
                    await Clients.OthersInGroup(RoomId).ParticipantDisconnection(userName);
                    Console.WriteLine($"<> {userName} disconnected");
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
        public async void BroadcastMessage(string message)
        {
            var name = Context.User.Identity.Name;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(message))
            {
                //await Clients.Others.BroadcastMessage(name, message);
                await Clients.OthersInGroup(RoomId).BroadcastMessage(name, message);
            }
        }
        
        public async Task<List<User>> Login(string name, string group,bool video,bool screen)
        {
            if (!ChatClients.ContainsKey(name))
            {
                Context.Items.TryAdd(Context.ConnectionId, name);
                RoomId = group;
                await JoinGroup(group);
                Console.WriteLine($"++ {name} logged in");
                List<User> users = new List<User>(ChatClients.Values.Where(x=>x.MeetingRoom == group));
                User newUser;
                newUser = new User { Name = name, ID = Context.ConnectionId, IsHost = false,MeetingRoom = group };
                var added = ChatClients.TryAdd(name, newUser);
                if (!added) return null;
                //await Clients.Others.ParticipantLogin(newUser);
                await Clients.OthersInGroup(RoomId).ParticipantLogin(newUser);
                await Clients.OthersInGroup(RoomId).ParticipantJoin(newUser,video,screen);
                return users;
            }
            return null;
        }

        public async void Logout()
        {
            var name = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            if (!string.IsNullOrEmpty(name))
            {
                User client = new User();
                ChatClients.TryRemove(name, out client);
                Context.Items.Remove(Context.ConnectionId,out object currentname);
                //await Clients.Others.ParticipantLogout(name);
                await Clients.OthersInGroup(RoomId).ParticipantLogout(name);
                Console.WriteLine($"-- {name} logged out");
            }
        }

        public async void SetOffer(User receiver, WebrtcSdp sdp,bool video,bool screen)
        {
            try
            {
                //if(video)
                //{
                //    var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
                //    if (!string.IsNullOrEmpty(receiver.Name) && sender != receiver.Name && sdp != null && ChatClients.ContainsKey(receiver.Name))
                //    {
                //        User client = new User();
                //        ChatClients.TryGetValue(sender, out client);
                //        await Clients.Client(receiver.ID).ParticipantReceiveOffer(client, receiver, sdp, video, screen);////added to
                //    }
                //}
                //else
                //{
                    User old = new User();
                    if (ChatClients.TryGetValue(receiver.Name, out old))
                    {
                        old.sessionid = receiver.sessionid;
                        User newuser = new User();
                        newuser = old;
                        if (ChatClients.TryUpdate(receiver.Name, receiver, old))
                        {
                            var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
                            if (!string.IsNullOrEmpty(receiver.Name) && sender != receiver.Name && sdp != null && ChatClients.ContainsKey(receiver.Name))
                            {
                                User client = new User();
                                ChatClients.TryGetValue(sender, out client);
                                await Clients.Client(receiver.ID).ParticipantReceiveOffer(client, receiver, sdp, video, screen);////added to
                            }
                        }
                    }
                //}
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public async void SetIceCandidate(User receiver, WebrtcIceCandidate candidate, bool video, bool screen)
        {
            try
            {
                User old = new User();
                var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
                var sender1 = ChatClients.SingleOrDefault((c) => c.Value.ID == receiver.ID).Key;
                ChatClients.TryGetValue(sender1, out old);
                old.sessionid = receiver.sessionid;
                old.MeetingRoom = RoomId;
                User newuser = new User();
                newuser = old;
                newuser.sessionid = old.sessionid;
                var t = ChatClients.TryUpdate(receiver.Name, newuser, old);
                if (t)
                {
                    if (!string.IsNullOrEmpty(receiver.Name) && sender != receiver.Name && candidate != null && ChatClients.ContainsKey(receiver.Name))
                    {
                        User client = new User();
                        ChatClients.TryGetValue(sender, out client);
                        await Clients.Client(receiver.ID).ParticipantOnSuccessIceCricket(client, candidate, video, screen);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public async void SetIceCandidateClient(User receiver, WebrtcIceCandidate candidate, bool video, bool screen)// for client ice candidate
        {
            var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            if (!string.IsNullOrEmpty(receiver.Name) && sender != receiver.Name && candidate != null && ChatClients.ContainsKey(receiver.Name))
            {
                User client = new User();
                ChatClients.TryGetValue(sender, out client);
                await Clients.Client(receiver.ID).ParticipantOnSuccessIceCricket(receiver, candidate, video, screen);
            }
        }
        public async void CreateAnswer(User receiver, WebrtcSdp sdp, bool video, bool screen)//getanswer
        {
            try
            {
                User old = new User();
                if (ChatClients.TryGetValue(ChatClients.SingleOrDefault((c) => c.Value.ID == receiver.ID).Key, out old))
                {
                    old.sessionid = receiver.sessionid;
                    old.MeetingRoom = RoomId;
                    User newuser = new User();
                    newuser = old;
                    if (ChatClients.TryUpdate(receiver.Name, receiver, old))
                    {
                        var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
                        if (!string.IsNullOrEmpty(receiver.Name) && sender != receiver.Name && sdp != null && ChatClients.ContainsKey(receiver.Name))
                        {
                            User client = new User();
                            ChatClients.TryGetValue(sender, out client);
                            await Clients.Client(receiver.ID).ParticipantOnSuccessAnswer(client, sdp, video, screen);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async void BroadcastTextMessage(string message)
        {
            var name = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(message))
            {
                //await Clients.Others.BroadcastTextMessage(name, message);
                await Clients.OthersInGroup(RoomId).BroadcastTextMessage(name, message);
            }
        }

        public async void BroadcastImageMessage(byte[] img, string filename)
        {
            var name = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            if (img != null)
            {
                //await Clients.Others.BroadcastPictureMessage(name, img,filename);
                await Clients.OthersInGroup(RoomId).BroadcastPictureMessage(name, img,filename);
            }
        }

        public async void UnicastTextMessage(string recepient, string message)
        {
            var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            if (!string.IsNullOrEmpty(sender) && recepient != sender && !string.IsNullOrEmpty(message) && ChatClients.ContainsKey(recepient))
            {
                User client = new User();
                ChatClients.TryGetValue(recepient, out client);
                await Clients.Client(client.ID).UnicastTextMessage(sender, message);
            }
        }

        public async void UnicastImageMessage(string recepient, byte[] img, string filename)
        {
            var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            if (!string.IsNullOrEmpty(sender) && recepient != sender && img != null && ChatClients.ContainsKey(recepient))
            {
                User client = new User();
                ChatClients.TryGetValue(recepient, out client);
                await Clients.Client(client.ID).UnicastPictureMessage(sender, img, filename);
            }
        }

        public async void Typing(string recepient)
        {
            if (recepient != "Everyone")
            {
                if (string.IsNullOrEmpty(recepient)) return;
                var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
                User client = new User();
                ChatClients.TryGetValue(recepient, out client);
                await Clients.Client(client.ID).ParticipantTyping(sender);
            }
            else//broadcast
            {
                if (string.IsNullOrEmpty(recepient)) return;
                var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
                User client = new User();
                ChatClients.TryGetValue(sender, out client);
                await Clients.All.ParticipantTyping(sender);
            }
        }

        public async void ScreenLogin(string name,string group,bool video,bool screen)
        {
            var sender = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId && c.Value.MeetingRoom == RoomId).Key;
            User olduser;
            ChatClients.TryGetValue(name, out olduser);// user info of old user
            List<User> users = new List<User>(ChatClients.Values.Where(x => x.MeetingRoom == RoomId));
            User newUser;
            newUser = new User { Name = name, ID = Context.ConnectionId, IsHost = false, MeetingRoom = RoomId };
            await Clients.OthersInGroup(RoomId).ParticipantJoin(newUser, video, screen);
        }
    }
}
