using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Net;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Windows.Forms;

namespace WrekRip {
    public class AlbumInfo {
        //change this to real database when done
        private const string DB_GET_URL = "http://opdesk.wrek.org/automation/wrekrip_get.php";
        private const string DB_ADD_URL = "http://opdesk.wrek.org/automation/wrekrip_av_add.php";

        public static string UserID;
        public static string Password;
        public static string SessionCookie;

        public int ID { get; set; }
        public string Artist { get; protected set; }
        public string Title { get; protected set; }
        public string Formats { get; protected set; }
        public TrackCollection Tracks { get; protected set; }

        
        //Example XML to expect
        //Artist is in both for V/A albums
        //<WREKAlbum artist="Artist-album" title="Title-album" formats="Weekend,RRR-other????" albumid="324234">
        //  <WREKTrack artist="Artiest-track" category="MRR" trackid="12345" tracknum="1" title="Title-track" av_name="AUT12345" duration="3124???" />
        //</WREKAlbum>

        //Allow user to enter an album ID, if not entered then get the next random one?
        //random to allow for skipping?

        public bool GetNext(String inputAlbum) {
            string albumXml;

            //Grab data from database
            InitializeWebClient();
            if(m_web == null){
                return false;
            }
            try {
                albumXml = m_web.DownloadString(DB_GET_URL + "?album_id=" + inputAlbum);
            } catch (WebException ex) {
                m_web = null;
                UserID = null;
                Password = null;
                
                //make sure this gets caught in the UI section
                throw ex;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(albumXml);

            //int albumCount = 0;

            foreach (var erType in doc.GetElementsByTagName("Error")) {
                XmlElement erMess = erType as XmlElement;
                MessageBox.Show(erMess.InnerText, "Error", MessageBoxButtons.OK);
                return false;
            }

            //Populate Album info
            foreach (var album in doc.GetElementsByTagName("WREKAlbum")) {
                //if (albumCount >= 1) {
                    XmlElement xmlAlbum = album as XmlElement;

                    //Does this ID need to be generated for the AV database, or is it copied from the other database?
                    ID = int.Parse(xmlAlbum.Attributes["albumid"].Value);
                    Artist = xmlAlbum.Attributes["artist"].Value;
                    Title = xmlAlbum.Attributes["title"].Value;
                    //Formats = xmlAlbum.Attributes["formats"].Value;
                    //albumCount++;
                //} else {
                    //In this case, the interface returned more than one album
                    //for now, this is unexpected behavior
                //    return false;
                //}
            }

            Tracks = new TrackCollection();
            foreach (var track in doc.GetElementsByTagName("WREKTrack")) {
                XmlElement xmlTrack = track as XmlElement;
                if (track == null) continue;

                TrackInfo t = Tracks.AddNew();
                t.Artist = xmlTrack.Attributes["artist"].Value;
                t.Category = xmlTrack.Attributes["category"].Value;
                //make sure this is indeed the track number
                t.Index = int.Parse(xmlTrack.Attributes["tracknum"].Value);
                //Is this ID taken from the database or is it generated for the new entry in the AV database?
                t.ID = int.Parse(xmlTrack.Attributes["track_id"].Value);
                t.AVName = xmlTrack.Attributes["av_name"].Value;
                t.Title = xmlTrack.Attributes["title"].Value;
                //how do we determine the audio length?  it might need to be ripped FIRST
                t.AudioLength = new MyAVTime(int.Parse(xmlTrack.Attributes["duration"].Value));
            }

            return true;
        }

        void InitializeWebClient() {
            if (m_web != null) return;
            m_web = new WebClient();
            byte[] data = null;
            DialogResult dR;
            if (string.IsNullOrEmpty(UserID)) {
                Login frm = new Login();
                dR = frm.ShowDialog();
                if (dR == DialogResult.Cancel) {
                    m_web = null;
                    UserID = null;
                    return;
                }
                NameValueCollection loginPosts = new NameValueCollection();
                loginPosts.Add("initials", UserID);
                loginPosts.Add("password", Password);
                data = m_web.UploadValues("http://opdesk.wrek.org/new_logon.php", "POST", loginPosts);
                SessionCookie = m_web.ResponseHeaders[HttpResponseHeader.SetCookie];
            }
            while(data == null || data.Length != 0) {
                MessageBox.Show("Login failed", "Error", MessageBoxButtons.OK);
                m_web = null;
                UserID = null;
                InitializeWebClient();
                return;
            }
                m_web.Credentials = new NetworkCredential(UserID, Password);
                m_web.Headers[HttpRequestHeader.Cookie] = SessionCookie;
        }
        WebClient m_web;

        //Example XML to expect
        //Artist is in both for V/A albums
        //Sends in this order, 1 POST per track
        //  <WREKTrack track_id="512313" av_category="MRR" av_name="AUT512313" duration="3124567" />
        //</WREKAlbum>

        public bool UpdateTracks() {
            //TODO Put code here to send AVName, AudioLength, etc back to database
            //doc.DocumentElement.AppendChild()

            foreach (var track in Tracks) {
                if (track == null) continue;

                NameValueCollection sendNVC = new NameValueCollection();
                sendNVC.Add("track_id", track.ID.ToString());
                sendNVC.Add("av_name", track.AVName);
                sendNVC.Add("av_category", track.Category);
                sendNVC.Add("duration", track.AudioLength.ToString());

                byte[] data = m_web.UploadValues(DB_ADD_URL, "POST", sendNVC);
            }

            //m_web.UploadData(DB_ADD_URL, byte ARRAY?);

            

            return true;
        }
    }
}
