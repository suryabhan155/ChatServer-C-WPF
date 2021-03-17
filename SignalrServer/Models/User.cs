using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalrServer.Models
{
    public class User
    {
        //public User(string name, string id,string session)
        //{
        //    Name = name;
        //    ID = id;
        //    sessionid = session;
        //}
        public string Name { get; set; }
        public string ID { get; set; }
        public string Message { get; set; }
        public bool IsHost { get; set; }
        public WebrtcSdp Sdp { get; set; }
        public WebrtcIceCandidate IceCandidate { get; set; }
        public string MeetingRoom { get; set; }
        public string sessionid { get; set; }
    }
    public class WebrtcSdp
    {
        public string Sdp { get; set; }
        public SdpTypes Type { get; set; }
    }
    public class WebrtcIceCandidate
    {
        public string SdpMid { get; set; }
        public int SdpIndex { get; set; }
        public string Sdp { get; set; }
    }
    public enum SdpTypes
    {
        Answer,
        Offer
    }
}
