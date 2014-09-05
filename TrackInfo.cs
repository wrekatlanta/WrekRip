using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace WrekRip {
    public class TrackInfo : INotifyPropertyChanged {
        public int Index { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Category { get; set; }
        public int ID { get; set; }

        public string AVName {
            get { return m_AVName; }
            set { if (value != m_AVName) { m_AVName = value; FirePropertyChanged("AVName"); } }
        }
        private string m_AVName;

        public MyAVTime AudioLength {
            get { return m_AudioLength; }
            set { if (value != m_AudioLength) { m_AudioLength = value; FirePropertyChanged("AudioLength"); } }
        }
        private MyAVTime m_AudioLength;

        public string Status {
            get { return m_Status; }
            set { if (value != m_Status) { m_Status = value; FirePropertyChanged("Status"); } }
        }
        private string m_Status;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void FirePropertyChanged(string name) {
            PropertyChangedEventHandler h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    public class TrackCollection : BindingList<TrackInfo> {
        public TrackInfo FindByCDName(string key) {
            int i = int.Parse(key.Substring(5, 2));
            foreach (TrackInfo t in this)
                if (t.Index == i) return t;
            throw new ArgumentException("Invalid track name: " + key);
        }
    }

    public class MyAVTime {
        public MyAVTime(int ms) {
            m_milliseconds = ms;
        }
        int m_milliseconds;

        public override string ToString() {
            int mins = m_milliseconds / 1000 / 60;
            int secs = (m_milliseconds / 1000) % 60;
            string s = string.Format("{0:00}:{1:00}", mins, secs);
            return s;
        }
    }
}
