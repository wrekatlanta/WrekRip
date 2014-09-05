using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AvFixupEngine;
using AudioVAULT;
using System.Diagnostics;

namespace WrekRip {
    public partial class WrekRip : Form {
        public WrekRip() {
            InitializeComponent();
        }

        public TrackCollection Tracks { get; set; }

        public TrackInfo CurrentTrack { get; set; }

        public void SetRunning(bool mode) {
            if (InvokeRequired) {
                BeginInvoke(new Action<bool>(SetRunning), mode);
                return;
            }
            btnStart.Enabled = !mode;
            btnStop.Enabled = mode;
            btnNextCD.Enabled = !mode;
            btnOK.Enabled = !mode;
        }

        #region UI Handlers
        private void btnStart_Click(object sender, EventArgs e) {
#if false
            StartIt();
#else
            SetRunning(true);
            CopyIt();
            SetRunning(false);
#endif
        }

        private void btnStop_Click(object sender, EventArgs e) {
            btnStop.Enabled = false;
            StopIt();
        }

        private void btnNextCD_Click(object sender, EventArgs e) {
            Cursor = Cursors.WaitCursor;

            if (m_album == null)
                m_album = new AlbumInfo();
            try {
                if (m_album.GetNext(txtAlbumID.Text)) {
                    txtAlbumID.Text = m_album.ID.ToString();
                    txtArtist.Text = m_album.Artist;
                    txtTitle.Text = m_album.Title;
                    txtFormats.Text = m_album.Formats;
                    Tracks = m_album.Tracks;
                    dgTracks.DataSource = Tracks;
                    btnStart.Enabled = true;
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Cursor = Cursors.Default;
        }
        AlbumInfo m_album;

        private void btnOK_Click(object sender, EventArgs e) {
            if (m_album != null) {
                m_album.UpdateTracks();
                txtAlbumID.Text = null;
                btnOK.Enabled = false;
                btnNextCD_Click(this, EventArgs.Empty);
            }
        }
        
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);

            SetRunning(false);
            btnStart.Enabled = false;
            btnOK.Enabled = false;

            dgTracks.AutoGenerateColumns = false;
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            if (m_engine != null) {
                m_engine.Fixups.ClearSelections();
            }
            StopIt();
            if (m_thread != null) {
                m_thread.Join(10000);
                m_thread = null;
            }
            if (m_adjplugin != null) {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_adjplugin);
                m_adjplugin = null;
            }
            if (m_plugin != null) {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_plugin);
                m_plugin = null;
            }
            if (m_engine != null) {
                m_engine.OnFile -= new __FixupEngine_OnFileEventHandler(engine_OnFile);
                m_engine.StatusMsg -= new __FixupEngine_StatusMsgEventHandler(engine_StatusMsg);
                m_engine.Error -= new __FixupEngine_ErrorEventHandler(engine_Error);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_engine);
                m_engine = null;
            }
        }

        #endregion

        #region FixupEngin Event Handlers
        void engine_Error(string msg) {
            if (InvokeRequired) {
                BeginInvoke(new Action<string>(engine_Error), msg);
                return;
            }
            //TODO Need more error handling
            txtError.Text = msg;
        }

        void engine_StatusMsg(string msg) {
            if (InvokeRequired) {
                BeginInvoke(new Action<string>(engine_StatusMsg), msg);
                return;
            }
            txtStatus.Text = msg;
        }

        void engine_OnFile(ref AudioVAULT.AVFile avf) {
            Debug.Print("OnFile: {0}, {1}", avf != null ? avf.Name : "", m_engine.canceled);
            TrackInfo t = null;
            if (avf != null) {
                t = Tracks.FindByCDName(avf.Name);
                CurrentTrack = t;
                Debug.Print("Start of: {0}, {1}", avf.Name, t.Index);
                t.Status = "Ripping";
            } else {
                Debug.Print("End of tracks");
            }
        }
        #endregion

        void SetProp(AVFixupPlugin plugin, string name, object value) {
            plugin.set_Properties(ref name, ref value);
        }

        void StartIt() {
            SetRunning(true);
            txtFile.Text = "<Starting>";
            m_thread = new System.Threading.Thread(CopyIt);
            m_thread.Start();
        }
        System.Threading.Thread m_thread;

        void StopIt() {
            Cursor = Cursors.WaitCursor;
            //if (m_thread != null) {
                if (m_engine != null)
                    m_engine.Abort();
                //m_thread.Join(10000);
            //}
            Cursor = Cursors.Default;
        }

        void CopyIt() {
            if (string.IsNullOrEmpty(System.Threading.Thread.CurrentThread.Name))
                System.Threading.Thread.CurrentThread.Name = "FixupEngine";
            InitializeFixupEngine();

            InitializeCopyPlugin();
            InitializeAdjustAudioPlugin();
            InitializeSetInfoPlugin();

            m_engine.OnFile += new __FixupEngine_OnFileEventHandler(engine_OnFile);
            m_engine.StatusMsg += new __FixupEngine_StatusMsgEventHandler(engine_StatusMsg);
            m_engine.Error += new __FixupEngine_ErrorEventHandler(engine_Error);

            m_engine.Run(true);
#if false
            m_engine.OnFile -= new __FixupEngine_OnFileEventHandler(engine_OnFile);
            m_engine.StatusMsg -= new __FixupEngine_StatusMsgEventHandler(engine_StatusMsg);
            m_engine.Error -= new __FixupEngine_ErrorEventHandler(engine_Error);

            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_engine);
            m_engine = null;

            System.Runtime.InteropServices.Marshal.ReleaseComObject(m_plugin);
            m_plugin = null;
