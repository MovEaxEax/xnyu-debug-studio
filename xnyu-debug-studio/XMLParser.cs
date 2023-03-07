using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xnyu_debug_studio
{
    public class XMLParser
    {
        public struct ModInfo
        {
            public string name;
            public string version;
            public string link;
        };

        public static List<ModInfo> mods;

        public XMLParser(string text)
        {
            mods = parseText(text);
        }

        public List<ModInfo> parseText(string text)
        {
            List<ModInfo> _mods = new List<ModInfo>();

            text = text.Replace("\n", "");
            text = text.Replace("\r", "");
            text = text.Replace("\t", "");

            text = text.Replace("<mods>", "");
            text = text.Replace("</mods>", "");

            text = text.Replace("<mod>", "");
            text = text.Replace("</mod>", "");

            int index = 0;
            while (index + 48 < text.Length)
            {
                int indexName = text.IndexOf("<name>", index);
                int indexVersion = text.IndexOf("<version>", index);
                int indexLink = text.IndexOf("<link>", index);

                int indexNameClose = text.IndexOf("</name>", index);
                int indexVersionClose = text.IndexOf("</version>", index);
                int indexLinkClose = text.IndexOf("</link>", index);

                if (indexName > -1 || indexVersion > -1 || indexLink > -1 || indexNameClose > -1 || indexVersionClose > -1 || indexLinkClose > -1) break;

                ModInfo _mod = new ModInfo();

                _mod.name = text.Substring(indexName + 6, indexNameClose - (indexName + 6));
                _mod.version = text.Substring(indexVersion + 9, indexVersionClose - (indexName + 9));
                _mod.link = text.Substring(indexLink + 6, indexLinkClose - (indexName + 6));

                if (_mod.name != "" && _mod.version != "" && _mod.link != "") _mods.Add(_mod);

                if (indexName > indexVersion && indexName > indexLink) index = indexNameClose + 7;
                if (indexVersion > indexName && indexVersion > indexLink) index = indexVersionClose + 10;
                if (indexLink > indexVersion && indexLink > indexName) index = indexLinkClose + 7;
            }

            return _mods;
        }

        public string[] getNames(string text)
        {
            string[] names = new string[mods.Count];
            for (int i = 0; i < mods.Count; i++) names[i] = mods[i].name;
            return names;
        }

        public string[] getVersions(string text)
        {
            string[] versions = new string[mods.Count];
            for (int i = 0; i < mods.Count; i++) versions[i] = mods[i].version;
            return versions;
        }

        public string[] getLinks(string text)
        {
            string[] links = new string[mods.Count];
            for (int i = 0; i < mods.Count; i++) links[i] = mods[i].link;
            return links;
        }

    }

}


