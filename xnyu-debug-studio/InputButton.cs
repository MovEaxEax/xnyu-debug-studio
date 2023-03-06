using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xnyu_debug_studio
{
    public class InputButton
    {
        public string type;
        public int id;
        public string command;
        public bool selected;

        public InputButton(int _id, string _command)
        {
            this.id = _id;
            this.command = _command;
            this.selected = false;
        }
    }

}