#endif
            SetRunning(false);
        }

        void InitializeFixupEngine() {
            m_engine = new FixupEngine();

            if (m_Hostname == null)
                m_Hostname = (string)m_engine.AVSystem.Properties.Item("Hostname").Value;
            
            //TODO This needs to be adjustable
            m_engine.FilePath = "D:/";

            StringBuilder sb = new StringBuilder();
            foreach (TrackInfo t in Tracks) {
                if (sb.Length > 0) sb.Append(",");
                sb.AppendFormat("Track{0:00}", t.Index);
            }
            m_engine.FileNames = sb.ToString();

            m_engine.LogFileName = @"c:\audiovau\log\WrekRip%y%m%d.log";
            m_engine.LogLevel = LogDetails.logFull;
            bool b = false;
            m_engine.set_ResetLogFile(ref b);
            m_engine.UseNFS = true;
            m_engine.VaultList.Item("Native").Checked = true;
        }
        FixupEngine m_engine;
        string m_Hostname;

        void InitializeCopyPlugin() {
            object o = "CopyToOtherVault";
            o = m_engine.Fixups.get_Item(ref o);
            m_plugin = o as AVFixupPlugin;
            
            m_plugin.Checked = true;
            SetProp(m_plugin, "Always", true);
            SetProp(m_plugin, "Format", "Music");

            //TODO This may not be true for all computers
            SetProp(m_plugin, "Target", m_Hostname);
            
            //TODO This will be a problem if this program is running on more than one computer at the same time
            SetProp(m_plugin, "NewName", "$${file.Name}");
        }
        AVFixupPlugin m_plugin;

        void InitializeAdjustAudioPlugin() {
            object o = "AdjustAudio";
            o = m_engine.Fixups.get_Item(ref o);
            AVFixupPlugin plugin = o as AVFixupPlugin;
            m_adjplugin = plugin;
            
            plugin.Checked = true;
            SetProp(plugin, "EnergyUpdate", "AsNeeded");
            SetProp(plugin, "GainUpdate", "Always");
            SetProp(plugin, "GainMode", "Peak");
            SetProp(plugin, "TargetLevel", -8);
            SetProp(plugin, "TargetRatio", 0.01);
            SetProp(plugin, "MaximumGain", 20);
            SetProp(plugin, "TrimUpdate", "Always");
            SetProp(plugin, "BeginTrimLevel", -40);
            SetProp(plugin, "BeginTrimPad", 50);
            SetProp(plugin, "EndTrimLevel", -37);
            SetProp(plugin, "EndTrimPad", 50);
            SetProp(plugin, "EOMUpdate", "Never");
            SetProp(plugin, "EOMLevel", -14);
            SetProp(plugin, "EOMMaximumDuration", 20);
            SetProp(plugin, "TempoUpdate", "Never");

        }
        AVFixupPlugin m_adjplugin;

        void InitializeSetInfoPlugin() {
            m_setinfoplugin = new SetInfoPlugin();
            m_setinfoplugin.Parent = this;
            AVFixupPlugin mpi = m_setinfoplugin as AVFixupPlugin;
            mpi.Checked = true;
            string s2 = "SetFileInfo";
            IAVFixupHost host = m_engine as IAVFixupHost;
            m_setinfoplugin.Init(ref host);
            m_engine.Fixups.Add(ref mpi, ref s2);
        }
        SetInfoPlugin m_setinfoplugin;

        private void txtAlbumID_TextChanged(object sender, EventArgs e) {
            
        }
    }
}
