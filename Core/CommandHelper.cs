using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventRiteComposer.Core
{
    public class CommandHelper
    {
        public static string[] audioExtensions = { ".WAV", ".MID", ".MIDI", ".WMA", ".MP3", ".OGG", ".RMA", ".FLAC" };
        public static string[] videoExtensions = { ".AVI", ".MP4", ".DIVX", ".WMV", ".MKV", ".flv", ".flv", ".vob", ".mov", ".mpg", ".mp2", ".mpeg", ".3gp" };
        public static string[] imageExtensions = { ".PNG", ".JPG", ".JPEG", ".BMP", ".GIF" };
        public static string[] presentExtensions = { ".ppt", ".pptx", ".pptm", ".potx", ".potm", ".pot", ".ppsx", ".ppsm", ".pps", ".ppam", ".ppa", ".odp" };
        
        public static bool IsAudioFile(string path)
        {
            return audioExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsVideoFile(string path)
        {
            return videoExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsImageFile(string path)
        {
            return imageExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsPresenterFile(string path)
        {
            return presentExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        public static int IndexOf<T>(IEnumerable<T> source, T value)
        {
            int index = 0;
            var comparer = EqualityComparer<T>.Default; // or pass in as a parameter
            foreach (T item in source)
            {
                if (comparer.Equals(item, value)) return index;
                index++;
            }
            return -1;
        }
    }
}
