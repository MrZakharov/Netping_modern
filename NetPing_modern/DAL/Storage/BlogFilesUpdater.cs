using NetPing.DAL;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NetPing_modern.DAL.Storage
{
    public class BlogFilesUpdater
    {
        //Warning!
        //We must keep at least two versions of backup because InnerPagesController.UCacheAsyncWork method invokes update again if exception was raised
        private const string LastBackupDirName = "LastBackup";
        private const string PrevBackupDirName = "PreviousBackup";

        private const string TempDirName = "Temp";

        private static readonly Logger Log = LogManager.GetLogger(LogNames.Loader);

        public static void MoveTempToBlog()
        {
            var rootBlogDir = new DirectoryInfo(UrlBuilder.LocalPath_blogFiles);
            var tempDir = new DirectoryInfo(UrlBuilder.LocalPath_blogTempFiles);

            rootBlogDir.MoveTo(Path.Combine(UrlBuilder.LocalPath_blogBackupFiles, LastBackupDirName));

            // The most critical operation - Blog folder may bacame bacome 
            try
            {
                tempDir.MoveTo(UrlBuilder.LocalPath_blogFiles);
            }
            catch (Exception e)
            {
                //We don't know what folder keeps data which we had before updating because of InnerPagesController.UCacheAsyncWork issue
                var errStr = string.Format("Critical error you could find last worked version of Blog folder in {0} or {1}. Please don't run update operation while you didn't copy backup folders!",
                    Path.Combine(UrlBuilder.LocalPath_blogBackupFiles, LastBackupDirName),
                    Path.Combine(UrlBuilder.LocalPath_blogBackupFiles, PrevBackupDirName));
                Log.Error(e, errStr);
                throw;
            }
        }

        public static void PrepareBackupFolders()
        {
            var rootBackupDir = new DirectoryInfo(UrlBuilder.LocalPath_blogBackupFiles);
            var lastBackupDir = new DirectoryInfo(Path.Combine(UrlBuilder.LocalPath_blogBackupFiles, LastBackupDirName));
            var prevBackupDir = new DirectoryInfo(Path.Combine(UrlBuilder.LocalPath_blogBackupFiles, PrevBackupDirName));
            var tempDir = new DirectoryInfo(UrlBuilder.LocalPath_blogTempFiles);

            if (!rootBackupDir.Exists)
            {
                rootBackupDir.Create();
            }

            if (tempDir.Exists)
            {
                DeleteFolder(tempDir.FullName);
            }
            tempDir.Create();


            if (prevBackupDir.Exists)
            {
                DeleteFolder(prevBackupDir.FullName);
            }
            //last became prev before each update
            if (lastBackupDir.Exists)
            {
                lastBackupDir.MoveTo(prevBackupDir.FullName);
            }

        }

        private static void DeleteFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);
            if (dir.Exists)
            {

                foreach (FileInfo fi in dir.GetFiles())
                {
                    fi.Delete();
                }

                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    DeleteFolder(di.FullName);
                    di.Delete();
                }

                dir.Delete();
            }
        }

    }
}