using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Exceptions;

namespace Umbraco.Core.IO
{
	public class IOHelper
    {
	    private readonly IHostingEnvironment _hostingEnvironment;
	    private readonly IApplicationEnvironment _appEnv;

	    public IOHelper(IHostingEnvironment hostingEnvironment, IApplicationEnvironment appEnv)
	    {
	        _hostingEnvironment = hostingEnvironment;
	        _appEnv = appEnv;
	    }

	    // static compiled regex for faster performance
        private readonly static Regex ResolveUrlPattern = new Regex("(=[\"\']?)(\\W?\\~(?:.(?![\"\']?\\s+(?:\\S+)=|[>\"\']))+.)[\"\']?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static char DirSepChar
        {
            get
            {
                return Path.DirectorySeparatorChar;
            }
        }

	    //internal static void UnZip(string zipFilePath, string unPackDirectory, bool deleteZipFile)
	    //{
	    //    // Unzip
	    //    string tempDir = unPackDirectory;
	    //    Directory.CreateDirectory(tempDir);

	    //    using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath)))
	    //    {
     //           ZipEntry theEntry;
     //           while ((theEntry = s.GetNextEntry()) != null)
     //           {
     //               string directoryName = Path.GetDirectoryName(theEntry.Name);
     //               string fileName = Path.GetFileName(theEntry.Name);

     //               if (fileName != String.Empty)
     //               {
     //                   FileStream streamWriter = File.Create(tempDir + Path.DirectorySeparatorChar + fileName);

     //                   int size = 2048;
     //                   byte[] data = new byte[2048];
     //                   while (true)
     //                   {
     //                       size = s.Read(data, 0, data.Length);
     //                       if (size > 0)
     //                       {
     //                           streamWriter.Write(data, 0, size);
     //                       }
     //                       else
     //                       {
     //                           break;
     //                       }
     //                   }

     //                   streamWriter.Close();

     //               }
     //           }

     //           // Clean up
     //           s.Close();
     //           if (deleteZipFile)
     //               File.Delete(zipFilePath);
     //       }
	    //}

        //Replaces tildes with the root dir
        public string ResolveUrl(string virtualPath)
        {
            if (virtualPath.StartsWith("~"))
                return ToAbsolute(virtualPath);
            if (Uri.IsWellFormedUriString(virtualPath, UriKind.Absolute))
                return virtualPath;
            return ToAbsolute(virtualPath);
        }

	    private string ToAbsolute(string virtualPath)
	    {
	        return string.Concat(_appEnv.ApplicationBasePath, virtualPath.TrimStart('~')).Replace("//", "/");
	    }

        public string MapPath(string path)
        {
            // Check if the path is already mapped
            if ((path.Length >= 2 && path[1] == Path.VolumeSeparatorChar)
                || path.StartsWith(@"\\")) //UNC Paths start with "\\". If the site is running off a network drive mapped paths will look like "\\Whatever\Boo\Bar"
            {
                return path;
            }

            return _hostingEnvironment.MapPath(path);
        }

  //      //use a tilde character instead of the complete path
		//internal static string ReturnPath(string settingsKey, string standardPath, bool useTilde)
  //      {
  //          string retval = ConfigurationManager.AppSettings[settingsKey];

  //          if (String.IsNullOrEmpty(retval))
  //              retval = standardPath;

  //          return retval.TrimEnd('/');
  //      }

        //internal static string ReturnPath(string settingsKey, string standardPath)
        //{
        //    return ReturnPath(settingsKey, standardPath, false);

        //}

        /// <summary>
        /// Verifies that the current filepath matches a directory where the user is allowed to edit a file.
        /// </summary>
        /// <param name="filePath">The filepath to validate.</param>
        /// <param name="validDir">The valid directory.</param>
        /// <returns>A value indicating whether the filepath is valid.</returns>
        internal bool VerifyEditPath(string filePath, string validDir)
        {
            return VerifyEditPath(filePath, new[] { validDir });
        }

	    /// <summary>
	    /// Validates that the current filepath matches a directory where the user is allowed to edit a file.
	    /// </summary>
	    /// <param name="filePath">The filepath to validate.</param>
	    /// <param name="validDir">The valid directory.</param>
	    /// <returns>True, if the filepath is valid, else an exception is thrown.</returns>
	    internal bool ValidateEditPath(string filePath, string validDir)
        {
            if (VerifyEditPath(filePath, validDir) == false)
                throw new FileSecurityException(
                    string.Format("The filepath '{0}' is not within an allowed directory for this type of files", 
                    filePath.Replace(_hostingEnvironment.WebRootPath, "")));
            return true;
        }

	    /// <summary>
	    /// Verifies that the current filepath matches one of several directories where the user is allowed to edit a file.
	    /// </summary>
	    /// <param name="filePath">The filepath to validate.</param>
	    /// <param name="validDirs">The valid directories.</param>
	    /// <returns>A value indicating whether the filepath is valid.</returns>
	    internal bool VerifyEditPath(string filePath, IEnumerable<string> validDirs)
        {
            // this is called from ScriptRepository, PartialViewRepository, etc.
            // filePath is the fullPath (rooted, filesystem path, can be trusted)
            // validDirs are virtual paths (eg ~/Views)
            //
            // except that for templates, filePath actually is a virtual path

            //TODO
            // what's below is dirty, there are too many ways to get the root dir, etc.
            // not going to fix everything today

            var mappedRoot = _hostingEnvironment.WebRootPath;
            if (filePath.StartsWith(mappedRoot) == false)
                filePath = MapPath(filePath);

            // yes we can (see above)
            //// don't trust what we get, it may contain relative segments
            //filePath = Path.GetFullPath(filePath);

            foreach (var dir in validDirs)
            {
                var validDir = dir;
                if (validDir.StartsWith(mappedRoot) == false)
                    validDir = MapPath(validDir);

                if (PathStartsWith(filePath, validDir, Path.DirectorySeparatorChar))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies that the current filepath has one of several authorized extensions.
        /// </summary>
        /// <param name="filePath">The filepath to validate.</param>
        /// <param name="validFileExtensions">The valid extensions.</param>
        /// <returns>A value indicating whether the filepath is valid.</returns>
        internal bool VerifyFileExtension(string filePath, List<string> validFileExtensions)
        {
            var ext = Path.GetExtension(filePath);
            return ext != null && validFileExtensions.Contains(ext.TrimStart('.'));
        }

        /// <summary>
        /// Validates that the current filepath has one of several authorized extensions.
        /// </summary>
        /// <param name="filePath">The filepath to validate.</param>
        /// <param name="validFileExtensions">The valid extensions.</param>
        /// <returns>True, if the filepath is valid, else an exception is thrown.</returns>
        /// <exception cref="FileSecurityException">The filepath is invalid.</exception>
        internal bool ValidateFileExtension(string filePath, List<string> validFileExtensions)
        {
            if (VerifyFileExtension(filePath, validFileExtensions) == false)
                throw new FileSecurityException(
                    string.Format("The extension for the current file '{0}' is not of an allowed type for this editor. This is typically controlled from either the installed MacroEngines or based on configuration in /config/umbracoSettings.config", 
                    filePath.Replace(_hostingEnvironment.WebRootPath, "")));
            return true;
        }

        public static bool PathStartsWith(string path, string root, char separator)
        {
            // either it is identical to root,
            // or it is root + separator + anything

            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) == false) return false;
            if (path.Length == root.Length) return true;
            if (path.Length < root.Length) return false;
            return path[root.Length] == separator;
        }

        /// <summary>
        /// Returns the path to the root of the application, by getting the path to where the assembly where this
        /// method is included is present, then traversing until it's past the /bin directory. Ie. this makes it work
        /// even if the assembly is in a /bin/debug or /bin/release folder
        /// </summary>
        /// <returns></returns>
        internal string GetRootDirectorySafe()
        {
        	return Path.GetDirectoryName(_hostingEnvironment.WebRootPath);
        }        

        /// <summary>
        /// Check to see if filename passed has any special chars in it and strips them to create a safe filename.  Used to overcome an issue when Umbraco is used in IE in an intranet environment.
        /// </summary>
        /// <param name="filePath">The filename passed to the file handler from the upload field.</param>
        /// <returns>A safe filename without any path specific chars.</returns>
        internal static string SafeFileName(string filePath)
        {
            // use string extensions
            return filePath.ToSafeFileName();
        }

	    public void EnsurePathExists(string path)
	    {
	        var absolutePath = MapPath(path);
	        if (Directory.Exists(absolutePath) == false)
	            Directory.CreateDirectory(absolutePath);
	    }

	    public void EnsureFileExists(string path, string contents)
	    {
	        var absolutePath = MapPath(path);
	        if (File.Exists(absolutePath)) return;

	        using (var writer = File.CreateText(absolutePath))
	        {
	            writer.Write(contents);
	        }
	    }

	    /// <summary>
	    /// Deletes all files passed in.
	    /// </summary>
	    /// <param name="files"></param>
	    /// <param name="mediaFileSystem"></param>
	    /// <param name="onError"></param>
	    /// <param name="contentSection"></param>
	    /// <returns></returns>
	    internal bool DeleteFiles(IEnumerable<string> files, MediaFileSystem mediaFileSystem, IContentSection contentSection,
            Action<string, Exception> onError = null)
        {
            //ensure duplicates are removed
            files = files.Distinct();

            var allsuccess = true;
            
            Parallel.ForEach(files, file =>
            {
                try
                {
                    if (file.IsNullOrWhiteSpace()) return;

                    var relativeFilePath = mediaFileSystem.GetRelativePath(file);
                    if (mediaFileSystem.FileExists(relativeFilePath) == false) return;

                    var parentDirectory = Path.GetDirectoryName(relativeFilePath);

                    // don't want to delete the media folder if not using directories.
                    if (contentSection.UploadAllowDirectories && parentDirectory != mediaFileSystem.GetRelativePath("/"))
                    {
                        //issue U4-771: if there is a parent directory the recursive parameter should be true
                        mediaFileSystem.DeleteDirectory(parentDirectory, String.IsNullOrEmpty(parentDirectory) == false);
                    }
                    else
                    {
                        mediaFileSystem.DeleteFile(file, true);
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke(file, e);
                    allsuccess = false;
                }
            });

            return allsuccess;
        }
    }
}
