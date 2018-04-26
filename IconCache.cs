using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;

namespace Datasheets2
{
    public class IconCache
    {
        // These filetypes have unique icons, so we cannot cache per-filetype
        private static readonly string[] DynamicFileTypes = new string[]
        {
            ".exe",
            ".ico",
            ".url",
        };

        // Internal struct for keeping track of icon loading state
        struct LoadTaskState
        {
            public LoadTaskState(Task task, Action onLoaded)
            {
                this.Task = task;
                this.Notifiers = new List<Action>() { onLoaded };
            }

            public Task Task { get; set; }
            public List<Action> Notifiers { get; set; }

            public void Notify()
            {
                foreach (var notifier in Notifiers)
                    notifier.Invoke();
            }
        }

        // Internal caches:
        private static ImageSource folderIcon;
        private static Dictionary<string, ImageSource> iconCache = new Dictionary<string, ImageSource>();
        private static Dictionary<string, LoadTaskState> loadTaskCache = new Dictionary<string, LoadTaskState>();

        private static string GetPathKey(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

            // These filetypes have unique icons, so we cannot cache per-filetype
            if (DynamicFileTypes.Contains(ext))
                return path;

            return ext;
        }

        /// <summary>
        /// Get an icon for the path from the cache, if available.
        /// If not available, start acquiring one in the background and notify when ready
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ImageSource GetIconForFile(string path, Action onLoaded = null)
        {
            ImageSource source = null;

            string key = GetPathKey(path);

            if (iconCache.TryGetValue(key, out source))
                return source;

            // Start loading
            LoadIconForFile(path, key, onLoaded);

            // ImageSource not yet available, return null
            return null;
        }

        private static void LoadIconForFile(string path, string key, Action onLoaded = null)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

            // Don't re-load an icon if we're already trying to load one for that type
            LoadTaskState state;
            if (loadTaskCache.TryGetValue(key, out state))
            {
                // Add notifier
                state.Notifiers.Add(onLoaded);
                return;
            }

            // Spin off a new task to handle loading of the icon
            var task = Task.Factory.StartNew(async () =>
            {
                var icon = await IconUtil.GetIconForPathAsync(
                    path, 
                    IconUtil.IconSize.SmallIcon, 
                    IconUtil.PathType.File, 
                    useCache: false);

                var source = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    //System.Windows.Int32Rect.Empty,
                    new System.Windows.Int32Rect(0, 0, 16, 16), // TODO: This doesn't work well with DPI scaling
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                // Must freeze the DependencyObject so it can be shared across threads
                source.Freeze();

                // Add the icon to our filetype cache
                // TODO: Do we need to lock for thread safety?
                iconCache[key] = source;

                // Notify users that the icon is ready
                state = loadTaskCache[key];
                state.Notify();
            });

            // Keep track of which filetypes we're currently loading
            loadTaskCache[key] = new LoadTaskState(task, onLoaded);
        }

        public static ImageSource GetIconForFolder()
        {
            // TODO: Make this async also. Probably don't really need to since it'll only be called once.
            if (folderIcon == null)
            {
                var icon = IconUtil.GetIconForPathAsync(
                    "C:\\folderthatdoesntexistasdasfasdfafsd",
                    IconUtil.IconSize.SmallIcon,
                    IconUtil.PathType.Directory
                ).Result; // Blocking

                folderIcon = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    //System.Windows.Int32Rect.Empty,
                    new System.Windows.Int32Rect(0, 0, 16, 16),
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                folderIcon.Freeze();
            }

            return folderIcon;
        }
    }
}
