using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryMappedChat
{
    [Serializable]
    class ChatMessage
    {
        public string Text { get; set; }
        public DateTime Time { get; set; }
        public string Username { get; set; }
    }
}
