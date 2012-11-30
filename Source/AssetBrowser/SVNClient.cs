using System;
using System.Collections.Generic;
using System.Text;
using SharpSvn;
using SharpSvn.UI;

namespace AssetBrowser
{
    class SVNClient
    {
        private readonly string URI_SEPARATOR_CHAR = "/";
        private readonly string PATH_SEPARATOR_CHAR = System.IO.Path.DirectorySeparatorChar.ToString();

        private SvnClient m_svnClient = null;
        private System.Windows.Forms.IWin32Window m_parentWindow = null;

        public System.Windows.Forms.IWin32Window ParentWindow
        {
            get
            {
                return m_parentWindow;
            }
            set
            {
                m_parentWindow = value;
                SvnUIBindArgs args = new SvnUIBindArgs();
                args.ParentWindow = m_parentWindow;
                SvnUI.Bind(m_svnClient, args);
            }
        }

        public SVNClient(System.Windows.Forms.IWin32Window parentWindow = null)
        {
            m_svnClient = new SvnClient();

            if (parentWindow != null)
            {
                SvnUIBindArgs args = new SvnUIBindArgs();
                args.ParentWindow = parentWindow;
                SvnUI.Bind(m_svnClient, args);
            }
        }

        public bool CleanUp(string path)
        {
            try
            {
                return m_svnClient.CleanUp(path);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Lock(string workingCopy)
        {
            try
            {
                return m_svnClient.Lock(workingCopy, "AssetBrowser");
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Unlock(string workingCopy)
        {
            try
            {
                return m_svnClient.Unlock(workingCopy);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool CheckOut(string remoteURI, string localPath, long revision = -1)
        {
            try
            {
                SvnUriTarget svnUriTarget = new SvnUriTarget(remoteURI);
                SvnCheckOutArgs args = new SvnCheckOutArgs();
                SvnUpdateResult result;

                if (revision > -1)
                    args.Revision = new SvnRevision(revision);

                return m_svnClient.CheckOut(svnUriTarget, localPath, args, out result);
            }
            catch (UriFormatException ex)
            {
                PrintException(ex.Message);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        /*public void Changes(string path)
        {
            try
            {
                SvnInfoEventArgs svnInfoEventArgs;
                m_svnClient.GetInfo(path, out svnInfoEventArgs);

                return;
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }
        }*/

        public bool Update(string path, long revision = -1)
        {
            try
            {
                SvnUpdateArgs args = new SvnUpdateArgs();
                SvnUpdateResult result;

                if (revision >= -1)
                    args.Revision = revision;

                return m_svnClient.Update(path, args, out result);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Revert(ICollection<string> paths)
        {
            try
            {
                SvnRevertArgs args = new SvnRevertArgs();

                m_svnClient.Revert(paths, args);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Resolve(string path, SvnAccept choice)
        {
            try
            {
                SvnResolveArgs args = new SvnResolveArgs();

                return m_svnClient.Resolve(path, choice, args);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Commit(ICollection<string> paths, string message)
        {
            try
            {
                SvnCommitArgs args = new SvnCommitArgs();
                SvnCommitResult result;

                args.LogMessage = message;

                foreach (string p in paths)
                    m_svnClient.Add(p);

                return m_svnClient.Commit(paths, args, out result);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Move(string sourcePath, string toPath)
        {
            try
            {
                SvnMoveArgs args = new SvnMoveArgs();

                return m_svnClient.Move(sourcePath, toPath, args);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        public bool Delete(ICollection<string> paths)
        {
            try
            {
                SvnDeleteArgs args = new SvnDeleteArgs();

                return m_svnClient.Delete(paths, args);
            }
            catch (SvnException ex)
            {
                PrintException(ex.Message);
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }

            return false;
        }

        private void PrintException(string msg)
        {
            ManagedServices.MaxscriptSDK.ExecuteMaxscriptCommand(string.Format("print(\"!!! .NET Exception :  {0}\")", msg));
        }
    }
}
