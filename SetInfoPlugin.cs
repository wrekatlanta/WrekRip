using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvFixupEngine;
using AudioVAULT;

namespace WrekRip {
    public class SetInfoPlugin : AVFixupPlugin, IAVFixupPlugin {
        public WrekRip Parent { get; set; }

        #region _IAVFixupPlugin Members

        public void BeginRun() { }
        public void BeginVault(ref AudioVAULT.Vault v) { }
        public bool Configure() { return true; }
        public bool Configured { get { return true; } }
        public string Description { get { return "Set File Information"; } }
        public void EndRun(ref bool Abort) { }
        public void EndVault(ref AudioVAULT.Vault v, ref bool Abort) { }
        public bool FilterFile(ref AudioVAULT.AVFile file) { throw new NotImplementedException(); }

        public bool FixFile(ref AudioVAULT.AVFile file) {
            TrackInfo trk = Parent.CurrentTrack;
            trk.Status = "Done";
            trk.AudioLength = new MyAVTime(file.get_AudioLength(false));
            file.Properties.Item("Description").Value = trk.Title;
            file.Properties.Item("Client").Value = trk.Artist;
            file.Properties.Item("Class").Value = "ALL";
            file.Category = trk.Category;
            RenameFile(file, string.Format("AUT{0:#0000}", trk.ID), true);
            trk.AVName = file.Name;
            file.Commit(0);
            return true;
        }

        public void Init(ref _IAVFixupHost Host) { m_Host = Host; }
        public void Init(ref IAVFixupHost host) { m_Host = host; }
        _IAVFixupHost m_Host;
        
        public bool IsFilter { get { return false; } }
        public bool IsFixup { get { return true; } }
        public string Name { get { return "SetFileInfo"; } }
        public object PropertyNames { get { return null; } }
        public bool Runnable { get { return true; } }
        public string Title { get { return "Set File Info..."; } }
        public object get_Properties(ref string PropName) { throw new NotImplementedException(); }
        public void set_Properties(ref string PropName, ref object __p2) { throw new NotImplementedException(); }

        #endregion

        #region _AVFixupPlugin Members

        public bool Checked { get; set; }
        public IAVFixupPlugin get_Plugin() { return this as IAVFixupPlugin; }
        public void set_Plugin(ref IAVFixupPlugin __p1) { throw new NotImplementedException(); }

        #endregion

        void RenameFile(AVFile avf, string newname, bool force) {
            Vault v = avf.Vault;
            while (force) {
                AVFile f = v.OpenFile(newname, AVFileOpenFlags.avfOpenExistingCheck, AVFileType.avfAudio, 0);
                if (f == null) break;
                f.Delete();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(f);
            }
            avf.Name = newname;
        }
    }
}
