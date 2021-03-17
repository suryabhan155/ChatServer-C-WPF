using SignalrServer.Models;
using System.Threading.Tasks;

namespace SignalrServer
{
    public interface IClient
    {
        Task ParticipantDisconnection(string name);
        Task ParticipantReconnection(string name);
        Task ParticipantLogin(User client);
        Task ParticipantJoin(User from, bool video, bool screen);
        Task ParticipantReceiveOffer(User from,User to, WebrtcSdp sdp,bool video,bool screen);//added to
        Task ParticipantSendIceCricket(User from, WebrtcIceCandidate candidate);
        Task ParticipantOnSuccessIceCricket(User from, WebrtcIceCandidate candidate, bool video, bool screen);
        Task ParticipantOnSuccessAnswer(User from, WebrtcSdp sdp, bool video, bool screen);
        Task ParticipantCreateAnswer(User from, WebrtcSdp sdp);
        Task ParticipantLogout(string name);
        Task UnicastMessage(string sender, string message);
        Task BroadcastMessage(string sender, string message);
        Task BroadcastTextMessage(string sender, string message);
        Task BroadcastPictureMessage(string sender, byte[] img, string filename);
        Task UnicastTextMessage(string sender, string message);
        Task UnicastPictureMessage(string sender, byte[] img, string filename);
        Task ParticipantTyping(string sender);
    }
}
