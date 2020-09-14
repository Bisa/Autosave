using System.IO;

namespace Autosave
{
	internal class AutosaveSlot
	{

        internal const int MaxID = 9999;
        internal const string Format = "{0:0000}";

        internal const string Delimiter = "-";

        DirectoryInfo dirInfo = null;
        int id = 0;

        string parent = string.Empty;

        public AutosaveSlot(string parentSlot, int autosaveId = 0){   
            dirInfo = new DirectoryInfo(Path.Combine(
                AutosaveController.GetSavePath(),
                parentSlot + Delimiter + string.Format(
                    Format, autosaveId
                )
            ));
            parent = parentSlot;
            id = autosaveId;
        }

        public AutosaveSlot(string parentSlot, DirectoryInfo directoryInfo, int autosaveId)
        {
            dirInfo = directoryInfo;
            parent = parentSlot;
            id = autosaveId;
        }

        public new string ToString() => string.Format(
                string.Format("{0}{1}{2}", parent, Delimiter, Format),
            id.ToString());

        internal int GetId()
        {
            return id;
        }

        internal DirectoryInfo GetDirectoryInfo()
        {
            return dirInfo;
        }

        internal string GetParent()
        {
            return parent;
        }
	}
}